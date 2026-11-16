
using UnityEngine;

namespace ICUSim
{
    /// <summary>Types of waveforms that can be displayed on the screen.</summary>
    public enum WaveType { ECG, Pleth, Capno, Pressure, Flow, Volume }

    /// <summary>A set of patient vital signs and ventilator parameters (used for each scenario).</summary>
    [System.Serializable]
    public class VitalSet
    {
        public string name = "Normal";

        [Header("Patient monitor")]
        public float heartRate = 85f;      // bpm
        public float spO2 = 96f;           // %
        public float etCO2 = 38f;          // mmHg
        public float systolic = 120f;      // mmHg
        public float diastolic = 75f;      // mmHg
        public float temperature = 37.2f;  // °C

        [Header("Ventilator")]
        public float respRate = 14f;          // fTotal, /min
        public float peakPressure = 22f;      // Ppeak, cmH2O
        public float peep = 5f;               // cmH2O
        public float tidalVolumeInsp = 450f;  // VTi delivered, mL
        public float tidalVolumeExp = 440f;   // VTe, mL
        public float fiO2 = 40f;              // %

        public VitalSet Clone() => (VitalSet)MemberwiseClone();
    }

    /// <summary>
    /// The single "source of truth" for the simulation: both monitors read their values from here.
    /// When switching scenarios, ventilator parameters change quickly, while vital signs change slowly.
    /// </summary>
    public class PatientModel : MonoBehaviour
    {
        [Tooltip("Time in seconds for ventilator parameters to reach their new values")]
        public float ventilatorResponseTime = 2f;

        [Tooltip("Time in seconds for vital signs (SpO2, HR, BP, etc.) to reach their new values")]
        public float vitalsResponseTime = 20f;

        [Range(0.2f, 0.5f)]
        [Tooltip("Fraction of each respiratory cycle spent on inspiration (0.33 = I:E 1:2)")]
        public float inspiratoryFraction = 0.33f;

        public VitalSet current = new VitalSet();
        VitalSet target;

        public float MeanArterialPressure => (current.systolic + 2f * current.diastolic) / 3f;
        public float MinuteVolume => current.tidalVolumeExp * current.respRate / 1000f; // L/min

        void Awake()
        {
            target = current.Clone();
        }

        public void SetTarget(VitalSet next, bool instant = false)
        {
            if (next == null) return;
            target = next.Clone();
            if (instant) current = next.Clone();
        }

        void Update()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            // Reach approximately 95% of the new value after the specified response time
            float fast = 1f - Mathf.Exp(-dt * 3f / Mathf.Max(0.01f, ventilatorResponseTime));
            float slow = 1f - Mathf.Exp(-dt * 3f / Mathf.Max(0.01f, vitalsResponseTime));

            // Ventilator parameters + EtCO2: fast response
            current.respRate        = Mathf.Lerp(current.respRate, target.respRate, fast);
            current.peakPressure    = Mathf.Lerp(current.peakPressure, target.peakPressure, fast);
            current.peep            = Mathf.Lerp(current.peep, target.peep, fast);
            current.tidalVolumeInsp = Mathf.Lerp(current.tidalVolumeInsp, target.tidalVolumeInsp, fast);
            current.tidalVolumeExp  = Mathf.Lerp(current.tidalVolumeExp, target.tidalVolumeExp, fast);
            current.fiO2            = Mathf.Lerp(current.fiO2, target.fiO2, fast);
            current.etCO2           = Mathf.Lerp(current.etCO2, target.etCO2, fast);

            // Vital signs: slow response
            current.heartRate   = Mathf.Lerp(current.heartRate, target.heartRate, slow);
            current.spO2        = Mathf.Lerp(current.spO2, target.spO2, slow);
            current.systolic    = Mathf.Lerp(current.systolic, target.systolic, slow);
            current.diastolic   = Mathf.Lerp(current.diastolic, target.diastolic, slow);
            current.temperature = Mathf.Lerp(current.temperature, target.temperature, slow);
        }

        /// <summary>Repetition rate of the waveform (cycles per minute).</summary>
        public float GetRate(WaveType type)
        {
            return (type == WaveType.ECG || type == WaveType.Pleth)
                ? current.heartRate
                : current.respRate;
        }

        /// <summary>Waveform value at phase 0..1 within one cycle.</summary>
        public float Evaluate(WaveType type, float phase)
        {
            switch (type)
            {
                case WaveType.ECG:      return Ecg(phase);
                case WaveType.Pleth:    return Pleth(phase) * (0.6f + 0.4f * Mathf.InverseLerp(80f, 96f, current.spO2));
                case WaveType.Capno:    return Capno(phase);
                case WaveType.Pressure: return Pressure(phase);
                case WaveType.Flow:     return Flow(phase);
                case WaveType.Volume:   return Volume(phase);
                default:                return 0f;
            }
        }

        // ---------- Waveform shapes ----------

        static float Gauss(float x, float mu, float sigma)
        {
            float d = (x - mu) / sigma;
            return Mathf.Exp(-0.5f * d * d);
        }

        // ECG: P - QRS - T
        static float Ecg(float p)
        {
            return 0.12f * Gauss(p, 0.10f, 0.025f)
                 - 0.12f * Gauss(p, 0.20f, 0.012f)
                 + 1.00f * Gauss(p, 0.23f, 0.018f)
                 - 0.25f * Gauss(p, 0.265f, 0.014f)
                 + 0.30f * Gauss(p, 0.45f, 0.045f);
        }

        // Pleth: pulse waveform with a dicrotic notch
        static float Pleth(float p)
        {
            return Gauss(p, 0.30f, 0.09f) + 0.35f * Gauss(p, 0.55f, 0.07f);
        }

        // Capnogram (mmHg): approximately 0 during inspiration, rapid rise followed by a plateau during expiration
        float Capno(float p)
        {
            float fi = inspiratoryFraction;
            float et = current.etCO2;
            if (p < fi)
                return et * (1f - Mathf.SmoothStep(0f, 1f, p / 0.06f));
            float q = (p - fi) / (1f - fi);
            float rise = Mathf.SmoothStep(0f, 1f, q / 0.15f);
            float plateau = 0.9f + 0.1f * q;
            return et * rise * plateau;
        }

        // Airway pressure (cmH2O), volume-controlled ventilation (VC)
        float Pressure(float p)
        {
            float fi = inspiratoryFraction;
            float peep = current.peep;
            float pk = current.peakPressure;
            if (p < fi)
            {
                float q = p / fi;
                float rise = Mathf.SmoothStep(0f, 1f, q / 0.08f);
                return peep + (pk - peep) * rise * (0.3f + 0.7f * q);
            }
            float e = (p - fi) / (1f - fi);
            return peep + (pk - peep) * Mathf.Exp(-e * 25f);
        }

        // Flow (L/min): positive during inspiration (square waveform), negative during expiration (exponential decay)
        float Flow(float p)
        {
            float fi = inspiratoryFraction;
            float period = 60f / Mathf.Max(1f, current.respRate);
            float ti = period * fi;
            float te = period - ti;

            if (p < fi)
            {
                float q = p / fi;
                float inspFlow = current.tidalVolumeInsp / 1000f / ti * 60f;
                float onRamp = Mathf.SmoothStep(0f, 1f, q / 0.05f);
                float offRamp = 1f - Mathf.SmoothStep(0f, 1f, (q - 0.95f) / 0.05f);
                return inspFlow * onRamp * offRamp;
            }

            const float k = 6f;
            float e = (p - fi) / (1f - fi);
            float peakExp = current.tidalVolumeExp / 1000f * k / (te * (1f - Mathf.Exp(-k))) * 60f;
            return -peakExp * Mathf.Exp(-k * e);
        }

        // Volume (mL): if VTe < VTi (leak/tube disconnection), the curve does not return to zero
        float Volume(float p)
        {
            float fi = inspiratoryFraction;
            float vti = current.tidalVolumeInsp;
            float vte = Mathf.Min(current.tidalVolumeExp, vti);
            if (p < fi) return vti * (p / fi);

            const float k = 6f;
            float e = (p - fi) / (1f - fi);
            return vti - vte * (1f - Mathf.Exp(-k * e)) / (1f - Mathf.Exp(-k));
        }
    }
}
