using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharedComponents.IPC
{
    [Serializable]
    public class BroadcastMessage
    {
        public BroadcastMessage(string sender, string receiver, string targetController, string command, string payload)
        {
            Sender = sender;
            Receiver = receiver;
            TargetController = targetController;
            Command = command;
            Payload = payload;
        }

        public BroadcastMessage(string sender, string receiver, string targetController, string command, object payload)
        {
            Sender = sender;
            Receiver = receiver;
            TargetController = targetController;
            Command = command;
            Payload = JsonConvert.SerializeObject(payload);
        }

        public string Sender { get; set; } // charname
        public string Receiver { get; set; } // charname or wildcard '*'
        public string TargetController { get; set; } // any controller Classname, i.e 'AbyssalController'
        public string Command { get; set; }
        public string Payload { get; set; }

        public override string ToString()
        {
            return $"Sender [{Sender}] Receiver [{Receiver}] TargetController [{TargetController}] Command [{Command}] Payload [{Payload}]";
        }

        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Payload);
        }
    }
}
