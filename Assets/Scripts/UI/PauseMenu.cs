// Menu tam dung (Esc) - ban web chi mo cai dat; ban nay them luu game, ve menu chinh, thoat.
// Khong mo neu Esc vua dong 1 bang khac trong cung khung hinh, hoac dang cau ca.
using UnityEngine;
using UnityEngine.UIElements;
using static SereneIsle.MenuPanels;

namespace SereneIsle
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public bool IsOpen => _overlay != null && _overlay.parent != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        VisualElement _overlay, _card;
        int _openedFrame = -10;

        GameSession S => GameSession.Instance;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var s = S;
            if (!s || !s.Running) return;
            if (IsOpen)
            {
                if (GameInput.CancelPressed && Time.frameCount > _openedFrame) Close();
                return;
            }
            var ts = ToolSystem.Instance;
            bool busy = ts && ts.Fishing && ts.Fishing.Busy;
            if (GameInput.CancelPressed && s.Active && !busy && Time.frameCount - s.UiClosedFrame > 1) Open();
        }

        public void Open()
        {
            var hud = GameHud.Instance;
            if (!hud || hud.Root == null || IsOpen) return;
            var ss = Resources.Load<StyleSheet>("UI/Menu");
            if (ss && !hud.Root.styleSheets.Contains(ss)) hud.Root.styleSheets.Add(ss);
            _openedFrame = Time.frameCount;
            _overlay = Overlay(hud.Root, out _card, Close, "pause-card");
            S.UiOpen = true;
            Main();
        }

        void Main()
        {
            var s = S;
            _card.Clear();
            _card.style.width = StyleKeyword.Null;
            _card.Add(L("panel-title", "Tạm dừng"));
            var date = s.Date;
            _card.Add(L("panel-sub", $"{s.PlayerName} · {LunarCalendar.SeasonName(date.Season)} · {date.ShortLabel} · {s.Clock.TimeLabel}"));
            _card.Add(Btn("Tiếp tục chơi", Close));
            _card.Add(Btn("Lưu game", () =>
            {
                bool ok = s.SaveSlot >= 0 && s.SaveNow();
                s.Say(ok ? $"Đã lưu vào đảo {s.SaveSlot + 1}." : "Chế độ thử nghiệm: không lưu.");
                Close();
            }));
            _card.Add(Btn("Cài đặt", () => Settings(_card, Main, () => { Close(); if (GameBootstrap.Instance) GameBootstrap.Instance.BackToMenu(); })));
            _card.Add(Btn("Ghi công", () => Credits(_card, Main), "muted"));
            _card.Add(Btn("Ủng hộ tác giả", () => Donate(_card, Main), "muted"));
            _card.Add(Btn("Lưu và về menu chính", () => { Close(); if (GameBootstrap.Instance) GameBootstrap.Instance.BackToMenu(); }, "muted"));
            if (!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer) _card.Add(Btn("Lưu và thoát game", () => { if (s.SaveSlot >= 0) s.SaveNow(); Application.Quit(); }, "danger"));
        }

        public void Close()
        {
            if (!IsOpen) return;
            _overlay.RemoveFromHierarchy();
            _overlay = null;
            if (S) S.UiOpen = false;
        }
    }
}
