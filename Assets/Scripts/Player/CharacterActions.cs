// Bang dong tac (ban web: ACT0, ACTS, ACT_DUR, actPose - dong 875-898, 1389).
// Moi dong tac la chuoi khung hinh [q 0..1, tu the, 'in' = tang toc (cu cham)].
// Tu the viet dang "khoa:gia tri,..." giong ban web cho de doi chieu; khoa thieu lay theo ACT0.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SereneIsle
{
    public static class CharacterActions
    {
        /// <summary>Thu tu khoa giong ChibiCharacter.K (POSE_KEYS cua ban web).</summary>
        public static readonly string[] Keys =
            { "hnR", "thL", "thR", "knL", "knR", "ftL", "ftR", "shL", "shR", "elL", "elR", "azL", "azR", "hipY", "hipR", "spR", "lean", "hdX", "hdY" };

        const string ACT0 = "hnR:0,shL:0,shR:0,elL:-.2,elR:-.2,azL:0,azR:0,spR:0,lean:.04,thL:0,thR:0,knL:.06,knR:.06,hipY:0,hipR:0,hdX:0,hdY:0";
        const string AXE_R = "shL:-.9,shR:-.95,elL:-.7,elR:-.7,azL:.35,azR:-.1,spR:.15,lean:.06,thL:-.22,thR:.2,knL:.22,knR:.22,hipR:.08";
        const string PICK_R = "shL:-1,shR:-1,elL:-.55,elR:-.55,azL:.3,azR:-.3,lean:.06,thL:-.18,thR:.16,knL:.16,knR:.16";
        const string EAT_A = "shR:-2.1,elR:-1.55,azR:-1.0,shL:-2.0,elL:-1.55,azL:1.0,hdX:.1";
        const string EAT_B = "shR:-2.2,elR:-1.5,azR:-1.05,shL:-2.1,elL:-1.5,azL:1.05,hdX:-.02";
        const string SIT = "thL:-1.45,thR:-1.45,knL:1.45,knR:1.45,hipY:-.27,lean:.06,shL:-.35,shR:-.35,elL:-.6,elR:-.6,hdX:-.05";
        const string BUILD_A = "shR:-1.45,elR:-1.3,lean:.25,knL:.4,knR:.4,thL:-.3,thR:-.3,hipY:-.03";
        const string BUILD_B = "shR:-.75,elR:-.45,lean:.3,knL:.4,knR:.4,thL:-.3,thR:-.3,hipY:-.03";

        /// <summary>Thoi luong (giay) - ACT_DUR.</summary>
        public static readonly Dictionary<string, float> Duration = new Dictionary<string, float>
        {
            ["eat"] = 1.15f, ["drink"] = 1.25f, ["shovel"] = .66f, ["axe"] = .62f, ["pick"] = .62f, ["hoe"] = .56f,
            ["can"] = .7f, ["seed"] = .5f, ["rod"] = .58f, ["net"] = .46f, ["build"] = .44f,
        };

        public struct Frame { public float q; public float[] pose; public bool easeIn; }

        static readonly Dictionary<string, Frame[]> _acts = new Dictionary<string, Frame[]>();
        static readonly float[] _act0 = Parse(ACT0, null);

        static CharacterActions()
        {
            Def("axe", (0, AXE_R, false), (.44f, "shL:-2.55,shR:-2.75,elL:-1.05,elR:-1.15,azL:.35,azR:.05,spR:.55,lean:-.1,thL:-.24,thR:.2,knL:.2,knR:.24,hipR:.25,hdX:-.1", false),
                (.64f, "shL:-.95,shR:-1.05,elL:-.2,elR:-.15,azL:.45,azR:-.2,spR:-.3,lean:.28,thL:-.32,thR:.26,knL:.38,knR:.32,hipR:-.15,hipY:-.02,hdX:.12", true), (1, AXE_R, false));
            Def("pick", (0, PICK_R, false), (.45f, "shL:-2.9,shR:-2.9,elL:-1,elR:-1,azL:.25,azR:-.25,lean:-.14,thL:-.18,thR:.16,knL:.1,knR:.1,hdX:-.15", false),
                (.64f, "shL:-.75,shR:-.75,elL:-.1,elR:-.1,azL:.35,azR:-.35,lean:.4,thL:-.45,thR:.06,knL:.46,knR:.46,hipY:-.03,hdX:.2", true), (1, PICK_R, false));
            Def("hoe", (0, PICK_R, false), (.45f, "shL:-2.6,shR:-2.6,elL:-.9,elR:-.9,azL:.25,azR:-.25,lean:-.1,thL:-.18,thR:.16,knL:.1,knR:.1", false),
                (.64f, "shL:-.5,shR:-.5,elL:-.05,elR:-.05,azL:.35,azR:-.35,lean:.48,thL:-.5,thR:.08,knL:.5,knR:.5,hipY:-.035,hdX:.25", true), (1, PICK_R, false));
            Def("shovel", (0, PICK_R, false), (.4f, "shL:-1.3,shR:-1.1,elL:-.9,elR:-.7,azL:.3,azR:-.3,lean:.1,thL:-.2,thR:.15,knL:.25,knR:.25", false),
                (.62f, "shL:-.55,shR:-.5,elL:-.15,elR:-.1,azL:.35,azR:-.35,lean:.5,thL:-.55,thR:.1,knL:.6,knR:.5,hipY:-.04,hdX:.25", true),
                (.82f, "shL:-1,shR:-.9,elL:-.6,elR:-.5,lean:.25,knL:.3,knR:.3", false), (1, PICK_R, false));
            Def("can", (0, "shR:-.9,elR:-.4,azR:-.1,lean:.06", false), (.3f, "shR:-1.15,elR:-.25,azR:-.1,hnR:.95,lean:.14,hdX:.2", false),
                (.78f, "shR:-1.15,elR:-.25,azR:-.1,hnR:1.05,lean:.14,hdX:.2", false), (1, "shR:-.9,elR:-.4,lean:.06", false));
            Def("seed", (0, "", false), (.42f, "knL:.75,knR:.75,thL:-.55,thR:-.55,hipY:-.05,lean:.48,shR:-.9,elR:-.35,shL:-.3,hdX:.25", false),
                (.62f, "knL:.75,knR:.75,thL:-.55,thR:-.55,hipY:-.05,lean:.48,shR:-1.2,elR:-.1,shL:-.3,hdX:.25", false), (1, "", false));
            Def("rod", (0, "shR:-1,elR:-.7,shL:-.7,elL:-.9", false), (.42f, "shR:-3,elR:-1.2,shL:-1.4,elL:-1,lean:-.12,spR:.3", false),
                (.6f, "shR:-1.1,elR:-.15,shL:-.9,elL:-.6,lean:.16,spR:-.1", true), (1, "shR:-1.05,elR:-.7,shL:-.75,elL:-.9", false));
            Def("net", (0, "shR:-1.2,elR:-.4,azR:.5,spR:.5", false), (.35f, "shR:-1.6,elR:-.6,azR:.75,spR:.95,hipR:.3", false),
                (.6f, "shR:-1.3,elR:-.1,azR:-.55,spR:-.75,lean:.15,hipR:-.25", true), (1, "shR:-.6,elR:-.3", false));
            Def("drink", (0, "", false), (.22f, "shR:-1.9,elR:-1.6,azR:-.8,hdX:0", false), (.4f, "shR:-2.2,elR:-1.5,azR:-1.0,hnR:.4,hdX:-.3,lean:-.05", false),
                (.82f, "shR:-2.3,elR:-1.45,azR:-1.0,hnR:.5,hdX:-.36,lean:-.06", false), (1, "", false));
            Def("eat", (0, "", false), (.2f, EAT_A, false), (.35f, EAT_B, false), (.5f, EAT_A, false), (.65f, EAT_B, false), (.8f, EAT_A, false), (1, "", false));
            Def("sit", (0, SIT, false), (1, SIT, false));
            Def("build", (0, BUILD_A, false), (.3f, BUILD_B, true), (.55f, BUILD_A, false), (.8f, BUILD_B, true), (1, "", false));
        }

        static void Def(string name, params (float q, string pose, bool easeIn)[] frames)
        {
            var arr = new Frame[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                arr[i] = new Frame { q = frames[i].q, pose = Parse(frames[i].pose, _act0), easeIn = frames[i].easeIn };
            _acts[name] = arr;
        }

        /// <summary>"khoa:gia tri,..." -&gt; mang theo Keys; thieu thi lay mac dinh (ACT0) hoac NaN neu khong co.</summary>
        static float[] Parse(string s, float[] defaults)
        {
            var r = new float[Keys.Length];
            for (int i = 0; i < r.Length; i++) r[i] = defaults != null ? defaults[i] : float.NaN;
            if (string.IsNullOrEmpty(s)) return r;
            foreach (var part in s.Split(','))
            {
                var kv = part.Split(':');
                int idx = Array.IndexOf(Keys, kv[0].Trim());
                if (idx < 0) throw new ArgumentException("Khoa tu the la: " + kv[0]);
                r[idx] = float.Parse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture);
            }
            return r;
        }

        public static bool Has(string act) => act != null && _acts.ContainsKey(act);

        public static float DurationOf(string act) => act != null && Duration.TryGetValue(act, out var d) ? d : .4f;

        /// <summary>actPose(K, q): noi suy giua 2 khung, smoothstep hoac t^3 khi 'in'. NaN = khong dieu khien khop do.</summary>
        public static void Sample(string act, float q, float[] result)
        {
            var K = _acts[act];
            int i = 0;
            while (i < K.Length - 2 && q > K[i + 1].q) i++;
            var a = K[i]; var b = K[i + 1];
            float span = b.q - a.q;
            float t = Math.Max(0, Math.Min(1, (q - a.q) / (span == 0 ? 1 : span)));
            t = b.easeIn ? t * t * t : t * t * (3 - 2 * t);
            for (int k = 0; k < result.Length; k++)
            {
                float va = a.pose[k], vb = b.pose[k];
                result[k] = float.IsNaN(va) || float.IsNaN(vb) ? float.NaN : va + (vb - va) * t;
            }
        }
    }
}
