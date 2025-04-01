using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SharedComponents.Events.DirectEventHandler;

namespace SharedComponents.Events
{
    public class ConsoleEventHandler
    {

        public delegate void ConsoleEvent(string charName, string message, bool isErr);

        public static event ConsoleEvent OnConsoleEvent;

        public static void InvokeConsoleEvent(string charname, string message, bool isErr)
        {
            OnConsoleEvent?.Invoke(charname, message, isErr);
        }
    }
}
