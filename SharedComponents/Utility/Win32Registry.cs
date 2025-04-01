using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public class Win32Registry
    {
        enum KEY_INFORMATION_CLASS
        {
            KeyBasicInformation,            // A KEY_BASIC_INFORMATION structure is supplied.
            KeyNodeInformation,             // A KEY_NODE_INFORMATION structure is supplied.
            KeyFullInformation,             // A KEY_FULL_INFORMATION structure is supplied.
            KeyNameInformation,             // A KEY_NAME_INFORMATION structure is supplied.
            KeyCachedInformation,           // A KEY_CACHED_INFORMATION structure is supplied.
            KeyFlagsInformation,            // Reserved for system use.
            KeyVirtualizationInformation,   // A KEY_VIRTUALIZATION_INFORMATION structure is supplied.
            KeyHandleTagsInformation,       // Reserved for system use.
            MaxKeyInfoClass                 // The maximum value in this enumeration type.
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KEY_NAME_INFORMATION
        {
            public UInt32 NameLength;     // The size, in bytes, of the key name string in the Name array.
            public char[] Name;           // An array of wide characters that contains the name of the key.
                                          // This character string is not null-terminated.
                                          // Only the first element in this array is included in the
                                          //    KEY_NAME_INFORMATION structure definition.
                                          //    The storage for the remaining elements in the array immediately
                                          //    follows this element.
        }

        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NtQueryKey(IntPtr hKey, KEY_INFORMATION_CLASS KeyInformationClass, IntPtr lpKeyInformation, int Length, out int ResultLength);

        public static String GetHKeyName(IntPtr hKey)
        {
            String result = String.Empty;
            IntPtr pKNI = IntPtr.Zero;

            int needed = 0;
            int status = NtQueryKey(hKey, KEY_INFORMATION_CLASS.KeyNameInformation, IntPtr.Zero, 0, out needed);
            Console.WriteLine($"Needed: {needed}");
            if ((UInt32)status == 0xC0000023)   // STATUS_BUFFER_TOO_SMALL
            {
                Console.Write("T1");
                pKNI = Marshal.AllocHGlobal(sizeof(UInt32) + needed + 4 /*paranoia*/);
                status = NtQueryKey(hKey, KEY_INFORMATION_CLASS.KeyNameInformation, pKNI, needed, out needed);
                if (status == 0)    // STATUS_SUCCESS
                {
                    char[] bytes = new char[2 + needed + 2];
                    Marshal.Copy(pKNI, bytes, 0, needed);
                    // startIndex == 2  skips the NameLength field of the structure (2 chars == 4 bytes)
                    // needed/2         reduces value from bytes to chars
                    //  needed/2 - 2    reduces length to not include the NameLength
                    result = new String(bytes, 2, (needed / 2) - 2);
                }
            }
            if (pKNI != IntPtr.Zero)
                Marshal.FreeHGlobal(pKNI);
            return result;
        }
    }
}
