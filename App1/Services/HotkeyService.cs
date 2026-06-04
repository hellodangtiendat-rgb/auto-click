using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace App1.Services
{
    public static class HotkeyService
    {
        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_F11 = 0x7A;
        public const int VK_F12 = 0x7B;

        public static bool IsKeyPressed(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        public static List<HotkeyOption> GetFunctionKeyOptions()
        {
            return new List<HotkeyOption>
            {
                new HotkeyOption("F1", VK_F1),
                new HotkeyOption("F2", VK_F2),
                new HotkeyOption("F3", VK_F3),
                new HotkeyOption("F4", VK_F4),
                new HotkeyOption("F5", VK_F5),
                new HotkeyOption("F6", VK_F6),
                new HotkeyOption("F7", VK_F7),
                new HotkeyOption("F8", VK_F8),
                new HotkeyOption("F9", VK_F9),
                new HotkeyOption("F10", VK_F10),
                new HotkeyOption("F11", VK_F11),
                new HotkeyOption("F12", VK_F12)
            };
        }

        public static string GetKeyName(int virtualKey)
        {
            foreach (HotkeyOption option in GetFunctionKeyOptions())
            {
                if (option.KeyCode == virtualKey)
                {
                    return option.Name;
                }
            }

            return $"VK_{virtualKey}";
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }

    public class HotkeyOption
    {
        public HotkeyOption(string name, int keyCode)
        {
            Name = name;
            KeyCode = keyCode;
        }

        public string Name { get; set; }
        public int KeyCode { get; set; }
    }
}