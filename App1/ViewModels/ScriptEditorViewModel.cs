using App1.Models;
using App1.Services;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace App1.ViewModels
{
    public class ScriptEditorViewModel : INotifyPropertyChanged
    {
        private readonly AutoClickService _autoClickService = new AutoClickService();
        private readonly DispatcherTimer _autoSaveTimer;

        private int _stepIdCounter = 1;
        private bool _suppressAutoSave = false;
        private CancellationTokenSource? _cancellation;

        // ── Observable State ──────────────────────────────────────────────────

        public ObservableCollection<ClickStep> ClickSteps { get; } = new();

        private string _scriptName = "";
        public string ScriptName
        {
            get => _scriptName;
            private set { _scriptName = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScriptDisplayName)); }
        }

        public string ScriptDisplayName => string.IsNullOrWhiteSpace(ScriptName)
            ? "Kịch bản: Chưa có"
            : $"Kịch bản: {ScriptName}";

        private string _autoSaveStatus = "";
        public string AutoSaveStatus
        {
            get => _autoSaveStatus;
            private set { _autoSaveStatus = value; OnPropertyChanged(); }
        }

        private string _runStatus = "Chưa chạy";
        public string RunStatus
        {
            get => _runStatus;
            private set { _runStatus = value; OnPropertyChanged(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(RunButtonLabel)); }
        }

        public string RunButtonLabel => IsRunning ? "Stop" : "Start";

        private int _loopCount = 1;
        public int LoopCount
        {
            get => _loopCount;
            set
            {
                if (_loopCount == value) return;
                _loopCount = value;
                OnPropertyChanged();
                ScheduleAutoSave();
            }
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        public Action<string>? Log { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        // ── Constructor ───────────────────────────────────────────────────────

        public ScriptEditorViewModel()
        {
            _autoSaveTimer = new DispatcherTimer();
            _autoSaveTimer.Interval = TimeSpan.FromMilliseconds(800);
            _autoSaveTimer.Tick += OnAutoSaveTick;

            ClickSteps.CollectionChanged += OnCollectionChanged;
        }

        // ── Auto-Save ─────────────────────────────────────────────────────────

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                        foreach (ClickStep step in e.NewItems)
                            step.PropertyChanged += OnStepPropertyChanged;
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (ClickStep step in e.OldItems)
                            step.PropertyChanged -= OnStepPropertyChanged;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                        foreach (ClickStep step in e.OldItems)
                            step.PropertyChanged -= OnStepPropertyChanged;
                    if (e.NewItems != null)
                        foreach (ClickStep step in e.NewItems)
                            step.PropertyChanged += OnStepPropertyChanged;
                    break;
                // Move: no subscription change needed
                // Reset: handled via UnsubscribeAll() before Clear()
            }

            ScheduleAutoSave();
        }

        private void OnStepPropertyChanged(object? sender, PropertyChangedEventArgs e) => ScheduleAutoSave();

        private void ScheduleAutoSave()
        {
            if (_suppressAutoSave || string.IsNullOrWhiteSpace(ScriptName)) return;
            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();
            AutoSaveStatus = "Chưa lưu...";
        }

        private async void OnAutoSaveTick(object? sender, object e)
        {
            _autoSaveTimer.Stop();
            if (string.IsNullOrWhiteSpace(ScriptName)) return;
            await ScriptStorageService.SaveAsync(ScriptName, _loopCount, ClickSteps);
            AutoSaveStatus = $"Đã lưu tự động lúc {DateTime.Now:HH:mm:ss}";
        }

        // ── Script Management ─────────────────────────────────────────────────

        public void CreateScript(string name)
        {
            WithSuppressAutoSave(() =>
            {
                ScriptName = name;
                ClearAll();
                _stepIdCounter = 1;
            });

            _loopCount = 1;
            OnPropertyChanged(nameof(LoopCount));
            AutoSaveStatus = "Kịch bản mới — chưa có bước nào.";
            RunStatus = "Chưa chạy";
            Log?.Invoke($"Đã tạo kịch bản: {name}");
        }

        public void RenameScript(string newName)
        {
            ScriptName = newName;
        }

        public async Task<bool> LoadAsync(string scriptName)
        {
            SavedClickScript? script = await ScriptStorageService.LoadAsync(scriptName);

            if (script == null)
            {
                Log?.Invoke($"Không tìm thấy kịch bản: {scriptName}");
                return false;
            }

            Stop();

            WithSuppressAutoSave(() =>
            {
                ScriptName = script.Name;
                ClearAll();

                foreach (ClickStep step in script.Steps)
                    ClickSteps.Add(step);

                _stepIdCounter = ClickSteps.Count > 0 ? ClickSteps.Max(s => s.Id) + 1 : 1;
                RefreshIndexes();
            });

            _loopCount = script.LoopCount;
            OnPropertyChanged(nameof(LoopCount));
            AutoSaveStatus = "Đã mở kịch bản.";
            RunStatus = "Sẵn sàng.";
            Log?.Invoke($"Đã mở kịch bản: {script.Name}");

            return true;
        }

        public void Reset()
        {
            Stop();
            WithSuppressAutoSave(() =>
            {
                ScriptName = "";
                ClearAll();
                _stepIdCounter = 1;
            });

            _loopCount = 1;
            OnPropertyChanged(nameof(LoopCount));
            AutoSaveStatus = "";
            RunStatus = "Chưa chạy";
        }

        // ── Step Management ───────────────────────────────────────────────────

        public void AddClick(int x, int y, int delayMs = 0)
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
            RefreshIndexes();
        }

        public void AddDelay(int delayMs)
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
            RefreshIndexes();
        }

        public void DeleteStep(int id)
        {
            ClickStep? target = ClickSteps.FirstOrDefault(s => s.Id == id);
            if (target == null) return;
            ClickSteps.Remove(target);
            RefreshIndexes();
            Log?.Invoke($"Đã xóa bước ID={id}");
        }

        public void ClearSteps()
        {
            ClearAll();
            Log?.Invoke("Đã xóa toàn bộ điểm click trong kịch bản.");
        }

        public void RefreshIndexes()
        {
            for (int i = 0; i < ClickSteps.Count; i++)
                ClickSteps[i].Index = i + 1;
        }

        // ── Execution ─────────────────────────────────────────────────────────

        public void ToggleRun()
        {
            if (IsRunning) Stop();
            else _ = RunAsync();
        }

        private async Task RunAsync()
        {
            if (string.IsNullOrWhiteSpace(ScriptName))
            {
                Log?.Invoke("Chưa có kịch bản để chạy.");
                RunStatus = "Chưa có kịch bản.";
                return;
            }

            if (ClickSteps.Count == 0)
            {
                Log?.Invoke("Không có điểm click nào trong kịch bản.");
                RunStatus = "Không có điểm click.";
                return;
            }

            _cancellation = new CancellationTokenSource();
            CancellationToken token = _cancellation.Token;

            IsRunning = true;
            RunStatus = "Đang chạy...";
            Log?.Invoke($"Bắt đầu chạy kịch bản: {ScriptName}");

            try
            {
                await _autoClickService.RunAsync(ClickSteps, _loopCount, token, msg => Log?.Invoke(msg));

                if (!token.IsCancellationRequested)
                {
                    Log?.Invoke("Hoàn thành kịch bản.");
                    RunStatus = "Hoàn thành.";
                }
            }
            catch (TaskCanceledException)
            {
                Log?.Invoke("Đã dừng kịch bản.");
                RunStatus = "Đã dừng.";
            }
            finally
            {
                IsRunning = false;
                _cancellation?.Dispose();
                _cancellation = null;
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _cancellation?.Cancel();
            IsRunning = false;
            RunStatus = "Đang dừng...";
            Log?.Invoke("Yêu cầu dừng kịch bản.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ClearAll()
        {
            UnsubscribeAll();
            ClickSteps.Clear();
        }

        private void UnsubscribeAll()
        {
            foreach (ClickStep step in ClickSteps)
                step.PropertyChanged -= OnStepPropertyChanged;
        }

        private void WithSuppressAutoSave(Action action)
        {
            _suppressAutoSave = true;
            try { action(); }
            finally { _suppressAutoSave = false; }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
