using App1.Helpers;
using App1.Models;
using App1.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace App1
{
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<ClickStep> ClickSteps { get; } = new ObservableCollection<ClickStep>();

        private readonly AutoClickService _autoClickService = new AutoClickService();

        private DispatcherTimer _timer;

        private bool _isTracking = true;
        private bool _isRunningScript = false;

        private bool _f6WasPressed = false;
        private bool _f7WasPressed = false;
        private bool _f8WasPressed = false;
        private bool _f9WasPressed = false;

        private int _currentX = 0;
        private int _currentY = 0;

        private int _stepIdCounter = 1;
        private string _currentScriptName = "";

        private CancellationTokenSource? _scriptCancellation;
        private int _addPointHotkey = HotkeyService.VK_F6;
        private int _toggleTrackingHotkey = HotkeyService.VK_F7;
        private int _runScriptHotkey = HotkeyService.VK_F8;
        private int _copyCoordinateHotkey = HotkeyService.VK_F9;

        private bool _addPointHotkeyWasPressed = false;
        private bool _toggleTrackingHotkeyWasPressed = false;
        private bool _runScriptHotkeyWasPressed = false;
        private bool _copyCoordinateHotkeyWasPressed = false;

        private void LoadHotkeyComboBoxes()
        {
            List<HotkeyOption> options = HotkeyService.GetFunctionKeyOptions();

            FillHotkeyComboBox(AddPointHotkeyComboBox, options);
            FillHotkeyComboBox(ToggleTrackingHotkeyComboBox, options);
            FillHotkeyComboBox(RunScriptHotkeyComboBox, options);
            FillHotkeyComboBox(CopyCoordinateHotkeyComboBox, options);

            SelectHotkeyComboBoxValue(AddPointHotkeyComboBox, _addPointHotkey);
            SelectHotkeyComboBoxValue(ToggleTrackingHotkeyComboBox, _toggleTrackingHotkey);
            SelectHotkeyComboBoxValue(RunScriptHotkeyComboBox, _runScriptHotkey);
            SelectHotkeyComboBoxValue(CopyCoordinateHotkeyComboBox, _copyCoordinateHotkey);
        }

        private void FillHotkeyComboBox(ComboBox comboBox, List<HotkeyOption> options)
        {
            comboBox.Items.Clear();

            foreach (HotkeyOption option in options)
            {
                comboBox.Items.Add(new ComboBoxItem
                {
                    Content = option.Name,
                    Tag = option.KeyCode
                });
            }
        }

        private void SelectHotkeyComboBoxValue(ComboBox comboBox, int keyCode)
        {
            foreach (object item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    comboBoxItem.Tag is int itemKeyCode &&
                    itemKeyCode == keyCode)
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }

        private int GetSelectedHotkeyValue(ComboBox comboBox, int defaultValue)
        {
            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem &&
                comboBoxItem.Tag is int keyCode)
            {
                return keyCode;
            }

            return defaultValue;
        }

        private void SaveHotkeySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            int addPointKey = GetSelectedHotkeyValue(AddPointHotkeyComboBox, HotkeyService.VK_F6);
            int toggleTrackingKey = GetSelectedHotkeyValue(ToggleTrackingHotkeyComboBox, HotkeyService.VK_F7);
            int runScriptKey = GetSelectedHotkeyValue(RunScriptHotkeyComboBox, HotkeyService.VK_F8);
            int copyCoordinateKey = GetSelectedHotkeyValue(CopyCoordinateHotkeyComboBox, HotkeyService.VK_F9);

            int[] selectedKeys =
            {
        addPointKey,
        toggleTrackingKey,
        runScriptKey,
        copyCoordinateKey
    };

            if (selectedKeys.Distinct().Count() != selectedKeys.Length)
            {
                HotkeySettingsStatusTextBlock.Text = "Không được đặt trùng phím tắt.";
                HotkeySettingsStatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 255, 90, 90)
                );
                return;
            }

            _addPointHotkey = addPointKey;
            _toggleTrackingHotkey = toggleTrackingKey;
            _runScriptHotkey = runScriptKey;
            _copyCoordinateHotkey = copyCoordinateKey;

            HotkeySettingsStatusTextBlock.Text = "Đã lưu cài đặt phím tắt.";
            HotkeySettingsStatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 124, 252, 0)
            );

            UpdateHotkeyInfoText();
        }

        private void ResetHotkeySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _addPointHotkey = HotkeyService.VK_F6;
            _toggleTrackingHotkey = HotkeyService.VK_F7;
            _runScriptHotkey = HotkeyService.VK_F8;
            _copyCoordinateHotkey = HotkeyService.VK_F9;

            SelectHotkeyComboBoxValue(AddPointHotkeyComboBox, _addPointHotkey);
            SelectHotkeyComboBoxValue(ToggleTrackingHotkeyComboBox, _toggleTrackingHotkey);
            SelectHotkeyComboBoxValue(RunScriptHotkeyComboBox, _runScriptHotkey);
            SelectHotkeyComboBoxValue(CopyCoordinateHotkeyComboBox, _copyCoordinateHotkey);

            HotkeySettingsStatusTextBlock.Text = "Đã khôi phục phím tắt mặc định.";
            HotkeySettingsStatusTextBlock.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 124, 252, 0)
            );

            UpdateHotkeyInfoText();
        }

        private void UpdateHotkeyInfoText()
        {
            string addPointKey = HotkeyService.GetKeyName(_addPointHotkey);
            string toggleTrackingKey = HotkeyService.GetKeyName(_toggleTrackingHotkey);
            string runScriptKey = HotkeyService.GetKeyName(_runScriptHotkey);
            string copyCoordinateKey = HotkeyService.GetKeyName(_copyCoordinateHotkey);

            CurrentHotkeyInfoTextBlock.Text =
                $"Phím tắt hiện tại:\n" +
                $"{addPointKey} = Thêm vị trí chuột vào kịch bản hiện tại\n" +
                $"{toggleTrackingKey} = Start / Stop theo dõi tọa độ\n" +
                $"{runScriptKey} = Start / Stop chạy kịch bản\n" +
                $"{copyCoordinateKey} = Copy tọa độ hiện tại";
        }

        public MainWindow()
        {
            this.InitializeComponent();

            LoadHotkeyComboBoxes();
            UpdateHotkeyInfoText();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            ShowPage(CoordinatePage);
        }

        private void ClickStepsListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            RefreshStepIndexes();
            AddLog("Đã thay đổi thứ tự điểm click.");
        }

        private void Timer_Tick(object sender, object e)
        {
            CheckHotkeys();

            if (_isTracking)
            {
                MousePoint point = MouseService.GetCursorPosition();

                _currentX = point.X;
                _currentY = point.Y;

                XTextBlock.Text = _currentX.ToString();
                YTextBlock.Text = _currentY.ToString();
                CoordinateTextBox.Text = $"X: {_currentX}, Y: {_currentY}";
            }
        }

        private void CheckHotkeys()
        {
            bool addPointPressed = HotkeyService.IsKeyPressed(_addPointHotkey);
            bool toggleTrackingPressed = HotkeyService.IsKeyPressed(_toggleTrackingHotkey);
            bool runScriptPressed = HotkeyService.IsKeyPressed(_runScriptHotkey);
            bool copyCoordinatePressed = HotkeyService.IsKeyPressed(_copyCoordinateHotkey);

            if (addPointPressed && !_addPointHotkeyWasPressed)
            {
                AddCurrentMousePositionToCurrentScript();
            }

            if (toggleTrackingPressed && !_toggleTrackingHotkeyWasPressed)
            {
                ToggleTracking();
            }

            if (runScriptPressed && !_runScriptHotkeyWasPressed)
            {
                ToggleRunScript();
            }

            if (copyCoordinatePressed && !_copyCoordinateHotkeyWasPressed)
            {
                CopyCurrentCoordinate();
            }

            _addPointHotkeyWasPressed = addPointPressed;
            _toggleTrackingHotkeyWasPressed = toggleTrackingPressed;
            _runScriptHotkeyWasPressed = runScriptPressed;
            _copyCoordinateHotkeyWasPressed = copyCoordinatePressed;
        }

        private void CopyCurrentCoordinate()
        {
            MousePoint point = MouseService.GetCursorPosition();

            _currentX = point.X;
            _currentY = point.Y;

            string coordinate = $"X: {_currentX}, Y: {_currentY}";

            XTextBlock.Text = _currentX.ToString();
            YTextBlock.Text = _currentY.ToString();
            CoordinateTextBox.Text = coordinate;

            var package = new DataPackage();
            package.SetText(coordinate);
            Clipboard.SetContent(package);

            StatusTextBlock.Text = $"Đã copy: {coordinate}";
            ShowCursorToast($"Đã copy\n{coordinate}", _currentX, _currentY);
        }

        private void AddCurrentMousePositionToCurrentScript()
        {
            MousePoint point = MouseService.GetCursorPosition();

            if (string.IsNullOrWhiteSpace(_currentScriptName))
            {
                ShowCursorToast("Chưa có kịch bản\nHãy tạo kịch bản trước", point.X, point.Y);
                return;
            }

            _currentX = point.X;
            _currentY = point.Y;

            int delayMs = ParseInt(InputDelayTextBox.Text, 0);

            AddClickStep(_currentX, _currentY, delayMs);

            ShowCursorToast($"Đã thêm Click\nX: {_currentX}, Y: {_currentY}\nDelay: {delayMs}ms", _currentX, _currentY);
            AddLog($"F6 thêm Click: X={_currentX}, Y={_currentY}, Delay sau click={delayMs}ms");
        }

        private void AddDelayStep(int delayMs)
        {
            ClickSteps.Add(new ClickStep
            {
                Id = _stepIdCounter++,
                Index = ClickSteps.Count + 1,
                Type = "Delay",
                X = 0,
                Y = 0,
                DelayMs = delayMs,
                Description = "Wait before next action"
            });

            RefreshStepIndexes();
        }

        private void AddClickStep(int x, int y, int delayMs = 0)
        {
            ClickSteps.Add(new ClickStep
            {
                Id = _stepIdCounter++,
                Index = ClickSteps.Count + 1,
                Type = "Click",
                X = x,
                Y = y,
                DelayMs = delayMs,
                Description = "Left click"
            });

            RefreshStepIndexes();
        }

        private void RefreshStepIndexes()
        {
            for (int i = 0; i < ClickSteps.Count; i++)
            {
                ClickSteps[i].Index = i + 1;
            }
        }

        private void ToggleTracking()
        {
            _isTracking = !_isTracking;

            MousePoint point = MouseService.GetCursorPosition();

            if (_isTracking)
            {
                StartStopButton.Content = "Stop";
                StatusTextBlock.Text = "Đang theo dõi tọa độ...";
                ShowCursorToast("Đã bật theo dõi", point.X, point.Y);
            }
            else
            {
                StartStopButton.Content = "Start";
                StatusTextBlock.Text = "Đã dừng theo dõi tọa độ.";
                ShowCursorToast("Đã dừng theo dõi", point.X, point.Y);
            }
        }

        private void ToggleRunScript()
        {
            if (_isRunningScript)
            {
                StopScript();
            }
            else
            {
                _ = RunScriptAsync();
            }
        }

        private async Task RunScriptAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentScriptName))
            {
                AddLog("Chưa có kịch bản để chạy.");
                RunStatusTextBlock.Text = "Chưa có kịch bản.";
                return;
            }

            if (ClickSteps.Count == 0)
            {
                AddLog("Không có điểm click nào trong kịch bản.");
                RunStatusTextBlock.Text = "Không có điểm click.";
                return;
            }

            int loopCount = ParseInt(LoopCountTextBox.Text, 1);

            _scriptCancellation = new CancellationTokenSource();
            CancellationToken token = _scriptCancellation.Token;

            _isRunningScript = true;
            RunScriptButton.Content = "Stop";
            RunStatusTextBlock.Text = "Đang chạy...";

            AddLog($"Bắt đầu chạy kịch bản: {_currentScriptName}");
            ShowCursorToast("Bắt đầu auto click", _currentX, _currentY);

            try
            {
                await _autoClickService.RunAsync(
                    ClickSteps,
                    loopCount,
                    token,
                    AddLog
                );

                if (!token.IsCancellationRequested)
                {
                    AddLog("Hoàn thành kịch bản.");
                    RunStatusTextBlock.Text = "Hoàn thành.";
                    ShowCursorToast("Hoàn thành auto click", _currentX, _currentY);
                }
            }
            catch (TaskCanceledException)
            {
                AddLog("Đã dừng kịch bản.");
                RunStatusTextBlock.Text = "Đã dừng.";
            }
            finally
            {
                _isRunningScript = false;
                RunScriptButton.Content = "Start";
                _scriptCancellation?.Dispose();
                _scriptCancellation = null;
            }
        }

        private void StopScript()
        {
            if (_scriptCancellation != null)
            {
                _scriptCancellation.Cancel();
            }

            _isRunningScript = false;
            RunScriptButton.Content = "Start";
            RunStatusTextBlock.Text = "Đang dừng...";
            AddLog("Yêu cầu dừng kịch bản.");
        }

        private void AddLog(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            LogTextBox.Text += $"[{time}] {message}\r\n";
        }

        private int ParseInt(string text, int defaultValue)
        {
            if (int.TryParse(text, out int value))
            {
                return value;
            }

            return defaultValue;
        }

        private void ShowCursorToast(string message, int x, int y)
        {
            CursorToastWindow toast = new CursorToastWindow(message, x, y);
            toast.Activate();
        }

        private void ShowPage(UIElement page)
        {
            CoordinatePage.Visibility = Visibility.Collapsed;
            ClickScriptPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;

            page.Visibility = Visibility.Visible;
        }

        private void CreateScriptButton_Click(object sender, RoutedEventArgs e)
        {
            string scriptName = ScriptNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(scriptName))
            {
                MousePoint point = MouseService.GetCursorPosition();
                ShowCursorToast("Vui lòng nhập tên kịch bản", point.X, point.Y);
                return;
            }

            _currentScriptName = scriptName;

            ClickSteps.Clear();
            LogTextBox.Text = "";

            InputXTextBox.Text = "0";
            InputYTextBox.Text = "0";
            InputDelayTextBox.Text = "0";
            LoopCountTextBox.Text = "1";

            CurrentScriptNameTextBlock.Text = $"Kịch bản: {_currentScriptName}";

            CreateScriptPanel.Visibility = Visibility.Collapsed;
            ScriptEditorPanel.Visibility = Visibility.Visible;

            AddLog($"Đã tạo kịch bản: {_currentScriptName}");
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            StopScript();

            _currentScriptName = "";
            ScriptNameTextBox.Text = "";

            ClickSteps.Clear();
            LogTextBox.Text = "";

            CreateScriptPanel.Visibility = Visibility.Visible;
            ScriptEditorPanel.Visibility = Visibility.Collapsed;
        }

        private void AddDelayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentScriptName))
            {
                AddLog("Chưa có kịch bản. Hãy tạo kịch bản trước.");
                return;
            }

            int delayMs = ParseInt(InputDelayTextBox.Text, 0);

            AddDelayStep(delayMs);
            AddLog($"Thêm Delay: {delayMs}ms");
        }

        private void AddPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentScriptName))
            {
                AddLog("Chưa có kịch bản. Hãy tạo kịch bản trước.");
                return;
            }

            int x = ParseInt(InputXTextBox.Text, 0);
            int y = ParseInt(InputYTextBox.Text, 0);
            int delayMs = ParseInt(InputDelayTextBox.Text, 0);

            AddClickStep(x, y, delayMs);
            AddLog($"Thêm Click thủ công: X={x}, Y={y}, Delay sau click={delayMs}ms");
        }

        private void DeleteStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            int id = ParseInt(button.Tag?.ToString() ?? "", -1);

            ClickStep? target = null;

            foreach (ClickStep step in ClickSteps)
            {
                if (step.Id == id)
                {
                    target = step;
                    break;
                }
            }

            if (target != null)
            {
                ClickSteps.Remove(target);
                RefreshStepIndexes();
                AddLog($"Đã xóa điểm click ID={id}");
            }
        }

        private void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleRunScript();
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";
        }

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            ClickSteps.Clear();
            AddLog("Đã xóa toàn bộ điểm click trong kịch bản.");
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTracking();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyCurrentCoordinate();
        }

        private void CoordinateMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(CoordinatePage);
        }

        private void ClickScriptMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(ClickScriptPage);
        }

        private void SettingsMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SettingsPage);
        }
    }
}