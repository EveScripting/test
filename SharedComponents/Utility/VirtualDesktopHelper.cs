using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public class VirtualDesktopHelper
    {

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
        public interface IVirtualDesktopManager
        {
            bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
            Guid GetWindowDesktopId(IntPtr topLevelWindow);
            void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
        }

        private static bool Win10 => Environment.OSVersion.Version.Major == 10;

        private static IVirtualDesktopManager _virtualDesktopManager;

        public static IVirtualDesktopManager VirtualDesktopManager
        {
            get
            {
                Guid CLSID_VirtualDesktopManager = new Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a");
                Guid IID_IVirtualDesktopManager = new Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b");
                Type type = Type.GetTypeFromCLSID(CLSID_VirtualDesktopManager);
                IVirtualDesktopManager vdm = (IVirtualDesktopManager)Activator.CreateInstance(type);
                _virtualDesktopManager = vdm;
                return _virtualDesktopManager;
            }
        }

        public static IReadOnlyList<(Guid DesktopId, string DesktopName)> GetVirtualDesktops()
        {
            var list = new List<(Guid, string)>();

            if (!Win10)
                return list;

            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops", writable: false);
            if (key != null)
            {
                if (key.GetValue("VirtualDesktopIDs") is byte[] ids)
                {
                    const int GuidSize = 16;
                    var span = ids.AsSpan();
                    while (span.Length >= GuidSize)
                    {
                        var guidBytes = span.Slice(0, GuidSize).ToArray(); // Convert span to byte[]
                        var guid = new Guid(guidBytes); // Use the byte[] constructor
                        string? name = null;
                        using (var keyName = key.OpenSubKey($@"Desktops\{guid:B}", writable: false))
                        {
                            name = keyName?.GetValue("Name") as string;
                        }

                        // note: you may want to use a resource string to localize the value
                        name ??= "Desktop " + (list.Count + 1);
                        list.Add((guid, name));

                        span = span.Slice(GuidSize);
                    }
                }
            }

            return list;
        }

    }
}
