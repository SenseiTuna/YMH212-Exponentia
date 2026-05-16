/*
 * PROJ_NAME: Exponentia
 * PROJ_ID: EXP-ROGUELITE-001
 * VERSION: 0.5.0
 * BUILD_DATE: 2026-05-01
 * BUILD_TIME: 18:45
 * DESCRIPTION: Player locomotion and action event gateway with central input support.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using Exponentia.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public enum ControlScheme
    {
        KeyboardMouse,
        XboxController,
        PlayStationController,
        GenericGamepad
    }

    public enum GamepadActionButton
    {
        South,
        East,
        West,
        North,
        LeftShoulder,
        RightShoulder,
        Start
    }

    [System.Serializable]
    private struct KeyboardBindings
    {
        public Key interact;
        public Key attack;
        public Key dodge;
    }

    [System.Serializable]
    private struct GamepadBindings
    {
        public GamepadActionButton interact;
        public GamepadActionButton attack;
        public GamepadActionButton dodge;
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gamepadMoveDeadzone = 0.1f;
    [SerializeField] private bool useCentralInput = true;
    [SerializeField] private PlayerInputReader inputReader;

    [Header("Keyboard Bindings")]
    [SerializeField] private KeyboardBindings keyboardBindings = new KeyboardBindings
    {
        interact = Key.E,
        attack = Key.Space,
        dodge = Key.LeftShift
    };

    [Header("Xbox Bindings")]
    [SerializeField] private GamepadBindings xboxBindings = new GamepadBindings
    {
        interact = GamepadActionButton.West,
        attack = GamepadActionButton.South,
        dodge = GamepadActionButton.East
    };

    [Header("PlayStation Bindings")]
    [SerializeField] private GamepadBindings playStationBindings = new GamepadBindings
    {
        interact = GamepadActionButton.West,
        attack = GamepadActionButton.South,
        dodge = GamepadActionButton.East
    };

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Gamepad activeGamepad;
    private Vector2 lastMoveDirection = Vector2.right;

    public ControlScheme CurrentControlScheme { get; private set; } = ControlScheme.KeyboardMouse;
    public bool InteractPressedThisFrame { get; private set; }
    public bool AttackPressedThisFrame { get; private set; }
    public bool DodgePressedThisFrame { get; private set; }
    public Vector2 LastMoveDirection => lastMoveDirection;

    public event System.Action OnInteractPressed;
    public event System.Action OnAttackPressed;
    public event System.Action OnDodgePressed;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        ResolveInputReaderIfNeeded();
    }

    private void Update()
    {
        ResolveInputReaderIfNeeded();
        UpdateControlScheme();
        moveInput = ReadMovementInput().normalized;
        UpdateLastMoveDirection();
        ReadActionButtons();
        EmitActionEvents();

        if (rb == null)
        {
            transform.Translate(moveInput * moveSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = Mathf.Max(0f, newMoveSpeed);
    }

    private void ResolveInputReaderIfNeeded()
    {
        if (inputReader != null)
        {
            return;
        }

        inputReader = GetComponent<PlayerInputReader>();
        if (inputReader == null && useCentralInput)
        {
            // Turkish: Prefab'da yoksa runtime'da ekleyerek merkezi input gecisini kirilmadan aciyoruz.
            inputReader = gameObject.AddComponent<PlayerInputReader>();
        }
    }

    private bool ShouldUseCentralInput()
    {
        return useCentralInput && inputReader != null;
    }

    private Vector2 ReadMovementInput()
    {
        if (ShouldUseCentralInput())
        {
            // Turkish: Hareket verisini merkezi input katmanindan cekiyoruz.
            return inputReader.MoveValue;
        }

        return ReadMovementByCurrentScheme();
    }

    private void ReadActionButtons()
    {
        if (ShouldUseCentralInput())
        {
            ReadActionButtonsFromCentralInput();
            return;
        }

        ReadActionButtonsByCurrentScheme();
    }

    private void ReadActionButtonsFromCentralInput()
    {
        // Turkish: Tek-frame aksiyonlari consume ederek eventlerin bir karede tek kez tetiklenmesini garanti ediyoruz.
        InteractPressedThisFrame = inputReader.ConsumeInteractPressedThisFrame();
        AttackPressedThisFrame = inputReader.ConsumeAttackPressedThisFrame();
        DodgePressedThisFrame = inputReader.ConsumeDashPressedThisFrame();
    }

    private void UpdateLastMoveDirection()
    {
        if (moveInput.sqrMagnitude > 0.001f)
        {
            lastMoveDirection = moveInput;
        }
    }

    private void UpdateControlScheme()
    {
        if (ShouldUseCentralInput())
        {
            UpdateControlSchemeFromInputDevices();
            return;
        }

        UpdateControlSchemeLegacy();
    }

    private void UpdateControlSchemeLegacy()
    {
        // Keyboard oncekiklendigi surece gamepad stick drift'i klavyeye gecisi engellemez.
        if (IsKeyboardInputActive())
        {
            CurrentControlScheme = ControlScheme.KeyboardMouse;
            return;
        }

        Gamepad recentlyUsedPad = GetRecentlyUsedGamepad();
        if (recentlyUsedPad != null)
        {
            activeGamepad = recentlyUsedPad;
            CurrentControlScheme = DetectGamepadScheme(recentlyUsedPad);
            return;
        }

        if (activeGamepad != null)
        {
            CurrentControlScheme = DetectGamepadScheme(activeGamepad);
        }
    }

    private void UpdateControlSchemeFromInputDevices()
    {
        // Turkish: Input asset kullansak da aktif cihazi cihaz sinifina bakarak anlik tespit ediyoruz.
        if (Keyboard.current != null && Keyboard.current.anyKey.isPressed)
        {
            CurrentControlScheme = ControlScheme.KeyboardMouse;
            return;
        }

        Gamepad recentlyUsedPad = GetRecentlyUsedGamepad();
        if (recentlyUsedPad != null)
        {
            activeGamepad = recentlyUsedPad;
            CurrentControlScheme = DetectGamepadScheme(recentlyUsedPad);
            return;
        }

        if (activeGamepad != null)
        {
            CurrentControlScheme = DetectGamepadScheme(activeGamepad);
        }
    }

    private Vector2 ReadMovementByCurrentScheme()
    {
        switch (CurrentControlScheme)
        {
            case ControlScheme.XboxController:
                return ReadXboxMovement();
            case ControlScheme.PlayStationController:
                return ReadPlayStationMovement();
            case ControlScheme.GenericGamepad:
                return ReadGenericGamepadMovement();
            default:
                return ReadKeyboardMovement();
        }
    }

    private void ReadActionButtonsByCurrentScheme()
    {
        InteractPressedThisFrame = false;
        AttackPressedThisFrame = false;
        DodgePressedThisFrame = false;

        switch (CurrentControlScheme)
        {
            case ControlScheme.XboxController:
                ReadXboxActionButtons();
                break;
            case ControlScheme.PlayStationController:
                ReadPlayStationActionButtons();
                break;
            case ControlScheme.GenericGamepad:
                ReadGenericGamepadActionButtons();
                break;
            default:
                ReadKeyboardActionButtons();
                break;
        }
    }

    private Vector2 ReadKeyboardMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        return new Vector2(x, y);
    }

    private Vector2 ReadXboxMovement()
    {
        return ReadGamepadStick(activeGamepad);
    }

    private Vector2 ReadPlayStationMovement()
    {
        return ReadGamepadStick(activeGamepad);
    }

    private Vector2 ReadGenericGamepadMovement()
    {
        return ReadGamepadStick(activeGamepad);
    }

    private void ReadKeyboardActionButtons()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        InteractPressedThisFrame = keyboard[keyboardBindings.interact].wasPressedThisFrame;
        AttackPressedThisFrame = keyboard[keyboardBindings.attack].wasPressedThisFrame;
        DodgePressedThisFrame = keyboard[keyboardBindings.dodge].wasPressedThisFrame;
    }

    private void ReadXboxActionButtons()
    {
        ReadGamepadActionButtons(activeGamepad, xboxBindings);
    }

    private void ReadPlayStationActionButtons()
    {
        ReadGamepadActionButtons(activeGamepad, playStationBindings);
    }

    private void ReadGenericGamepadActionButtons()
    {
        ReadGamepadActionButtons(activeGamepad, xboxBindings);
    }

    private void ReadGamepadActionButtons(Gamepad gamepad, GamepadBindings bindings)
    {
        if (gamepad == null)
            return;

        InteractPressedThisFrame = IsGamepadActionPressedThisFrame(gamepad, bindings.interact);
        AttackPressedThisFrame = IsGamepadActionPressedThisFrame(gamepad, bindings.attack);
        DodgePressedThisFrame = IsGamepadActionPressedThisFrame(gamepad, bindings.dodge);
    }

    private void EmitActionEvents()
    {
        if (InteractPressedThisFrame) OnInteractPressed?.Invoke();
        if (AttackPressedThisFrame) OnAttackPressed?.Invoke();
        if (DodgePressedThisFrame) OnDodgePressed?.Invoke();
    }

    private Vector2 ReadGamepadStick(Gamepad gamepad)
    {
        if (gamepad == null)
            return Vector2.zero;

        Vector2 move = gamepad.leftStick.ReadValue();
        if (move.sqrMagnitude < gamepadMoveDeadzone * gamepadMoveDeadzone)
            return Vector2.zero;

        return move;
    }

    private Gamepad GetRecentlyUsedGamepad()
    {
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            Gamepad pad = Gamepad.all[i];
            if (pad == null)
                continue;

            Vector2 leftStick = pad.leftStick.ReadValue();
            Vector2 rightStick = pad.rightStick.ReadValue();
            float deadzoneSqr = gamepadMoveDeadzone * gamepadMoveDeadzone;
            bool usedStick =
                leftStick.sqrMagnitude > deadzoneSqr ||
                rightStick.sqrMagnitude > deadzoneSqr;
            bool usedButtons =
                pad.buttonSouth.wasPressedThisFrame ||
                pad.buttonNorth.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame ||
                pad.buttonEast.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame ||
                pad.leftShoulder.wasPressedThisFrame ||
                pad.rightShoulder.wasPressedThisFrame ||
                pad.leftTrigger.wasPressedThisFrame ||
                pad.rightTrigger.wasPressedThisFrame;

            if (usedStick || usedButtons)
                return pad;
        }

        return null;
    }

    private bool IsKeyboardInputActive()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return
            keyboard.wKey.isPressed ||
            keyboard.aKey.isPressed ||
            keyboard.sKey.isPressed ||
            keyboard.dKey.isPressed ||
            keyboard.upArrowKey.isPressed ||
            keyboard.downArrowKey.isPressed ||
            keyboard.leftArrowKey.isPressed ||
            keyboard.rightArrowKey.isPressed ||
            keyboard[keyboardBindings.interact].wasPressedThisFrame ||
            keyboard[keyboardBindings.attack].wasPressedThisFrame ||
            keyboard[keyboardBindings.dodge].wasPressedThisFrame;
    }

    private ControlScheme DetectGamepadScheme(Gamepad gamepad)
    {
        if (gamepad == null)
            return ControlScheme.KeyboardMouse;

        string description = gamepad.displayName + " " + gamepad.description.product + " " + gamepad.description.manufacturer;
        string normalized = description.ToLowerInvariant();

        if (normalized.Contains("xbox") || normalized.Contains("xinput"))
            return ControlScheme.XboxController;

        if (normalized.Contains("playstation") ||
            normalized.Contains("dualshock") ||
            normalized.Contains("dualsense") ||
            normalized.Contains("ps4") ||
            normalized.Contains("ps5") ||
            normalized.Contains("sony"))
            return ControlScheme.PlayStationController;

        return ControlScheme.GenericGamepad;
    }

    private bool IsGamepadActionPressedThisFrame(Gamepad gamepad, GamepadActionButton button)
    {
        if (gamepad == null)
            return false;

        switch (button)
        {
            case GamepadActionButton.South:
                return gamepad.buttonSouth.wasPressedThisFrame;
            case GamepadActionButton.East:
                return gamepad.buttonEast.wasPressedThisFrame;
            case GamepadActionButton.West:
                return gamepad.buttonWest.wasPressedThisFrame;
            case GamepadActionButton.North:
                return gamepad.buttonNorth.wasPressedThisFrame;
            case GamepadActionButton.LeftShoulder:
                return gamepad.leftShoulder.wasPressedThisFrame;
            case GamepadActionButton.RightShoulder:
                return gamepad.rightShoulder.wasPressedThisFrame;
            case GamepadActionButton.Start:
                return gamepad.startButton.wasPressedThisFrame;
            default:
                return false;
        }
    }
}
