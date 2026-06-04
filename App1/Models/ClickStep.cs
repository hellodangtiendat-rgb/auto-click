using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App1.Models
{
    public class ClickStep : INotifyPropertyChanged
    {
        private int _index;
        private string _type = "Click";
        private int _x;
        private int _y;
        private int _delayMs;
        private string _description = "Left click";

        public int Id { get; set; }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IndexText));
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TypeText));
                OnPropertyChanged(nameof(IsClickStep));
                OnPropertyChanged(nameof(IsDelayStep));
            }
        }

        public int X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(XInput));
            }
        }

        public int Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(YInput));
            }
        }

        public int DelayMs
        {
            get => _delayMs;
            set
            {
                _delayMs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DelayInput));
                OnPropertyChanged(nameof(DelayText));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public bool IsClickStep => Type == "Click";
        public bool IsDelayStep => Type == "Delay";

        public string IndexText => $"#{Index}";
        public string TypeText => Type == "Delay" ? "Delay" : "Click";
        public string DelayText => $"{DelayMs}ms";

        public string XInput
        {
            get => X.ToString();
            set
            {
                if (int.TryParse(value, out int result))
                {
                    X = result;
                }
            }
        }

        public string YInput
        {
            get => Y.ToString();
            set
            {
                if (int.TryParse(value, out int result))
                {
                    Y = result;
                }
            }
        }

        public string DelayInput
        {
            get => DelayMs.ToString();
            set
            {
                if (int.TryParse(value, out int result))
                {
                    DelayMs = result;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}