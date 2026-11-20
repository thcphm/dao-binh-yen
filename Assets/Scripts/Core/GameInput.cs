// Input cua game, chi dung Input System moi. Phim mac dinh giong ban web:
// WASD/mui ten di, Shift chay, Space nhay, keo chuot phai/giua xoay camera, lan chuot hoac +/- zoom, Z/C xoay camera.
// Tay cam: stick trai di, stick phai xoay, A nhay, LT/B chay.
// Moi hanh dong la 1 InputAction -> giai doan 5 dung InputActionRebindingExtensions de doi phim
// va luu bang SaveBindingOverridesAsJson().
using UnityEngine;
using UnityEngine.InputSystem;

namespace SereneIsle
{
    public static class GameInput
    {
        public static InputActionMap Map { get; private set; }
        public static InputAction MoveAction { get; private set; }
        public static InputAction RunAction { get; private set; }
        public static InputAction JumpAction { get; private set; }
        public static InputAction OrbitAction { get; private set; }     // giu chuot phai/giua
        public static InputAction LookDeltaAction { get; private set; } // do dich chuot (pixel)
        public static InputAction LookStickAction { get; private set; } // stick phai
        public static InputAction ScrollAction { get; private set; }
        public static InputAction ZoomKeyAction { get; private set; }   // + / -
        public static InputAction RotateKeyAction { get; private set; } // Z / C

        static bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (Map != null) { Map.Disable(); Map.Dispose(); }
            Map = null;
            _ready = false;
        }

        static void Ensure()
        {
            if (_ready) return;
            _ready = true;
            Map = new InputActionMap("Gameplay");

            MoveAction = Map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            MoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            MoveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            MoveAction.AddBinding("<Gamepad>/leftStick");

            RunAction = Map.AddAction("Run", InputActionType.Button);
            RunAction.AddBinding("<Keyboard>/leftShift");
            RunAction.AddBinding("<Keyboard>/rightShift");
            RunAction.AddBinding("<Gamepad>/leftTrigger");
            RunAction.AddBinding("<Gamepad>/buttonEast");

            JumpAction = Map.AddAction("Jump", InputActionType.Button);
            JumpAction.AddBinding("<Keyboard>/space");
            JumpAction.AddBinding("<Gamepad>/buttonSouth");

            OrbitAction = Map.AddAction("Orbit", InputActionType.Button);
            OrbitAction.AddBinding("<Mouse>/rightButton");
            OrbitAction.AddBinding("<Mouse>/middleButton");

            LookDeltaAction = Map.AddAction("LookDelta", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            LookStickAction = Map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");
            ScrollAction = Map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y", expectedControlLayout: "Axis");

            ZoomKeyAction = Map.AddAction("ZoomKey", InputActionType.Value, expectedControlLayout: "Axis");
            ZoomKeyAction.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/equals").With("Negative", "<Keyboard>/minus");
            ZoomKeyAction.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/numpadPlus").With("Negative", "<Keyboard>/numpadMinus");

            RotateKeyAction = Map.AddAction("RotateKey", InputActionType.Value, expectedControlLayout: "Axis");
            RotateKeyAction.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/z").With("Negative", "<Keyboard>/c");

            Map.Enable();
        }

        /// <summary>x = phai, y = toi (stick co the &lt; 1).</summary>
        public static Vector2 Move { get { Ensure(); return Vector2.ClampMagnitude(MoveAction.ReadValue<Vector2>(), 1f); } }
        public static bool Run { get { Ensure(); return RunAction.IsPressed(); } }
        public static bool JumpPressed { get { Ensure(); return JumpAction.WasPressedThisFrame(); } }
        public static bool OrbitHeld { get { Ensure(); return OrbitAction.IsPressed(); } }
        /// <summary>Do dich chuot (pixel) frame nay, y duong = len.</summary>
        public static Vector2 MouseDelta { get { Ensure(); return LookDeltaAction.ReadValue<Vector2>(); } }
        public static Vector2 LookStick { get { Ensure(); return LookStickAction.ReadValue<Vector2>(); } }

        /// <summary>-1 / 0 / +1 moi nac lan chuot (duong = lan len).</summary>
        public static float ScrollSteps
        {
            get { Ensure(); float y = ScrollAction.ReadValue<float>(); return Mathf.Abs(y) < .01f ? 0 : Mathf.Sign(y); }
        }

        /// <summary>+1 khi vua bam '=' / '+', -1 khi vua bam '-'.</summary>
        public static float ZoomKeySteps
        {
            get { Ensure(); return ZoomKeyAction.WasPressedThisFrame() ? Mathf.Sign(ZoomKeyAction.ReadValue<float>()) : 0; }
        }

        /// <summary>Z = +1, C = -1 (giu phim).</summary>
        public static float CameraRotateKeys { get { Ensure(); return RotateKeyAction.ReadValue<float>(); } }
    }
}
