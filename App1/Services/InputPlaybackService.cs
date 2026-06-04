using App1.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace App1.Services
{
    public class InputPlaybackService
    {
        public async Task PlayAsync(
            IList<RecordedAction> actions,
            int loopCount,
            CancellationToken token,
            Action<string> log)
        {
            int loop = 1;

            while (!token.IsCancellationRequested)
            {
                if (loopCount > 0 && loop > loopCount)
                {
                    break;
                }

                log($"Playback vòng {loop}");

                foreach (RecordedAction action in actions)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (action.DelayMs > 0)
                    {
                        await Task.Delay(action.DelayMs, token);
                    }

                    ExecuteAction(action);

                    log(action.DisplayText);
                }

                loop++;
            }
        }

        private void ExecuteAction(RecordedAction action)
        {
            switch (action.ActionType)
            {
                case "MouseLeftDown":
                    SetCursorPos(action.X, action.Y);
                    MouseEvent(MOUSEEVENTF_LEFTDOWN);
                    break;

                case "MouseLeftUp":
                    SetCursorPos(action.X, action.Y);
                    MouseEvent(MOUSEEVENTF_LEFTUP);
                    break;

                case "MouseRightDown":
                    SetCursorPos(action.X, action.Y);
                    MouseEvent(MOUSEEVENTF_RIGHTDOWN);
                    break;

                case "MouseRightUp":
                    SetCursorPos(action.X, action.Y);
                    MouseEvent(MOUSEEVENTF_RIGHTUP);
                    break;

                case "MouseMiddleDown":
                    SetCursorPos(action.X, action.Y);
                    MouseEvent(MOUSEEVENTF_MIDDLEDOWN);
                    break;

                case "MouseMiddleUp":
                    SetCursorPos(action.X, action.Y);
                    MouseEvent(MOUSEEVENTF_MIDDLEUP);
                    break;

                case "KeyDown":
                    KeyEvent(action.KeyCode, false);
                    break;

                case "KeyUp":
                    KeyEvent(action.KeyCode, true);
                    break;
            }
        }

        private void MouseEvent(uint flag)
        {
            INPUT[] inputs = new INPUT[1];

            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            input.U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = flag,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            inputs[0] = input;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void KeyEvent(int keyCode, bool keyUp)
        {
            INPUT[] inputs = new INPUT[1];

            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)keyCode,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            inputs[0] = input;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion U;

            public MOUSEINPUT mi
            {
                get => U.mi;
                set => U.mi = value;
            }

            public KEYBDINPUT ki
            {
                get => U.ki;
                set => U.ki = value;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern uint SendInput(
            uint nInputs,
            INPUT[] pInputs,
            int cbSize
        );
    }
}