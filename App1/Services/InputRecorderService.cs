using App1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace App1.Services
{
    public class InputRecorderService
    {
        private IntPtr _mouseHookId = IntPtr.Zero;
        private IntPtr _keyboardHookId = IntPtr.Zero;

        private LowLevelMouseProc? _mouseProc;
        private LowLevelKeyboardProc? _keyboardProc;

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private long _lastEventTime = 0;

        public bool IsRecording { get; private set; }

        public event Action<RecordedAction>? ActionRecorded;

        public void Start()
        {
            if (IsRecording)
            {
                return;
            }

            _lastEventTime = 0;
            _stopwatch.Restart();

            _mouseProc = MouseHookCallback;
            _keyboardProc = KeyboardHookCallback;

            _mouseHookId = SetHook(_mouseProc);
            _keyboardHookId = SetHook(_keyboardProc);

            IsRecording = true;
        }

        public void Stop()
        {
            if (!IsRecording)
            {
                return;
            }

            if (_mouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }

            if (_keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }

            _stopwatch.Stop();
            IsRecording = false;
        }

        private int GetDelay()
        {
            long now = _stopwatch.ElapsedMilliseconds;
            int delay = (int)(now - _lastEventTime);
            _lastEventTime = now;

            if (delay < 0)
            {
                delay = 0;
            }

            return delay;
        }

        private IntPtr SetHook(Delegate proc)
        {
            using Process currentProcess = Process.GetCurrentProcess();
            using ProcessModule? currentModule = currentProcess.MainModule;

            if (currentModule == null)
            {
                return IntPtr.Zero;
            }

            IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);

            if (proc is LowLevelMouseProc mouseProc)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, mouseProc, moduleHandle, 0);
            }

            if (proc is LowLevelKeyboardProc keyboardProc)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, moduleHandle, 0);
            }

            return IntPtr.Zero;
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsRecording)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                string? actionType = wParam.ToInt32() switch
                {
                    WM_LBUTTONDOWN => "MouseLeftDown",
                    WM_LBUTTONUP => "MouseLeftUp",
                    WM_RBUTTONDOWN => "MouseRightDown",
                    WM_RBUTTONUP => "MouseRightUp",
                    WM_MBUTTONDOWN => "MouseMiddleDown",
                    WM_MBUTTONUP => "MouseMiddleUp",
                    _ => null
                };

                if (actionType != null)
                {
                    ActionRecorded?.Invoke(new RecordedAction
                    {
                        ActionType = actionType,
                        X = hookStruct.pt.x,
                        Y = hookStruct.pt.y,
                        DelayMs = GetDelay()
                    });
                }
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsRecording)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                string? actionType = wParam.ToInt32() switch
                {
                    WM_KEYDOWN => "KeyDown",
                    WM_KEYUP => "KeyUp",
                    WM_SYSKEYDOWN => "KeyDown",
                    WM_SYSKEYUP => "KeyUp",
                    _ => null
                };

                if (actionType != null)
                {
                    int keyCode = hookStruct.vkCode;

                    ActionRecorded?.Invoke(new RecordedAction
                    {
                        ActionType = actionType,
                        KeyCode = keyCode,
                        KeyName = GetKeyNameFromVk(keyCode),
                        DelayMs = GetDelay()
                    });
                }
            }

            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelMouseProc lpfn,
            IntPtr hMod,
            uint dwThreadId
        );

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId
        );

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private static string GetKeyNameFromVk(int vk)
        {
            // Try to get a readable name for common keys. This is a small fallback map.
            return vk switch
            {
                0x08 => "Back",
                0x09 => "Tab",
                0x10 => "Shift",
                0x11 => "Ctrl",
                0x12 => "Alt",
                0x13 => "Pause",
                0x14 => "CapsLock",
                0x1B => "Escape",
                0x20 => "Space",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x2C => "PrintScreen",
                0x2D => "Insert",
                0x2E => "Delete",
                _ when (vk >= 0x30 && vk <= 0x39) => ((char)vk).ToString(), // 0-9
                _ when (vk >= 0x41 && vk <= 0x5A) => ((char)vk).ToString(), // A-Z
                _ when (vk >= 0x60 && vk <= 0x69) => ("Num" + (vk - 0x60).ToString()), // Numpad 0-9
                _ when (vk >= 0x70 && vk <= 0x87) => ("F" + (vk - 0x6F).ToString()), // F1-F24
                _ => vk.ToString()
            };
        }
    }
}