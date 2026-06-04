using App1.Helpers;
using App1.Models;
using App1.Services;
using App1.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace App1
{
    public sealed partial class MainWindow : Window
    {
        public ScriptEditorViewModel ScriptVM { get; } = new ScriptEditorViewModel();
        public ObservableCollection<ScriptListItem> ScriptList { get; } = new();

        private readonly DispatcherTimer _pollingTimer;
        private HotkeySettings _hotkeys = AppSettingsService.Load();

        private bool _addPointWasPressed, _runScriptWasPressed;
        private bool _sidebarCollapsed = false;
        private const double SidebarExpandedWidth = 260;
        private const double SidebarCollapsedWidth = 44;

        public MainWindow()
        {
            this.InitializeComponent();

            ScriptVM.Log = AddLog;
            ScriptVM.PropertyChanged += OnScriptVmPropertyChanged;

            LoadHotkeyComboBoxes();
            UpdateHotkeyInfoText();

            _pollingTimer = new DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromMilliseconds(30);
            _pollingTimer.Tick += OnPollingTick;
            _pollingTimer.Start();

            ShowPage(ClickScriptPage);
            RefreshSavedScriptsList();
        }

        private void OnScriptVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptEditorViewModel.LoopCount))
                LoopCountTextBox.Text = ScriptVM.LoopCount.ToString();

            if (e.PropertyName == nameof(ScriptEditorViewModel.IsRunning))
            {
                if (ScriptVM.IsRunning)
                    MinimizeWindow();
                else
                    RestoreWindow();
            }
        }

        // ── POLLING & HOTKEYS ─────────────────────────────────────────────────

        private void OnPollingTick(object? sender, object e) => CheckHotkeys();

        private void CheckHotkeys()
        {
            bool addPoint = HotkeyService.IsKeyPressed(_hotkeys.AddPoint);
            bool runScript = HotkeyService.IsKeyPressed(_hotkeys.RunScript);

            if (addPoint && !_addPointWasPressed) AddMousePositionToScript();
            if (runScript && !_runScriptWasPressed) ScriptVM.ToggleRun();

            _addPointWasPressed = addPoint;
            _runScriptWasPressed = runScript;
        }

        private void AddMousePositionToScript()
        {
            if (string.IsNullOrWhiteSpace(ScriptVM.ScriptName)) return;

            MousePoint pos = MouseService.GetCursorPosition();
            ScriptVM.AddClick(pos.X, pos.Y, delayMs: 0);

            ShowCursorToast($"[{ScriptVM.ScriptName}]\nThêm Click X={pos.X}, Y={pos.Y}", pos.X, pos.Y);
            AddLog($"Thêm Click vào '{ScriptVM.ScriptName}': X={pos.X}, Y={pos.Y}");
        }

        // ── SCRIPT LIBRARY ────────────────────────────────────────────────────

        private bool _suppressAutoOpen = false;

        private void RefreshSavedScriptsList()
        {
            string? activeSafe = string.IsNullOrWhiteSpace(ScriptVM.ScriptName)
                ? null
                : ScriptStorageService.GetSafeName(ScriptVM.ScriptName);

            _suppressAutoOpen = true;

            ScriptList.Clear();

            ScriptListItem? activeItem = null;
            foreach (string name in ScriptStorageService.GetSavedScriptNames())
            {
                var item = new ScriptListItem(name);
                ScriptList.Add(item);
                if (name == activeSafe) activeItem = item;
            }

            if (activeItem != null)
                SavedScriptsListView.SelectedItem = activeItem;

            _suppressAutoOpen = false;
        }

        private async void SavedScriptsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScriptListItem? selected = SavedScriptsListView.SelectedItem as ScriptListItem;
            foreach (ScriptListItem item in ScriptList)
                item.IsSelected = item == selected;

            if (!_suppressAutoOpen && selected != null)
                await OpenScriptAsync(selected.Name);
        }

        private async void NewScriptDialogButton_Click(object sender, RoutedEventArgs e)
        {
            string? name = await ShowInputDialogAsync("Tạo kịch bản mới", "Ví dụ: Login tool, Farm task...");
            if (string.IsNullOrWhiteSpace(name)) return;

            ScriptVM.CreateScript(name);
            LogTextBox.Text = "";
            ShowEditor();
            RefreshSavedScriptsList();
        }

        private async void InlineRenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ScriptListItem item)
                await RenameScriptAsync(item.Name);
        }

        private async void InlineDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ScriptListItem item)
                await DeleteScriptAsync(item.Name);
        }

        private async Task OpenScriptAsync(string name)
        {
            bool loaded = await ScriptVM.LoadAsync(name);
            if (!loaded) return;

            LogTextBox.Text = "";
            ShowEditor();
            RefreshSavedScriptsList();
        }

        private async Task RenameScriptAsync(string oldName)
        {
            string? newName = await ShowInputDialogAsync("Đổi tên kịch bản", "Nhập tên mới", oldName);
            if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;

            bool ok = await ScriptStorageService.RenameAsync(oldName, newName);
            if (!ok)
            {
                await ShowAlertAsync($"Không thể đổi tên. Tên \"{newName}\" đã tồn tại hoặc không hợp lệ.");
                return;
            }

            if (IsActiveScript(oldName))
                ScriptVM.RenameScript(newName);

            RefreshSavedScriptsList();
            AddLog($"Đã đổi tên '{oldName}' → '{newName}'.");
        }

        private async Task DeleteScriptAsync(string name)
        {
            bool confirmed = await ShowConfirmAsync($"Xóa kịch bản \"{name}\"?", "Thao tác này không thể hoàn tác.");
            if (!confirmed) return;

            ScriptStorageService.Delete(name);

            if (IsActiveScript(name))
            {
                ScriptVM.Reset();
                ShowPlaceholder();
            }

            RefreshSavedScriptsList();
            AddLog($"Đã xóa kịch bản: {name}");
        }

        private bool IsActiveScript(string safeName)
            => !string.IsNullOrWhiteSpace(ScriptVM.ScriptName)
            && ScriptStorageService.GetSafeName(ScriptVM.ScriptName) == safeName;

        private void ShowEditor()
        {
            ScriptEditorPlaceholder.Visibility = Visibility.Collapsed;
            ScriptEditorPanel.Visibility = Visibility.Visible;
        }

        private void ShowPlaceholder()
        {
            ScriptEditorPanel.Visibility = Visibility.Collapsed;
            ScriptEditorPlaceholder.Visibility = Visibility.Visible;
        }

        // ── DIALOG HELPERS ────────────────────────────────────────────────────

        private async Task<string?> ShowInputDialogAsync(string title, string placeholder, string defaultValue = "")
        {
            TextBox input = new TextBox
            {
                PlaceholderText = placeholder,
                Text = defaultValue,
                SelectionStart = defaultValue.Length
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = input,
                PrimaryButtonText = "Xác nhận",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? input.Text.Trim() : null;
        }

        private async Task<bool> ShowConfirmAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Xóa",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private async Task ShowAlertAsync(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Thông báo",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        // ── SCRIPT EDITOR ─────────────────────────────────────────────────────

        private void AddPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptVM.ScriptName))
            {
                AddLog("Chưa có kịch bản. Hãy tạo kịch bản trước.");
                return;
            }

            ScriptVM.AddClick(x: 0, y: 0, delayMs: 100);
            AddLog("Thêm Click: X=0, Y=0, Delay=100ms");
        }

        private void AddDelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ScriptVM.ScriptName))
            {
                AddLog("Chưa có kịch bản. Hãy tạo kịch bản trước.");
                return;
            }

            ScriptVM.AddDelay(delayMs: 1000);
            AddLog("Thêm Delay: 1000ms");
        }

        private void DeleteStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int id = ParseInt(btn.Tag?.ToString() ?? "", -1);
            ScriptVM.DeleteStep(id);
        }

        private void ClickStepsListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            ScriptVM.RefreshIndexes();
            AddLog("Đã thay đổi thứ tự điểm click.");
        }

        private void ClearListButton_Click(object sender, RoutedEventArgs e) => ScriptVM.ClearSteps();

        private void RunScriptButton_Click(object sender, RoutedEventArgs e) => ScriptVM.ToggleRun();

        private void ClearLogButton_Click(object sender, RoutedEventArgs e) => LogTextBox.Text = "";

        private void LoopCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ScriptVM.LoopCount = ParseInt(LoopCountTextBox.Text, 1);

        // ── HOTKEY SETTINGS ───────────────────────────────────────────────────

        private void LoadHotkeyComboBoxes()
        {
            List<HotkeyOption> options = HotkeyService.GetFunctionKeyOptions();

            FillHotkeyComboBox(AddPointHotkeyComboBox, options);
            FillHotkeyComboBox(RunScriptHotkeyComboBox, options);

            SelectHotkeyComboBoxValue(AddPointHotkeyComboBox, _hotkeys.AddPoint);
            SelectHotkeyComboBoxValue(RunScriptHotkeyComboBox, _hotkeys.RunScript);
        }

        private void FillHotkeyComboBox(ComboBox comboBox, List<HotkeyOption> options)
        {
            comboBox.Items.Clear();
            foreach (HotkeyOption option in options)
                comboBox.Items.Add(new ComboBoxItem { Content = option.Name, Tag = option.KeyCode });
        }

        private void SelectHotkeyComboBoxValue(ComboBox comboBox, int keyCode)
        {
            foreach (object item in comboBox.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Tag is int code && code == keyCode)
                {
                    comboBox.SelectedItem = cbi;
                    return;
                }
            }
        }

        private int GetSelectedHotkeyValue(ComboBox comboBox, int defaultValue)
        {
            if (comboBox.SelectedItem is ComboBoxItem cbi && cbi.Tag is int code)
                return code;
            return defaultValue;
        }

        private async void SaveHotkeySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            int addPoint = GetSelectedHotkeyValue(AddPointHotkeyComboBox, HotkeyService.VK_F1);
            int run = GetSelectedHotkeyValue(RunScriptHotkeyComboBox, HotkeyService.VK_F2);

            if (addPoint == run)
            {
                HotkeySettingsStatusTextBlock.Text = "Không được đặt trùng phím tắt.";
                HotkeySettingsStatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 255, 90, 90));
                return;
            }

            _hotkeys = new HotkeySettings { AddPoint = addPoint, RunScript = run };
            await AppSettingsService.SaveAsync(_hotkeys);

            HotkeySettingsStatusTextBlock.Text = "Đã lưu cài đặt — sẽ áp dụng cho lần khởi động tiếp theo.";
            HotkeySettingsStatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 124, 252, 0));

            UpdateHotkeyInfoText();
        }

        private async void ResetHotkeySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _hotkeys = new HotkeySettings();
            await AppSettingsService.SaveAsync(_hotkeys);

            SelectHotkeyComboBoxValue(AddPointHotkeyComboBox, _hotkeys.AddPoint);
            SelectHotkeyComboBoxValue(RunScriptHotkeyComboBox, _hotkeys.RunScript);

            HotkeySettingsStatusTextBlock.Text = "Đã khôi phục phím tắt mặc định.";
            HotkeySettingsStatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 124, 252, 0));

            UpdateHotkeyInfoText();
        }

        private void UpdateHotkeyInfoText()
        {
            string addKey = HotkeyService.GetKeyName(_hotkeys.AddPoint);
            string runKey = HotkeyService.GetKeyName(_hotkeys.RunScript);

            CurrentHotkeyInfoTextBlock.Text =
                $"Phím tắt hiện tại:\n" +
                $"{addKey} = Thêm vị trí chuột vào kịch bản\n" +
                $"{runKey} = Start / Stop chạy kịch bản";

            HotkeyHintTextBlock.Text = $"{addKey} = Thêm click   {runKey} = Chạy / Dừng";
        }

        // ── UI NAVIGATION ─────────────────────────────────────────────────────

        private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _sidebarCollapsed = !_sidebarCollapsed;

            if (_sidebarCollapsed)
            {
                ClickScriptPage.ColumnDefinitions[0].Width = new GridLength(SidebarCollapsedWidth);
                SidebarTitleText.Visibility = Visibility.Collapsed;
                SidebarNewScriptButton.Visibility = Visibility.Collapsed;
                SavedScriptsListView.Visibility = Visibility.Collapsed;
                SidebarToggleButton.Content = "»";
            }
            else
            {
                ClickScriptPage.ColumnDefinitions[0].Width = new GridLength(SidebarExpandedWidth);
                SidebarTitleText.Visibility = Visibility.Visible;
                SidebarNewScriptButton.Visibility = Visibility.Visible;
                SavedScriptsListView.Visibility = Visibility.Visible;
                SidebarToggleButton.Content = "«";
            }
        }

        private void ClickScriptMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(ClickScriptPage);
            RefreshSavedScriptsList();
        }

        private void SettingsMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(SettingsPage);

        private void ShowPage(UIElement page)
        {
            ClickScriptPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;
            page.Visibility = Visibility.Visible;
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private void AddLog(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            LogTextBox.Text += $"[{time}] {message}\r\n";
        }

        private void ShowCursorToast(string message, int x, int y)
        {
            CursorToastWindow toast = new CursorToastWindow(message, x, y);
            toast.Activate();
        }

        private static int ParseInt(string text, int defaultValue)
            => int.TryParse(text, out int v) ? v : defaultValue;

        // ── WINDOW STATE ──────────────────────────────────────────────────────

        private void MinimizeWindow()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_MINIMIZE);
        }

        private void RestoreWindow()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            ShowWindow(hwnd, SW_RESTORE);
        }

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
