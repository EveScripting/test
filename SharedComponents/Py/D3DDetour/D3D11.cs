using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EasyHook;
using SharedComponents.IPC;


namespace SharedComponents.Py.D3DDetour
{

    public class D3DEventArgs : EventArgs
    {
        public IntPtr SwapChain { get; }
        public D3DEventArgs(IntPtr swapChain)
        {
            this.SwapChain = swapChain;
        }
    }

    public class D3DResizeEventArgs : EventArgs
    {
        public int Height { get; }
        public int Width { get; }
        public D3DResizeEventArgs(int h, int w)
        {
            Height = h;
            Width = w;
        }
    }

    public class D3D11 : D3DHook
    {
        private Direct3D11Present _presentDelegate;
        private Direct3D11ResizeBuffers _resizeBuffersDelegate;
        private LocalHook _hook;
        private LocalHook _hook2;

        public override unsafe void Initialize()
        {
            var form = new Form();
            var chainDescription = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription()
                {
                    Format = 28
                },
                Usage = 32,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription()
                {
                    Count = 1
                },
                IsWindowed = true
            };

            var ppSwapChain = IntPtr.Zero;
            var ppDevice = IntPtr.Zero;
            var ppImmediateContext = IntPtr.Zero;
            var pFeatureLevel = IntPtr.Zero;
            var ret = D3D11CreateDeviceAndSwapChain(IntPtr.Zero, 1, IntPtr.Zero, 0x00000020, IntPtr.Zero, 0, 7, &chainDescription, out ppSwapChain, out ppDevice,
                out pFeatureLevel, out ppImmediateContext);
            //WCFClient.Instance.GetPipeProxy.RemoteLog($"ppSwapChain {ppSwapChain} ppDevice {ppDevice} pFeatureLevel {pFeatureLevel} ppImmediateContext {ppImmediateContext}");
            var presentPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppSwapChain), 64);

            var resizeBuffersPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppSwapChain), 13 * 8);

            //WCFClient.Instance.GetPipeProxy.RemoteLog($"PTR {ptr}");
            var d3dChain = (D3DVirtVoid)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppSwapChain), 8), typeof(D3DVirtVoid));
            var d3dDevice = (D3DVirtVoid)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppDevice), 8), typeof(D3DVirtVoid));
            var d3dContext =
                (D3DVirtVoid)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppImmediateContext), 8), typeof(D3DVirtVoid));
            d3dChain(ppSwapChain);
            d3dDevice(ppDevice);
            d3dContext(ppImmediateContext);
            _resizeBuffersDelegate = (Direct3D11ResizeBuffers)Marshal.GetDelegateForFunctionPointer(resizeBuffersPtr, typeof(Direct3D11ResizeBuffers));
            _presentDelegate = (Direct3D11Present)Marshal.GetDelegateForFunctionPointer(presentPtr, typeof(Direct3D11Present));
            _hook = LocalHook.Create(presentPtr, (Delegate)new Direct3D11Present(PresentDetour), (object)this);
            _hook.ThreadACL.SetExclusiveACL(new Int32[] { });
            _hook2 = LocalHook.Create(resizeBuffersPtr, (Delegate)new Direct3D11ResizeBuffers(ResizeBuffersDetour), (object)this);
            _hook2.ThreadACL.SetExclusiveACL(new Int32[] { });
        }

        private int PresentDetour(IntPtr swapChainPtr, int syncInterval, int flags)
        {
            RaiseEvent(new D3DEventArgs(swapChainPtr));
            return _presentDelegate(swapChainPtr, syncInterval, flags);
        }

        private int ResizeBuffersDetour(IntPtr k, int bufferCount, int width, int height, int format, int flags)
        {
            RaiseResizeEvent(new D3DResizeEventArgs(height, width));
            Console.WriteLine($@"Ptr {k} BuffCount {bufferCount} Width {width} Height {height} Format {format} Flags {flags}");
            var res = _resizeBuffersDelegate(k, 0, width, height, format, flags);

            return res;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Direct3D11ResizeBuffers(IntPtr k, int bufferCount, int width, int height, int format, int flags);

        public override void Remove()
        {
            _hook.Dispose();
            _hook2.Dispose();
        }

        [DllImport("d3d11.dll")]
        private static extern unsafe int D3D11CreateDeviceAndSwapChain(IntPtr pAdapter, int driverType, IntPtr Software,
            int flags, IntPtr pFeatureLevels,
            int FeatureLevels, int SDKVersion,
            void* pSwapChainDesc, [Out] out IntPtr ppSwapChain,
            [Out] out IntPtr ppDevice, [Out] out IntPtr pFeatureLevel,
            [Out] out IntPtr ppImmediateContext);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void D3DVirtVoid(IntPtr instance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Direct3D11Present(IntPtr swapChainPtr, int syncInterval, int flags);

        [StructLayout(LayoutKind.Sequential)]
        public struct SwapChainDescription
        {
            public ModeDescription ModeDescription;
            public SampleDescription SampleDescription;
            public int Usage;
            public int BufferCount;
            public IntPtr OutputHandle;
            [MarshalAs(UnmanagedType.Bool)] public bool IsWindowed;
            public int SwapEffect;
            public int Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rational
        {
            public int Numerator;
            public int Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ModeDescription
        {
            public int Width;
            public int Height;
            public Rational RefreshRate;
            public int Format;
            public int ScanlineOrdering;
            public int Scaling;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SampleDescription
        {
            public int Count;
            public int Quality;
        }
    }
}