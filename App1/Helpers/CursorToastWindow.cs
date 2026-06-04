using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace App1.Helpers
{
    public sealed class CursorToastWindow : Window
    {
        private readonly DispatcherTimer _closeTimer;
        private readonly int _x;
        private readonly int _y;

        public CursorToastWindow(string message, int x, int y)
        {
            _x = x;
            _y = y;

            this.ExtendsContentIntoTitleBar = true;

            Grid root = new Grid
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0))
            };

            Border box = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(235, 35, 35, 35)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                }
            };

            root.Children.Add(box);

            this.Content = root;
            this.SetTitleBar(root);

            this.Activated += CursorToastWindow_Activated;

            _closeTimer = new DispatcherTimer();
            _closeTimer.Interval = TimeSpan.FromMilliseconds(900);
            _closeTimer.Tick += CloseTimer_Tick;
            _closeTimer.Start();
        }

        private void CursorToastWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            int width = 220;
            int height = 72;

            int toastX = _x + 18;
            int toastY = _y + 18;

            appWindow.Resize(new SizeInt32(width, height));
            appWindow.Move(new PointInt32(toastX, toastY));

            SetWindowPos(
                hwnd,
                HWND_TOPMOST,
                toastX,
                toastY,
                width,
                height,
                SWP_NOACTIVATE | SWP_SHOWWINDOW
            );
        }

        private void CloseTimer_Tick(object sender, object e)
        {
            _closeTimer.Stop();
            this.Close();
        }

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags
        );
    }
}