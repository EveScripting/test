extern alias SC;
using EVESharpCore.Cache;
using SC::SharedComponents.SharedMemory;
using SC::SharedComponents.EveMarshal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Hooks
{
    public class PacketSendHook : BaseCoreHook
    {
        private DirectEve _directEve;
        private DirectHook _hook;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SendHookDele(IntPtr byteArrayPtr, int length);

        public static event OnPacketSendHandler OnPacketSend;
        public delegate void OnPacketSendHandler(byte[] packetBytes);

        [DllImport("MemMan.dll")]
        public static extern void SendPacket(IntPtr byteArrayPtr, int length);

        public bool error = false;

        public override void Setup()
        {
            try
            {
                Console.WriteLine(nameof(UsedSharedMemoryNames.SendFuncPointer));
                _directEve.Log("---- SendFuncPtr: nameof(UsedSharedMemoryNames.SendFuncPointer: [" + nameof(UsedSharedMemoryNames.SendFuncPointer) + "]");
                _directEve.Log("---- SendFuncPtr: UsedSharedMemoryNames.SendFuncPointer: [" + UsedSharedMemoryNames.SendFuncPointer.ToString() + "]");
                var sendFuncPtr = new SharedArray<IntPtr>(Process.GetCurrentProcess().Id + nameof(UsedSharedMemoryNames.SendFuncPointer));
                Debug.WriteLine("--- SendFuncPtr: " + sendFuncPtr[0].ToString("X8"));
                _directEve.Log("---- SendFuncPtr: sendFuncPtr[0].ToString(\"X8\"): [" + sendFuncPtr[0].ToString("X8") + "]");
                _hook = _directEve.Hooking.CreateNewHook(sendFuncPtr[0], new SendHookDele(SendPacketDetour));
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine("Exception [" + ex + "]");
            }
        }

        public override void Teardown()
        {
            if (!error)
            {
                _directEve.Hooking.RemoveHook(_hook);
                _hook = null;
            }
        }

        public override void Configure(DirectEve directEve)
        {
            _directEve = directEve;
        }

        private void SendPacketDetour(IntPtr byteArrayPtr, int length)
        {

            if(OnPacketSend == null)
                return;

            if (OnPacketSend.GetInvocationList().Length == 0)
                return;

            var _byteArray = new byte[length];
            Marshal.Copy(byteArrayPtr, _byteArray, 0, length);

            try
            {
                //var unmarshal = new Unmarshal();
                //var unmarshObj = unmarshal.Process(_byteArray, null);
                OnPacketSend?.Invoke(_byteArray);
            }
            catch (Exception ex)
            {
                try
                {
                    OnPacketSend?.Invoke(_byteArray);
                    Debug.WriteLine(ex);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
