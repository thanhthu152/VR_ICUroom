using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ICUSim
{
    /// <summary>
    /// Chuyen giua cac kich ban. Bam phim 0-3 (khi dang Play, nho bam vao khung Game truoc):
    /// 0 = Binh thuong, 1 = Ap luc cao, 2 = The tich tho ra thap, 3 = Tan so tho cao.
    /// Cac gia tri chi la MINH HOA - can giang vien / dieu duong ICU kiem tra lai.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        public PatientModel patient;
        public int startScenario = 0;
        public VitalSet[] scenarios = DefaultScenarios();

        public int CurrentIndex { get; private set; }
        public string CurrentName =>
            (scenarios != null && CurrentIndex < scenarios.Length) ? scenarios[CurrentIndex].name : "";

        void Start()
        {
            if (patient == null) patient = FindFirstObjectByType<PatientModel>();
            Apply(startScenario, true);
        }

        void Update()
        {
            for (int i = 0; i <= 9 && scenarios != null && i < scenarios.Length; i++)
            {
                if (Pressed(i)) Apply(i, false);
            }
        }

        /// <summary>Goi tu nut bam UI / VR sau nay.</summary>
        public void ApplyScenario(int index)
        {
            Apply(index, false);
        }

        void Apply(int index, bool instant)
        {
            if (patient == null || scenarios == null || index < 0 || index >= scenarios.Length) return;
            CurrentIndex = index;
            patient.SetTarget(scenarios[index], instant);
            Debug.Log($"[ScenarioManager] Scenario {index}: {scenarios[index].name}");
        }

        static bool Pressed(int n)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (n)
            {
                case 0: return kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame;
                case 1: return kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
                case 2: return kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;
                case 3: return kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame;
                case 4: return kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
                case 5: return kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame;
                case 6: return kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame;
                case 7: return kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame;
                case 8: return kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame;
                case 9: return kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(KeyCode.Alpha0 + n);
#endif
        }

        static VitalSet[] DefaultScenarios()
        {
            return new[]
            {
                new VitalSet
                {
                    name = "0. Normal",
                    heartRate = 85, spO2 = 96, etCO2 = 38, systolic = 120, diastolic = 75, temperature = 37.2f,
                    respRate = 14, peakPressure = 22, peep = 5, tidalVolumeInsp = 450, tidalVolumeExp = 440, fiO2 = 40
                },
                new VitalSet
                {
                    // Vd: dom tac, gap ong, co that phe quan -> ap luc dinh vuot gioi han
                    name = "1. High airway pressure",
                    heartRate = 108, spO2 = 88, etCO2 = 44, systolic = 138, diastolic = 84, temperature = 37.2f,
                    respRate = 14, peakPressure = 42, peep = 5, tidalVolumeInsp = 340, tidalVolumeExp = 330, fiO2 = 40
                },
                new VitalSet
                {
                    // Vd: tuot ong / ro khi -> the tich tho ra thap, mat song CO2
                    name = "2. Low exhaled volume",
                    heartRate = 112, spO2 = 86, etCO2 = 3, systolic = 132, diastolic = 82, temperature = 37.2f,
                    respRate = 14, peakPressure = 8, peep = 1, tidalVolumeInsp = 450, tidalVolumeExp = 90, fiO2 = 40
                },
                new VitalSet
                {
                    // Vd: kich thich, dau, thieu oxy -> benh nhan tho nhanh
                    name = "3. High respiratory rate",
                    heartRate = 118, spO2 = 92, etCO2 = 31, systolic = 148, diastolic = 90, temperature = 37.4f,
                    respRate = 34, peakPressure = 26, peep = 5, tidalVolumeInsp = 420, tidalVolumeExp = 410, fiO2 = 40
                },
            };
        }
    }
}
