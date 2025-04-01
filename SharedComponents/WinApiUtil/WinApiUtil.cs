/*
 * ---------------------------------------
 * User: duketwo
 * Date: 19.06.2016
 * Time: 16:30
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharedComponents.Utility;

namespace SharedComponents.WinApiUtil
{
    public static class WinApiUtil
    {
        private const uint WM_GETTEXT = 0x000D;

        public static void SetWindowsPos(IntPtr hWnd, int x, int y)
        {
            Pinvokes.SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, SWP.NOACTIVATE | SWP.NOSIZE | SWP.NOZORDER);
        }

        public static void PlaceChildAboveParent(IntPtr hWndParent, IntPtr hWndChild)
        {
            Pinvokes.SetWindowPos(hWndParent, hWndChild, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOACTIVATE | SWP.NOMOVE);
        }

        public static bool IsWindowTopMost(IntPtr hWnd)
        {
            return (Pinvokes.GetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE) & Pinvokes.WS_EX_TOPMOST) != 0;
        }

        public static void SetWindowTopMost(IntPtr hWnd, bool topMost = true)
        {
            Pinvokes.SetWindowPos(hWnd, topMost ? HWndInsertAfter.TopMost : HWndInsertAfter.NoTopMost, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
        }

        public static void SetWindowTopMostTest(IntPtr hWnd)
        {
            Pinvokes.SetWindowPos(hWnd, HWndInsertAfter.Top, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
        }


        public static void UpdateWindow(IntPtr hWnd)
        {
            Pinvokes.UpdateWindow(hWnd);
        }

        public static void ForceRedraw(IntPtr hWnd)
        {
            Pinvokes.RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, RedrawWindowFlags.UpdateNow | RedrawWindowFlags.Invalidate | RedrawWindowFlags.Frame);
        }

        public static void SetHWndInsertAfter(IntPtr hWnd, IntPtr hWndInsertAfter)
        {
            Pinvokes.SetWindowPos(hWnd, hWndInsertAfter, 0, 0, 0, 0, SWP.NOSIZE | SWP.NOMOVE | SWP.NOACTIVATE);
        }

        public static void DockToParentWindow(RECT parentRect, IntPtr hWndParent, IntPtr hWndChild, Alignment alignment)
        {
            if (!IsValidHWnd(hWndChild))
                return;
            if (!IsValidHWnd(hWndParent))
                return;
            var childRect = GetWindowRect(hWndChild);

            switch (alignment)
            {
                case Alignment.TOP:
                    SetWindowsPos(hWndChild, parentRect.Width > childRect.Width ? parentRect.Left + (parentRect.Width - childRect.Width) / 2 : parentRect.X,
                        parentRect.Y + 15);
                    break;
                case Alignment.TOPRIGHT:
                    // TODO: not implemented yet
                    break;
                case Alignment.RIGHT:
                    // TODO: not implemented yet
                    break;
                case Alignment.RIGHTBOT:
                    SetWindowsPos(hWndChild, parentRect.Right - childRect.Width - 6, parentRect.Bottom - childRect.Height - 6);
                    break;
                case Alignment.BOT:
                    // TODO: not implemented yet
                    break;
                case Alignment.BOTLEFT:
                    // TODO: not implemented yet
                    break;
                case Alignment.LEFT:
                    // TODO: not implemented yet
                    break;
                case Alignment.LEFTTOP:
                    // TODO: not implemented yet
                    break;
                default:
                    break;
            }
        }

        public static bool IsValidHWnd(IntPtr hWnd)
        {
            return Pinvokes.IsWindow(hWnd);
        }

        /// <summary>
        ///     alpha: 0: the window is completely transparent ... 255: the window is opaque
        /// </summary>
        public static void SetClickThrough(IntPtr hWnd, byte alpha, Color? transparencyKeycolor = null)
        {
            if (transparencyKeycolor == null)
                transparencyKeycolor = Color.Black;

            var wl = Pinvokes.User32_GetWindowLong(hWnd, GetWindowLong.GWL_EXSTYLE);
            Pinvokes.User32_SetWindowLong(hWnd, GetWindowLong.GWL_EXSTYLE,
                wl | (int)ExtendedWindowStyles.WS_EX_LAYERED | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT);

            Pinvokes.User32_SetLayeredWindowAttributes(hWnd,
                (((Color)transparencyKeycolor).B << 16) + (((Color)transparencyKeycolor).G << 8) + ((Color)transparencyKeycolor).R, alpha,
                LayeredWindowAttributes.LWA_COLORKEY | LayeredWindowAttributes.LWA_ALPHA);
        }

        public static bool IsMinimized(IntPtr hWnd)
        {
            return Pinvokes.IsIconic(hWnd);
        }

        public static void ForcePaint(IntPtr hWnd)
        {
            Pinvokes.SendMessage(hWnd, Pinvokes.WmPaint, IntPtr.Zero, IntPtr.Zero);
        }

        public static void CloseWindow(IntPtr hWnd)
        {
            Pinvokes.SendMessage((int)hWnd, (uint)WM.SYSCOMMAND, (int)SysCommands.SC_CLOSE, 0);
        }

        public static void ShowWindow(IntPtr hWnd)
        {
            Pinvokes.ShowWindow(hWnd, Pinvokes.SW_SHOW);
        }

        public static void HideWindow(IntPtr hWnd)
        {
            Pinvokes.ShowWindow(hWnd, Pinvokes.SW_HIDE);
        }

        public static void SetOpacity(IntPtr hWnd, byte alpha)
        {
            Pinvokes.SetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE, Pinvokes.GetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE) ^ (int)Pinvokes.WS_EX_LAYERED);
            Pinvokes.SetLayeredWindowAttributes(hWnd, 0, alpha, Pinvokes.LWA_ALPHA);
        }

        public static void RemoveFromTaskbar(IntPtr hWnd)
        {
            Pinvokes.SetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE, Pinvokes.GetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE) | Pinvokes.WS_EX_TOOLWINDOW);
        }

        public static bool IsWindowAddedToTaskbar(IntPtr hWnd)
        {
            return (Pinvokes.GetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE) & Pinvokes.WS_EX_TOOLWINDOW) == 0;
        }

        public static void AddToTaskbar(IntPtr hWnd)
        {
            Pinvokes.SetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE, Pinvokes.GetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE) & ~Pinvokes.WS_EX_TOOLWINDOW);
        }

        public static void RemoveWS_EX_NOACTIVATE(IntPtr hWnd)
        {
            Pinvokes.SetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE, Pinvokes.GetWindowLong(hWnd, Pinvokes.GWL_EXSTYLE) & ~(int)Pinvokes.WS_EX_NOACTIVATE);
        }

        public static void SetParent(IntPtr hWndParent, IntPtr hWndChild)
        {
            Pinvokes.SetParent(hWndChild, hWndParent);
        }

        public static RECT GetWindowRect(IntPtr hWnd)
        {
            RECT rect;
            Pinvokes.GetWindowRect(hWnd, out rect);
            return rect;
        }

        public static void DestroyWindow(IntPtr hWnd)
        {
            Pinvokes.DestroyWindow(hWnd);
        }


        public static void SetForegroundWindow(IntPtr hWnd)
        {
            Pinvokes.SetForegroundWindow(hWnd);
        }

        public static void BringWindowToTop(IntPtr hWnd)
        {
            Pinvokes.BringWindowToTop(hWnd);
        }

        //public static IntPtr GetForegroundWindow()
        //{
        //    return Pinvokes.GetForegroundWindow();
        //}

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam,
            StringBuilder lParam);

        public static IntPtr GetEveHWnd(int pid)
        {
            var windows = Util.GetInvisibleWindows(pid).Concat(Util.GetVisibleWindows(pid));
            foreach (var w in windows)
            {
                var className = new StringBuilder(100);
                var builderClassname = new StringBuilder(255);
                Pinvokes.GetClassName(w.Key, builderClassname, builderClassname.Capacity);

                if (builderClassname.ToString().Trim().Equals("tri") || builderClassname.ToString().Trim().Equals("trinityWindow"))
                    return w.Key;
            }
            return IntPtr.Zero;
        }


        private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) =>
                    {
                        handles.Add(hWnd);
                        return true;
                    }, IntPtr.Zero);

            return handles;
        }


        public static IDictionary<IntPtr, WindowInfo> GetOpenWindows(int pid)
        {
            var windows = new Dictionary<IntPtr, WindowInfo>();
            foreach (var handle in EnumerateProcessWindowHandles(pid))
            {
                var title = new StringBuilder(1000);
                var message = new StringBuilder(1000);
                SendMessage(handle, WM_GETTEXT, message.Capacity, message);
                var builderClassname = new StringBuilder(255);
                Pinvokes.GetClassName(handle, builderClassname, 255);
                var WindowClassname = builderClassname.ToString();
                RECT rect;
                Pinvokes.GetWindowRect(handle, out rect);
                var wInfo = new WindowInfo(0, pid, title.ToString(), WindowClassname, rect);
                windows.Add(handle, wInfo);
            }
            return windows;
        }

        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title/classname of all the open windows.</returns>
        public static IDictionary<IntPtr, WindowInfo> GetOpenWindows()
        {
            var shellWindow = Pinvokes.GetShellWindow();
            var windows = new Dictionary<IntPtr, WindowInfo>();

            Pinvokes.EnumWindows(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                //                if (!Pinvokes.IsWindowVisible(hWnd)) return true;

                var length = Pinvokes.GetWindowTextLength(hWnd);
                var windowTitle = String.Empty;
                if (length != 0)
                {
                    var builderTitle = new StringBuilder(255);
                    Pinvokes.GetWindowText(hWnd, builderTitle, length + 1);
                    windowTitle = builderTitle.ToString();
                }

                var processId = 0;
                var threadId = Pinvokes.GetWindowThreadProcessId(hWnd, out processId);

                var builderClassname = new StringBuilder(255);
                Pinvokes.GetClassName(hWnd, builderClassname, length + 1);
                var WindowClassname = builderClassname.ToString();

                RECT rect;
                Pinvokes.GetWindowRect(hWnd, out rect);

                windows[hWnd] = new WindowInfo(threadId, processId, windowTitle, WindowClassname, rect);

                return true;
            }, 0);

            return windows;
        }

        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
    }
}