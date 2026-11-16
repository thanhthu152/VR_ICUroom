
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ICUSim
{
    /// <summary>
    /// Monitor_screen
    /// Keywords: Tile_HR, Tile_SpO2, Tile_EtCO2, Tile_NIBP, Tile_RR, Tile_Temp (each tile contains Txt_Value),
    /// TopBar/AlarmBanner/Txt_Alarm and TopBar/Txt_Clock.
    /// </summary>
    public class MonitorDisplay : MonoBehaviour
    {
        public PatientModel patient;

        [Header("Alarm limitation")]
        public float lowSpO2Limit = 90f;
        public float highHRLimit = 120f;
        public float lowHRLimit = 50f;
        public float lowEtCO2Limit = 10f;
        public float highRRLimit = 30f;

        [Header("Non-invasive blood pressure")]
        [Tooltip("Time interval between blood pressure measurements in seconds (real monitors measure periodically)")]
        public float nibpIntervalSeconds = 15f;

        [Header("Colors")]
        public Color alarmTileColor = new Color(0.45f, 0.08f, 0.08f);
        public Color bannerColor = new Color(0.776f, 0.157f, 0.157f);
        public float blinkHz = 1.5f;

        TMP_Text hr, spo2, etco2, nibp, rr, temp, alarmText, clock;
        Image hrTile, spo2Tile, etco2Tile, rrTile;
        Color hrBase, spo2Base, etco2Base, rrBase;
        GameObject banner;
        Image bannerImage;
        float nextNibp;

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        void Awake()
        {
            if (patient == null) patient = FindFirstObjectByType<PatientModel>();

            hr    = Txt("Tile_HR");
            spo2  = Txt("Tile_SpO2");
            etco2 = Txt("Tile_EtCO2");
            nibp  = Txt("Tile_NIBP");
            rr    = Txt("Tile_RR");
            temp  = Txt("Tile_Temp");

            hrTile    = Img("Tile_HR");    hrBase    = hrTile    ? hrTile.color    : Color.white;
            spo2Tile  = Img("Tile_SpO2");  spo2Base  = spo2Tile  ? spo2Tile.color  : Color.white;
            etco2Tile = Img("Tile_EtCO2"); etco2Base = etco2Tile ? etco2Tile.color : Color.white;
            rrTile    = Img("Tile_RR");    rrBase    = rrTile    ? rrTile.color    : Color.white;

            Transform b = transform.Find("TopBar/AlarmBanner");
            if (b != null)
            {
                banner = b.gameObject;
                bannerImage = b.GetComponent<Image>();
                Transform t = b.Find("Txt_Alarm");
                if (t != null) alarmText = t.GetComponent<TMP_Text>();
            }
            else Debug.LogWarning($"[MonitorDisplay] Could not find 'TopBar/AlarmBanner' in {name}");

            Transform c = transform.Find("TopBar/Txt_Clock");
            if (c != null) clock = c.GetComponent<TMP_Text>();
        }

        void Update()
        {
            if (patient == null) return;
            VitalSet v = patient.current;

            Set(hr,    v.heartRate,   "0");
            Set(spo2,  v.spO2,        "0");
            Set(etco2, v.etCO2,       "0");
            Set(rr,    v.respRate,    "0");
            Set(temp,  v.temperature, "0.0");

            // Blood pressure is updated only when a measurement is taken
            if (nibp != null && Time.time >= nextNibp)
            {
                nextNibp = Time.time + nibpIntervalSeconds;
                int s = Mathf.RoundToInt(v.systolic);
                int d = Mathf.RoundToInt(v.diastolic);
                int m = Mathf.RoundToInt(patient.MeanArterialPressure);
                nibp.text = $"{s}/{d} ({m})";
            }

            if (clock != null)
            {
                string now = DateTime.Now.ToString("HH:mm", Inv);
                if (clock.text != now) clock.text = now;
            }

            bool loSpO2 = v.spO2 < lowSpO2Limit;
            bool loCO2  = v.etCO2 < lowEtCO2Limit;
            bool hiHR   = v.heartRate > highHRLimit;
            bool loHR   = v.heartRate < lowHRLimit;
            bool hiRR   = v.respRate > highRRLimit;

            Tint(spo2Tile,  spo2Base,  loSpO2);
            Tint(etco2Tile, etco2Base, loCO2);
            Tint(hrTile,    hrBase,    hiHR || loHR);
            Tint(rrTile,    rrBase,    hiRR);

            string msg = loSpO2 ? "SpO2 LOW"
                       : loCO2  ? "EtCO2 LOW"
                       : hiHR   ? "HR HIGH"
                       : loHR   ? "HR LOW"
                       : hiRR   ? "RR HIGH"
                       : null;
            UpdateBanner(msg);
        }

        void UpdateBanner(string msg)
        {
            if (banner == null) return;
            bool on = !string.IsNullOrEmpty(msg);
            if (banner.activeSelf != on) banner.SetActive(on);
            if (!on) return;
            if (alarmText != null && alarmText.text != msg) alarmText.text = msg;
            if (bannerImage != null)
            {
                float k = Mathf.PingPong(Time.time * blinkHz * 2f, 1f);
                bannerImage.color = Color.Lerp(bannerColor * 0.45f, bannerColor, k);
            }
        }

        void Tint(Image img, Color baseColor, bool alarm)
        {
            if (img == null) return;
            img.color = alarm ? alarmTileColor : baseColor;
        }

        static void Set(TMP_Text t, float value, string format)
        {
            if (t == null) return;
            string s = value.ToString(format, Inv);
            if (t.text != s) t.text = s;
        }

        TMP_Text Txt(string tile)
        {
            Transform t = transform.Find(tile + "/Txt_Value");
            if (t == null) { Debug.LogWarning($"[MonitorDisplay] Could not find '{tile}/Txt_Value'"); return null; }
            return t.GetComponent<TMP_Text>();
        }

        Image Img(string tile)
        {
            Transform t = transform.Find(tile);
            return t != null ? t.GetComponent<Image>() : null;
        }
    }
}
