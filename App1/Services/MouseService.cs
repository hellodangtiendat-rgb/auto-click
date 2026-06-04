using System;
using System.Runtime.InteropServices;

namespace App1.Services
{
    public static class MouseService
    {
        public static MousePoint GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return new MousePoint(point.X, point.Y);
        }

        public static void MoveTo(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void LeftClick(int x, int y)
        {
            SetCursorPos(x, y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            ThreadSleep(40);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private static void ThreadSleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(
            uint dwFlags,
            uint dx,
            uint dy,
            uint dwData,
            UIntPtr dwExtraInfo
        );

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }

    public readonly struct MousePoint
    {
        public MousePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}