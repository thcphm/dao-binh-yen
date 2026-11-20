// Danh sach mo hinh nhan vat + animation (KayKit, CC0). Tao boi menu "Serene Isle > Nhap nhan vat KayKit"
// (Editor/CharacterImporter.cs), luu o Assets/Resources/CharacterLibrary.asset de nap luc chay (ca WebGL).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SereneIsle
{
    public class CharacterLibrary : ScriptableObject
    {
        [Serializable]
        public class Model
        {
            public string id;            // "Rogue", "Ranger"
            public GameObject prefab;    // FBX (rig Generic, mesh doc duoc)
            public Avatar avatar;
            public Texture2D palette;    // anh bang mau 8x4 o (doc duoc)
        }

        [Serializable]
        public class Clip
        {
            public AnimationClip clip;
            [Tooltip("Toc do di chuyen cua dong tac o ti le goc (don vi/giay), 0 = dung tai cho")]
            public float groundSpeed;
        }

        public List<Model> models = new List<Model>();
        public List<Clip> clips = new List<Clip>();

        Dictionary<string, Clip> _byName;

        static CharacterLibrary _instance;
        public static CharacterLibrary Instance
        {
            get
            {
                if (!_instance) _instance = Resources.Load<CharacterLibrary>("CharacterLibrary");
                return _instance;
            }
        }

        public Model GetModel(string id)
        {
            foreach (var m in models) if (m.id == id) return m;
            return models.Count > 0 ? models[0] : null;
        }

        public Clip GetClip(string name)
        {
            if (_byName == null || _byName.Count != clips.Count)
            {
                _byName = new Dictionary<string, Clip>();
                foreach (var c in clips) if (c.clip) _byName[c.clip.name] = c;
            }
            return _byName.TryGetValue(name, out var r) ? r : null;
        }
    }
}
