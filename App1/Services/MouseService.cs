using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace App1.Services
{
    public static class MouseService
    {
        public static MousePoint GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return new MousePoint(point.X, point.Y);
        }

        public static void MoveTo(int x, int y) => SetCursorPos(x, y);

        public static async Task LeftClickAsync(int x, int y)
        {
            SetCursorPos(x, y);
            SendMouseInput(MOUSEEVENTF_LEFTDOWN);
            await Task.Delay(40);
            SendMouseInput(MOUSEEVENTF_LEFTUP);
        }

        private static void SendMouseInput(uint flags)
        {
            INPUT[] inputs =
            {
                new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } }
                }
            };
            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT { public int type; public InputUnion U; }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }
    }

    public readonly struct MousePoint
    {
        public MousePoint(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
    }
}
