// Mo hinh thu co hoat canh (Quaternius Ultimate Animated Animal Pack, CC0). Tao boi menu
// "Serene Isle > Nhap thu Quaternius" (Editor/AnimalImporter.cs), luu o Assets/Resources/AnimalLibrary.asset.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class AnimalLibrary : ScriptableObject
    {
        [Serializable]
        public class Model
        {
            public string id;                 // Bull, Cow, Deer, ShibaInu
            public GameObject prefab;
            public Avatar avatar;
            public List<AnimationClip> clips = new List<AnimationClip>(); // ten da bo "AnimalArmature|"
        }

        public List<Model> models = new List<Model>();

        static AnimalLibrary _instance;
        public static AnimalLibrary Instance
        {
            get
            {
                if (!_instance) _instance = Resources.Load<AnimalLibrary>("AnimalLibrary");
                return _instance;
            }
        }

        public Model Get(string id)
        {
            foreach (var m in models) if (m.id == id) return m;
            return null;
        }

        public static AnimationClip Clip(Model m, string name)
        {
            if (m == null) return null;
            foreach (var c in m.clips) if (c && (c.name == name || c.name.EndsWith("|" + name))) return c;
            return null;
        }
    }
}
