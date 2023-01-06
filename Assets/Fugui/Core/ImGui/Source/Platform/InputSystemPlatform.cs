﻿#if HAS_INPUTSYSTEM
using ImGuiNET;
using System.Collections.Generic;
using UImGui.Assets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace UImGui.Platform
{
	// Implemented features:
	// [x] Platform: Clipboard support.
	// [x] Platform: Mouse cursor shape and visibility. Disable with io.ConfigFlags |= ImGuiConfigFlags.NoMouseCursorChange.
	// [x] Platform: Keyboard arrays indexed using InputSystem.Key codes, e.g. ImGui.IsKeyPressed(Key.Space).
	// [x] Platform: Gamepad support. Enabled with io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad.
	// [~] Platform: IME support.
	// [~] Platform: INI settings support.

	/// <summary>
	/// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
	/// </summary>
	internal sealed class InputSystemPlatform : PlatformBase
	{
		private readonly List<char> _textInput = new List<char>();

		private int[] _mainKeys;

		private Keyboard _keyboard = null;

		public InputSystemPlatform(CursorShapesAsset cursorShapes, IniSettingsAsset iniSettings) :
			base(cursorShapes, iniSettings)
		{ }

		public override bool Initialize(ImGuiIOPtr io, UIOConfig config, string platformName)
		{
			InputSystem.onDeviceChange += OnDeviceChange;
			base.Initialize(io, config, platformName);

			io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;

			unsafe
			{
				PlatformCallbacks.SetClipboardFunctions(PlatformCallbacks.GetClipboardTextCallback, PlatformCallbacks.SetClipboardTextCallback);
			}

			SetupKeyboard(io, Keyboard.current);

			return true;
		}

		public override void Shutdown(ImGuiIOPtr io)
		{
			base.Shutdown(io);
			InputSystem.onDeviceChange -= OnDeviceChange;
		}

		public override void PrepareFrame(ImGuiIOPtr io, Rect displayRect)
		{
			base.PrepareFrame(io, displayRect);

			UpdateKeyboard(io, Keyboard.current);
			UpdateMouse(io, Mouse.current);
			UpdateCursor(io, ImGui.GetMouseCursor());
			UpdateGamepad(io, Gamepad.current);
		}

		private static void UpdateMouse(ImGuiIOPtr io, Mouse mouse)
		{
			if (mouse == null) return;

			// Set Unity mouse position if requested.
			if (io.WantSetMousePos)
			{
				mouse.WarpCursorPosition(Utils.ImGuiToScreen(io.MousePos));
			}

			io.MousePos = Utils.ScreenToImGui(mouse.position.ReadValue());

			Vector2 mouseScroll = mouse.scroll.ReadValue() / 120f;
			io.MouseWheel = mouseScroll.y;
			io.MouseWheelH = mouseScroll.x;

			io.MouseDown[0] = mouse.leftButton.isPressed;
			io.MouseDown[1] = mouse.rightButton.isPressed;
			io.MouseDown[2] = mouse.middleButton.isPressed;
		}

		private static void UpdateGamepad(ImGuiIOPtr io, Gamepad gamepad)
		{
			io.BackendFlags = gamepad == null ?
				io.BackendFlags & ~ImGuiBackendFlags.HasGamepad :
				io.BackendFlags | ImGuiBackendFlags.HasGamepad;

			if (gamepad == null || (io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) == 0) return;

			io.NavInputs[(int)ImGuiNavInput.Activate] = gamepad.buttonSouth.ReadValue(); // A / Cross
			io.NavInputs[(int)ImGuiNavInput.Cancel] = gamepad.buttonEast.ReadValue(); // B / Circle
			io.NavInputs[(int)ImGuiNavInput.Menu] = gamepad.buttonWest.ReadValue(); // X / Square
			io.NavInputs[(int)ImGuiNavInput.Input] = gamepad.buttonNorth.ReadValue(); // Y / Triangle

			io.NavInputs[(int)ImGuiNavInput.DpadLeft] = gamepad.dpad.left.ReadValue(); // D-Pad Left
			io.NavInputs[(int)ImGuiNavInput.DpadRight] = gamepad.dpad.right.ReadValue(); // D-Pad Right
			io.NavInputs[(int)ImGuiNavInput.DpadUp] = gamepad.dpad.up.ReadValue(); // D-Pad Up
			io.NavInputs[(int)ImGuiNavInput.DpadDown] = gamepad.dpad.down.ReadValue(); // D-Pad Down

			io.NavInputs[(int)ImGuiNavInput.FocusPrev] = gamepad.leftShoulder.ReadValue(); // LB / L1
			io.NavInputs[(int)ImGuiNavInput.FocusNext] = gamepad.rightShoulder.ReadValue(); // RB / R1
			io.NavInputs[(int)ImGuiNavInput.TweakSlow] = gamepad.leftShoulder.ReadValue(); // LB / L1
			io.NavInputs[(int)ImGuiNavInput.TweakFast] = gamepad.rightShoulder.ReadValue(); // RB / R1

			io.NavInputs[(int)ImGuiNavInput.LStickLeft] = gamepad.leftStick.left.ReadValue();
			io.NavInputs[(int)ImGuiNavInput.LStickRight] = gamepad.leftStick.right.ReadValue();
			io.NavInputs[(int)ImGuiNavInput.LStickUp] = gamepad.leftStick.up.ReadValue();
			io.NavInputs[(int)ImGuiNavInput.LStickDown] = gamepad.leftStick.down.ReadValue();
		}

		private void SetupKeyboard(ImGuiIOPtr io, Keyboard keyboard)
		{
			if (_keyboard != null)
			{
				for (int i = 0; i < (int)ImGuiKey.COUNT; ++i)
				{
					io.KeyMap[i] = -1;
				}

				_keyboard.onTextInput -= _textInput.Add;
			}

			_keyboard = keyboard;

			// Map and store new keys by assigning io.KeyMap and setting value of array.
			_mainKeys = new int[] {
				// Letter keys mapped by display name to avoid being layout agnostic (used as shortcuts).
				io.KeyMap[(int)ImGuiKey.A] = (int)((KeyControl)keyboard["#(a)"]).keyCode, // For text edit CTRL+A: select all.
				io.KeyMap[(int)ImGuiKey.C] = (int)((KeyControl)keyboard["#(c)"]).keyCode, // For text edit CTRL+C: copy.
				io.KeyMap[(int)ImGuiKey.V] = (int)((KeyControl)keyboard["#(v)"]).keyCode, // For text edit CTRL+V: paste.
				io.KeyMap[(int)ImGuiKey.X] = (int)((KeyControl)keyboard["#(x)"]).keyCode, // For text edit CTRL+X: cut.
				io.KeyMap[(int)ImGuiKey.Y] = (int)((KeyControl)keyboard["#(y)"]).keyCode, // For text edit CTRL+Y: redo.
				io.KeyMap[(int)ImGuiKey.Z] = (int)((KeyControl)keyboard["#(z)"]).keyCode, // For text edit CTRL+Z: undo.

				io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab,

				io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.LeftArrow,
				io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.RightArrow,
				io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.UpArrow,
				io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.DownArrow,

				io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp,
				io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown,

				io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home,
				io.KeyMap[(int)ImGuiKey.End] = (int)Key.End,
				io.KeyMap[(int)ImGuiKey.Insert] = (int)Key.Insert,
				io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete,
				io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.Backspace,

				io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space,
				io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape,
				io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter,
				io.KeyMap[(int)ImGuiKey.KeyPadEnter] = (int)Key.NumpadEnter,
			};
			_keyboard.onTextInput += _textInput.Add;
		}

		private void UpdateKeyboard(ImGuiIOPtr io, Keyboard keyboard)
		{
			if (keyboard == null) return;

			// main keys
			for (int keyIndex = 0; keyIndex < _mainKeys.Length; keyIndex++)
			{
				int key = _mainKeys[keyIndex];
				io.KeysDown[key] = keyboard[(Key)key].isPressed;
			}

			// Keyboard modifiers.
			io.KeyShift = keyboard[Key.LeftShift].isPressed || keyboard[Key.RightShift].isPressed;
			io.KeyCtrl = keyboard[Key.LeftCtrl].isPressed || keyboard[Key.RightCtrl].isPressed;
			io.KeyAlt = keyboard[Key.LeftAlt].isPressed || keyboard[Key.RightAlt].isPressed;
			io.KeySuper = keyboard[Key.LeftMeta].isPressed || keyboard[Key.RightMeta].isPressed;

			// Text input.
			for (int i = 0, iMax = _textInput.Count; i < iMax; ++i)
			{
				io.AddInputCharacter(_textInput[i]);
			}

			_textInput.Clear();
		}

		private void OnDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (device is Keyboard keyboard)
			{
				// Keyboard layout change, remap main keys.
				if (change == InputDeviceChange.ConfigurationChanged)
				{
					SetupKeyboard(ImGui.GetIO(), keyboard);
				}

				// Keyboard device changed, setup again.
				if (Keyboard.current != _keyboard)
				{
					SetupKeyboard(ImGui.GetIO(), Keyboard.current);
				}
			}
		}
	}
}
#endif