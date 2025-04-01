using System.Drawing;

namespace SharedComponents.Utility.AsyncLogQueue
{
    public class LogEntry
    {

        public string Message { get; set; }
        public string DescriptionOfWhere { get; private set; }
        public Color? Color { get; private set; }

        public LogEntry(string m, string dow, Color? c)
        {
            Message = m;
            Color = c;
            DescriptionOfWhere = dow;
        }
    }
}
