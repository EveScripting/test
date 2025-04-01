/*
 * ---------------------------------------
 * User: duketwo
 * Date: 19.06.2016
 * Time: 16:15
 * 
 * ---------------------------------------
 */

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SharedComponents.WinApiUtil
{
    public class Pinvokes
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        public const int LWA_ALPHA = 0x2;

        public const int LWA_COLORKEY = 0x1;

        //Declare the mouse hook constant.
        //For other hook types, you can obtain these values from Winuser.h in the Microsoft SDK.
        public const int WH_MOUSE = 7;

        public const short SWP_NOMOVE = 0X2;
        public const short SWP_NOSIZE = 1;
        public const short SWP_NOZORDER = 0X4;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int WM_SETREDRAW = 11;

        public const int WmPaint = 0x000F;

        public const int GWL_HWNDPARENT = -8;

        public const int GWL_ID = -12;
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;



        public const int SW_RESTORE = 9;
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 1;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_MINIMIZE = 6;
        public const int SW_FORCEMINIMIZE = 11;
        public const int SW_MAXIMIZE = 3;

        // Window Styles
        public const UInt32 WS_OVERLAPPED = 0;

        public const UInt32 WS_POPUP = 0x80000000;
        public const UInt32 WS_CHILD = 0x40000000;
        public const UInt32 WS_MINIMIZE = 0x20000000;
        public const UInt32 WS_VISIBLE = 0x10000000;
        public const UInt32 WS_DISABLED = 0x8000000;
        public const UInt32 WS_CLIPSIBLINGS = 0x4000000;
        public const UInt32 WS_CLIPCHILDREN = 0x2000000;
        public const UInt32 WS_MAXIMIZE = 0x1000000;
        public const UInt32 WS_CAPTION = 0xC00000; // WS_BORDER or WS_DLGFRAME
        public const UInt32 WS_BORDER = 0x800000;
        public const UInt32 WS_DLGFRAME = 0x400000;
        public const UInt32 WS_VSCROLL = 0x200000;
        public const UInt32 WS_HSCROLL = 0x100000;
        public const UInt32 WS_SYSMENU = 0x80000;
        public const UInt32 WS_THICKFRAME = 0x40000;
        public const UInt32 WS_GROUP = 0x20000;
        public const UInt32 WS_TABSTOP = 0x10000;
        public const UInt32 WS_MINIMIZEBOX = 0x20000;
        public const UInt32 WS_MAXIMIZEBOX = 0x10000;
        public const UInt32 WS_TILED = WS_OVERLAPPED;
        public const UInt32 WS_ICONIC = WS_MINIMIZE;
        public const UInt32 WS_SIZEBOX = WS_THICKFRAME;

        // Extended Window Styles
        public const UInt32 WS_EX_DLGMODALFRAME = 0x0001;

        public const UInt32 WS_EX_NOPARENTNOTIFY = 0x0004;
        public const UInt32 WS_EX_TOPMOST = 0x0008;
        public const UInt32 WS_EX_ACCEPTFILES = 0x0010;
        public const UInt32 WS_EX_TRANSPARENT = 0x0020;
        public const UInt32 WS_EX_MDICHILD = 0x0040;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const UInt32 WS_EX_WINDOWEDGE = 0x0100;
        public const UInt32 WS_EX_CLIENTEDGE = 0x0200;
        public const UInt32 WS_EX_CONTEXTHELP = 0x0400;
        public const UInt32 WS_EX_RIGHT = 0x1000;
        public const UInt32 WS_EX_LEFT = 0x0000;
        public const UInt32 WS_EX_RTLREADING = 0x2000;
        public const UInt32 WS_EX_LTRREADING = 0x0000;
        public const UInt32 WS_EX_LEFTSCROLLBAR = 0x4000;
        public const UInt32 WS_EX_RIGHTSCROLLBAR = 0x0000;
        public const UInt32 WS_EX_CONTROLPARENT = 0x10000;
        public const UInt32 WS_EX_STATICEDGE = 0x20000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const UInt32 WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
        public const UInt32 WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
        public const UInt32 WS_EX_LAYERED = 0x00080000;
        public const UInt32 WS_EX_NOINHERITLAYOUT = 0x00100000; // Disable inheritence of mirroring by children
        public const UInt32 WS_EX_LAYOUTRTL = 0x00400000; // Right to left mirroring
        public const UInt32 WS_EX_COMPOSITED = 0x02000000;
        public const UInt32 WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern Int64 SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);


        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hObjectSource,
            int nXSrc, int nYSrc, int dwRop);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
            int nHeight);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        //[DllImport("user32.dll")]
        //public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);



        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int User32_GetWindowLong(IntPtr hWnd, GetWindowLong nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        public static extern int User32_SetWindowLong(IntPtr hWnd, GetWindowLong nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
        public static extern bool User32_SetLayeredWindowAttributes(IntPtr hWnd, int crKey, byte bAlpha, LayeredWindowAttributes dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("USER32.DLL")]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("USER32.DLL")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("USER32.DLL")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);


        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(int hhk);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    }
}