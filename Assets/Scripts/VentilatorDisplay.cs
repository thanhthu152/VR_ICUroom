using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ICUSim
{
    /// <summary>
    /// Gan vao Ventilator_screen (Canvas cua may tho).
    /// Tu tim cac o theo ten: Tile_Ppeak, Tile_VTe, Tile_fTotal, Tile_MVe, Tile_FiO2, Tile_PEEP
    /// (moi o co con Txt_Value) va TopBar/AlarmBanner/Txt_Alarm.
    /// </summary>
    public class VentilatorDisplay : MonoBehaviour
    {
        public PatientModel patient;

        [Header("Gioi han bao dong")]
        public float highPressureLimit = 40f;      // cmH2O
        public float lowExhaledVolumeLimit = 300f; // mL
        public float highRespRateLimit = 30f;      // /min

        [Header("Mau")]
        public Color alarmTileColor = new Color(0.55f, 0.10f, 0.10f);
        public Color bannerColor = new Color(0.776f, 0.157f, 0.157f);
        public float blinkHz = 1.5f;

        TMP_Text ppeak, vte, ftotal, mve, fio2, peep, alarmText;
        Image ppeakTile, vteTile, ftotalTile;
        Color ppeakBase, vteBase, ftotalBase;
        GameObject banner;
        Image bannerImage;

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        void Awake()
        {
            if (patient == null) patient = FindFirstObjectByType<PatientModel>();

            ppeak  = Txt("Tile_Ppeak");
            vte    = Txt("Tile_VTe");
            ftotal = Txt("Tile_fTotal");
            mve    = Txt("Tile_MVe");
            fio2   = Txt("Tile_FiO2");
            peep   = Txt("Tile_PEEP");

            ppeakTile  = Img("Tile_Ppeak");  ppeakBase  = ppeakTile  ? ppeakTile.color  : Color.white;
            vteTile    = Img("Tile_VTe");    vteBase    = vteTile    ? vteTile.color    : Color.white;
            ftotalTile = Img("Tile_fTotal"); ftotalBase = ftotalTile ? ftotalTile.color : Color.white;

            Transform b = transform.Find("TopBar/AlarmBanner");
            if (b != null)
            {
                banner = b.gameObject;
                bannerImage = b.GetComponent<Image>();
                Transform t = b.Find("Txt_Alarm");
                if (t != null) alarmText = t.GetComponent<TMP_Text>();
            }
            else Debug.LogWarning($"[VentilatorDisplay] Khong tim thay 'TopBar/AlarmBanner' trong {name}");
        }

        void Update()
        {
            if (patient == null) return;
            VitalSet v = patient.current;

            Set(ppeak,  v.peakPressure,   "0");
            Set(vte,    v.tidalVolumeExp, "0");
            Set(ftotal, v.respRate,       "0");
            Set(mve,    patient.MinuteVolume, "0.0");
            Set(fio2,   v.fiO2,           "0");
            Set(peep,   v.peep,           "0");

            bool hiP = v.peakPressure > highPressureLimit;
            bool loV = v.tidalVolumeExp < lowExhaledVolumeLimit;
            bool hiR = v.respRate > highRespRateLimit;

            Tint(ppeakTile,  ppeakBase,  hiP);
            Tint(vteTile,    vteBase,    loV);
            Tint(ftotalTile, ftotalBase, hiR);

            string msg = hiP ? "HIGH AIRWAY PRESSURE"
                       : loV ? "LOW EXHALED VOLUME"
                       : hiR ? "HIGH RESPIRATORY RATE"
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
            if (t == null) { Debug.LogWarning($"[VentilatorDisplay] Khong tim thay '{tile}/Txt_Value'"); return null; }
            return t.GetComponent<TMP_Text>();
        }

        Image Img(string tile)
        {
            Transform t = transform.Find(tile);
            return t != null ? t.GetComponent<Image>() : null;
        }
    }
}
