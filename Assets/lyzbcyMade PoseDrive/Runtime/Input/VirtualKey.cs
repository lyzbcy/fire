using System;
using UnityEngine;

namespace PoseDrive.Runtime.Input
{
    /// <summary>
    /// 描述统一的虚拟按键，兼容键盘、鼠标与手柄。
    /// </summary>
    [Serializable]
    public struct VirtualKey : IEquatable<VirtualKey>
    {
        /// <summary>按键类型。</summary>
        public VirtualKeyType Type;

        /// <summary>键盘按键。</summary>
        public KeyCode KeyboardKey;

        /// <summary>鼠标按键编号。</summary>
        public int MouseButton;

        /// <summary>手柄按钮名称。</summary>
        public string GamepadButton;

        /// <summary>创建键盘按键。</summary>
        public static VirtualKey Keyboard(KeyCode key) => new VirtualKey
        {
            Type = VirtualKeyType.Keyboard,
            KeyboardKey = key
        };

        /// <summary>创建鼠标按键。</summary>
        public static VirtualKey Mouse(int button) => new VirtualKey
        {
            Type = VirtualKeyType.Mouse,
            MouseButton = button
        };

        /// <summary>创建手柄按键。</summary>
        public static VirtualKey Gamepad(string button) => new VirtualKey
        {
            Type = VirtualKeyType.Gamepad,
            GamepadButton = button
        };

        /// <inheritdoc />
        public bool Equals(VirtualKey other)
        {
            return Type == other.Type &&
                   KeyboardKey == other.KeyboardKey &&
                   MouseButton == other.MouseButton &&
                   string.Equals(GamepadButton, other.GamepadButton, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is VirtualKey other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Type;
                hashCode = (hashCode * 397) ^ (int)KeyboardKey;
                hashCode = (hashCode * 397) ^ MouseButton;
                hashCode = (hashCode * 397) ^ (GamepadButton != null ? GamepadButton.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Type switch
            {
                VirtualKeyType.Keyboard => KeyboardKey.ToString(),
                VirtualKeyType.Mouse => $"Mouse{MouseButton}",
                VirtualKeyType.Gamepad => GamepadButton ?? "GamepadButton",
                _ => "UnknownKey"
            };
        }
    }

    /// <summary>
    /// 虚拟按键类型。
    /// </summary>
    public enum VirtualKeyType
    {
        Keyboard,
        Mouse,
        Gamepad
    }
}

