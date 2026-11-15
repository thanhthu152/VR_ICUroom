using UnityEngine;
using UnityEngine.UI;

namespace ICUSim
{
    /// <summary>
    /// Ve mot dang song kieu monitor: net ve chay tu trai sang phai,
    /// co mot khoang trong (gap) di truoc dau ve, ghi de len song cu.
    /// Gan component nay vao mot object UI con nam trong khung Wave_...
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class WaveformGraphic : MaskableGraphic
    {
        public PatientModel patient;
        public WaveType wave = WaveType.ECG;

        [Header("Truc doc")]
        public float minValue = -0.5f;
        public float maxValue = 1.2f;

        [Header("Hien thi")]
        [Min(1f)] public float sweepSeconds = 5f;
        [Range(50, 1500)] public int samples = 600;
        [Min(0.5f)] public float lineWidth = 3f;
        [Range(0f, 0.2f)] public float gapFraction = 0.03f;

        float[] buffer;
        int write;
        int filled;
        float accumulator;
        float phase;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            Allocate();
        }

        void Allocate()
        {
            buffer = new float[Mathf.Max(2, samples)];
            write = 0;
            filled = 0;
            accumulator = 0f;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            if (patient == null)
            {
                patient = FindFirstObjectByType<PatientModel>();
                if (patient == null) return;
            }
            if (buffer == null || buffer.Length != samples) Allocate();

            float step = sweepSeconds / buffer.Length;
            accumulator += Time.deltaTime;

            int guard = 0;
            while (accumulator >= step && guard++ < buffer.Length)
            {
                accumulator -= step;
                phase += step * patient.GetRate(wave) / 60f;
                phase -= Mathf.Floor(phase);
                buffer[write] = patient.Evaluate(wave, phase);
                write = (write + 1) % buffer.Length;
                if (filled < buffer.Length) filled++;
            }
            if (accumulator >= step) accumulator = 0f; // bo qua neu frame bi giat qua lau

            SetVerticesDirty();
        }

        // Trong Edit mode: ve truoc mot doan song de xem bo cuc
        void FillPreview()
        {
            if (patient == null) patient = FindFirstObjectByType<PatientModel>();
            if (patient == null) return;
            if (buffer == null || buffer.Length != samples) buffer = new float[Mathf.Max(2, samples)];

            float step = sweepSeconds / buffer.Length;
            float p = 0f;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = patient.Evaluate(wave, p);
                p += step * patient.GetRate(wave) / 60f;
                p -= Mathf.Floor(p);
            }
            filled = buffer.Length;
            write = 0;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!Application.isPlaying) FillPreview();
            if (buffer == null || filled < 2) return;

            Rect r = GetPixelAdjustedRect();
            int n = buffer.Length;
            int gap = Application.isPlaying ? Mathf.Max(1, Mathf.RoundToInt(gapFraction * n)) : 0;
            float half = lineWidth * 0.5f;

            for (int i = 0; i < n - 1; i++)
            {
                int next = i + 1;
                if (filled < n)
                {
                    if (next >= filled) break; // chua ve toi day
                }
                else if (gap > 0)
                {
                    int d = (next - write + n) % n;
                    if (d < gap) continue; // khoang trong truoc dau ve
                }

                Vector2 a = ToPoint(r, i, n, buffer[i]);
                Vector2 b = ToPoint(r, next, n, buffer[next]);
                AddSegment(vh, a, b, half);
            }
        }

        Vector2 ToPoint(Rect r, int index, int n, float value)
        {
            float x = r.xMin + r.width * index / (n - 1f);
            float y = r.yMin + r.height * Mathf.Clamp01(Mathf.InverseLerp(minValue, maxValue, value));
            return new Vector2(x, y);
        }

        void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float half)
        {
            Vector2 dir = b - a;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
            dir.Normalize();
            Vector2 nrm = new Vector2(-dir.y, dir.x) * half;
            // keo dai moi doan mot chut de cac khop noi lien mach
            a -= dir * half * 0.5f;
            b += dir * half * 0.5f;

            int idx = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = color;
            v.position = a - nrm; vh.AddVert(v);
            v.position = a + nrm; vh.AddVert(v);
            v.position = b + nrm; vh.AddVert(v);
            v.position = b - nrm; vh.AddVert(v);
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx);
        }
    }
}
