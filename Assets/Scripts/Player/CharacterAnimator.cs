// Phat animation KayKit bang Playables (khong can AnimatorController): tron muot giua cac dong tac (crossfade).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SereneIsle
{
    public sealed class CharacterAnimator
    {
        const int Slots = 4;

        PlayableGraph _graph;
        AnimationMixerPlayable _mixer;
        readonly AnimationClipPlayable[] _play = new AnimationClipPlayable[Slots];
        readonly float[] _w = new float[Slots];
        readonly string[] _name = new string[Slots];
        int _cur = -1;
        float _fade = .2f;

        public string Current => _cur >= 0 ? _name[_cur] : null;
        /// <summary>Thoi gian (giay, theo toc do phat) ke tu khi bat dau dong tac hien tai.</summary>
        public double CurrentTime => _cur >= 0 ? _play[_cur].GetTime() : 0;
        public bool Valid => _graph.IsValid();

        public CharacterAnimator(Animator animator, string graphName)
        {
            _graph = PlayableGraph.Create(graphName);
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            _mixer = AnimationMixerPlayable.Create(_graph, Slots);
            var output = AnimationPlayableOutput.Create(_graph, "Anim", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        public void Destroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
        }

        /// <summary>Chuyen sang dong tac 'clip'. restart = phat lai tu dau neu dang chay chinh dong tac nay.</summary>
        public void Play(AnimationClip clip, float fade = .2f, float speed = 1f, bool restart = false)
        {
            if (!clip || !_graph.IsValid()) return;
            if (_cur >= 0 && _name[_cur] == clip.name)
            {
                _play[_cur].SetSpeed(speed);
                if (restart) _play[_cur].SetTime(0);
                return;
            }
            // tim o trong / co trong so nho nhat de dat dong tac moi
            int slot = -1; float minW = float.MaxValue;
            for (int i = 0; i < Slots; i++)
            {
                if (i == _cur) continue;
                if (!_play[i].IsValid()) { slot = i; break; }
                if (_w[i] < minW) { minW = _w[i]; slot = i; }
            }
            if (_play[slot].IsValid())
            {
                _graph.Disconnect(_mixer, slot);
                _play[slot].Destroy();
            }
            var p = AnimationClipPlayable.Create(_graph, clip);
            p.SetApplyFootIK(false);
            p.SetSpeed(speed);
            _graph.Connect(p, 0, _mixer, slot);
            _play[slot] = p;
            _name[slot] = clip.name;
            _w[slot] = _cur < 0 ? 1 : 0;
            _cur = slot;
            _fade = Mathf.Max(.001f, fade);
            Apply();
        }

        public void SetSpeed(float speed)
        {
            if (_cur >= 0) _play[_cur].SetSpeed(speed);
        }

        /// <summary>Goi moi khung: tang trong so dong tac hien tai, giam cac dong tac cu.</summary>
        public void Update(float dt)
        {
            if (_cur < 0) return;
            float step = dt / _fade;
            for (int i = 0; i < Slots; i++)
            {
                if (!_play[i].IsValid()) continue;
                _w[i] = Mathf.MoveTowards(_w[i], i == _cur ? 1 : 0, step);
            }
            Apply();
        }

        void Apply()
        {
            float sum = 0;
            for (int i = 0; i < Slots; i++) sum += _play[i].IsValid() ? _w[i] : 0;
            for (int i = 0; i < Slots; i++)
                _mixer.SetInputWeight(i, _play[i].IsValid() && sum > 0 ? _w[i] / sum : 0);
        }
    }
}
