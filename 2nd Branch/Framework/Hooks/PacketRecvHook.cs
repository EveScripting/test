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
    public class PacketRecvHook : BaseCoreHook
    {
        private DirectEve _directEve;
        private DirectHook _hook;

        public static event OnPacketRecvHandler OnPacketRecv;
        public delegate void OnPacketRecvHandler(byte[] packetBytes);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void RecvHookDele(IntPtr byteArrayPtr, int length);

        [DllImport("MemMan.dll")]
        public static extern void RecvPacket(IntPtr byteArrayPtr, int length);

        public bool error = false;

        public override void Setup()
        {
            try
            {
                Console.WriteLine(nameof(UsedSharedMemoryNames.RecvFuncPointer));
                _directEve.Log("---- RecvFuncPtr: nameof(UsedSharedMemoryNames.RecvFuncPointer: [" + nameof(UsedSharedMemoryNames.RecvFuncPointer) + "]");
                _directEve.Log("---- RecvFuncPtr: UsedSharedMemoryNames.RecvFuncPointer: [" + UsedSharedMemoryNames.RecvFuncPointer.ToString() + "]");
                var recvFuncPtr = new SharedArray<IntPtr>(Process.GetCurrentProcess().Id + nameof(UsedSharedMemoryNames.RecvFuncPointer));
                Debug.WriteLine("---- RecvFuncPtr: " + recvFuncPtr[0].ToString("X8"));
                _directEve.Log("---- RecvFuncPtr: recvFuncPtr[0].ToString(\"X8\"): [" + recvFuncPtr[0].ToString("X8") + "]");
                _hook = _directEve.Hooking.CreateNewHook(recvFuncPtr[0], new RecvHookDele(RecvPacketDetour));
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine("Exception [" + ex + "]");
            }
        }

        public override void Teardown()
        {
            try
            {
                if (!error)
                {
                    _directEve.Hooking.RemoveHook(_hook);
                    _hook = null;
                }
            }
            catch (Exception) { }
        }

        public override void Configure(DirectEve directEve)
        {
            _directEve = directEve;
        }

        private void RecvPacketDetour(IntPtr byteArrayPtr, int length)
        {
            if (OnPacketRecv == null)
                return;

            if (OnPacketRecv.GetInvocationList().Length == 0)
                return;

            var _byteArray = new byte[length];
            Marshal.Copy(byteArrayPtr, _byteArray, 0, length);

            try
            {
                //var unmarshal = new Unmarshal();
                //var unmarshObj = unmarshal.Process(_byteArray, null);
                OnPacketRecv?.Invoke(_byteArray);
            }
            catch (Exception ex)
            {
                try
                {
                    OnPacketRecv?.Invoke(_byteArray);
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
