namespace App1.Models
{
    public class RecordedAction
    {
        public int Index { get; set; }

        public string ActionType { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }

        public int KeyCode { get; set; }
        public string KeyName { get; set; } = "";

        public int DelayMs { get; set; }

        public string DisplayText
        {
            get
            {
                if (ActionType.StartsWith("Mouse"))
                {
                    return $"#{Index} | {ActionType} | X={X}, Y={Y} | Delay={DelayMs}ms";
                }

                return $"#{Index} | {ActionType} | Key={KeyName} ({KeyCode}) | Delay={DelayMs}ms";
            }
        }
    }
}