// Chi so nhan vat (ban web: statsUpdate, useEnergy, ENR, consume, EMAX).
// Tinh theo gio trong game: ban web 1 giay that = 2 phut game.
//  - Nuoc: -5 / gio.
//  - Nang luong (kiem doi): -10% EMAX / gio  => 7h-12h khong an con 50%.
//    Chay (Shift) ton gap doi; 80% phan ton khi chay / dung dung cu duoc tra lai sau ~1 giay nghi, toc do 12% EMAX / giay.
//  - Mau: -1 / gio; them -7 khi het nuoc, -5 khi het nang luong.
using System;

namespace SereneIsle
{
    public sealed class PlayerStats
    {
        public float Hp = 100, Water = 100, Energy = 100, Calm = 0;
        /// <summary>Thuong nang luong toi da (nha +20, giay phep the luc...) - cac giai doan sau.</summary>
        public float EnergyBonus;
        public float EnergyMax => 100 + EnergyBonus;

        /// <summary>Phan nang luong se hoi lai (ban web: ENR.rec).</summary>
        public float Recover { get; private set; }
        float _restTime;

        /// <summary>Canh bao (toast) khi chi so tut duoi 20.</summary>
        public event Action<string> Warning;
        bool _warnWater, _warnEnergy, _warnHp;

        public bool Dead => Hp <= 0;

        /// <param name="dt">giay that (da nhan he so ngoi nghi)</param>
        /// <param name="gameHours">so gio game da troi qua</param>
        public void Tick(float dt, float gameHours, bool sprinting, bool sitting = false, int calmLevel = 0, bool staminaLicence2 = false)
        {
            Water = Math.Max(0, Water - 5 * gameHours);

            float baseDrain = EnergyMax * .1f * gameHours * (calmLevel >= 2 ? .8f : 1f) * (sitting ? .3f : 1f);
            float sprintDrain = sprinting && !staminaLicence2 ? baseDrain : 0;
            Energy = Math.Max(0, Energy - baseDrain - sprintDrain);
            if (sprintDrain > 0) { Recover += sprintDrain * .8f; _restTime = 0; }
            _restTime += dt;
            if (_restTime > 1 && Recover > 0)
            {
                float r = Math.Min(Recover, dt * EnergyMax * .12f * (sitting ? 2 : 1));
                Recover -= r;
                Energy = Math.Min(EnergyMax, Energy + r);
            }
            Recover = Math.Min(Recover, Math.Max(0, EnergyMax - Energy));

            float dh = -1;
            if (Water <= 0) dh -= 7;
            if (Energy <= 0) dh -= 5;
            Hp = Math.Max(0, Math.Min(100, Hp + dh * gameHours));

            Warn(ref _warnWater, Water, "Khát nước quá… Tìm ao suối hoặc uống dừa, chai nước.");
            Warn(ref _warnEnergy, Energy, "Đói và mệt rồi. Ăn gì đó đi.");
            Warn(ref _warnHp, Hp, "Máu thấp! Ăn uống ngay kẻo ngất.");
        }

        void Warn(ref bool flag, float v, string msg)
        {
            if (v < 20 && !flag) { flag = true; Warning?.Invoke(msg); }
            if (v > 30) flag = false;
        }

        /// <summary>Dung dung cu: tru nang luong, 80% se hoi lai sau khi nghi.</summary>
        public bool UseEnergy(float n)
        {
            if (Energy < n) { Warning?.Invoke("Quá mệt! Ăn gì đó hoặc đi ngủ."); return false; }
            Energy = Math.Max(0, Energy - n);
            Recover += n * .8f;
            _restTime = 0;
            return true;
        }

        public void Consume(ItemDefinition it)
        {
            if (!it) return;
            Hp = Math.Min(100, Hp + it.eatHp);
            Water = Math.Min(100, Water + it.eatWater);
            Energy = Math.Min(EnergyMax, Energy + it.eatEnergy);
            Recover = Math.Min(Recover, Math.Max(0, EnergyMax - Energy));
        }

        public void ClearRecover() { Recover = 0; _restTime = 0; }
    }
}
