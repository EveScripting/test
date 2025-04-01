/*
 * ---------------------------------------
 * User: duketwo
 * Date: 19.06.2016
 * Time: 15:05
 * 
 * ---------------------------------------
 */

using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SharedComponents.WinApiUtil
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CWPRETSTRUCT
    {
        public IntPtr lResult;
        public IntPtr lParam;
        public IntPtr wParam;
        public uint message;
        public IntPtr hWnd;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct MSG
    {
        public IntPtr hwnd;
        public UInt32 message;
        public UIntPtr wParam;
        public UIntPtr lParam;
        public UInt32 time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CWPSTRUCT
    {
        public IntPtr lparam;
        public IntPtr wparam;
        public int message;
        public IntPtr hwnd;
    }

    public class WindowInfo
    {
        public int ProcessId;
        public string WindowClassname;
        public RECT WindowRect;

        public int WindowThreadId;
        public string WindowTitle;

        public WindowInfo(int windowThreadId, int processId, string windowTitle, string windowClassname, RECT windowRect)
        {
            WindowThreadId = windowThreadId;
            ProcessId = processId;
            WindowTitle = windowTitle;
            WindowClassname = windowClassname;
            WindowRect = windowRect;
        }
    }

    //Declare the wrapper managed POINT class.
    [StructLayout(LayoutKind.Sequential)]
    public class POINT
    {
        public int x;
        public int y;
    }

    //Declare the wrapper managed MouseHookStruct class.
    [StructLayout(LayoutKind.Sequential)]
    public class MouseHookStruct
    {
        public int dwExtraInfo;
        public int hwnd;
        public POINT pt;
        public int wHitTestCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public RECT(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom)
        {
        }

        public int X
        {
            get => Left;
            set
            {
                Right -= Left - value;
                Left = value;
            }
        }

        public int Y
        {
            get => Top;
            set
            {
                Bottom -= Top - value;
                Top = value;
            }
        }

        public int Height
        {
            get => Bottom - Top;
            set => Bottom = value + Top;
        }

        public int Width
        {
            get => Right - Left;
            set => Right = value + Left;
        }

        public Point Location
        {
            get => new Point(Left, Top);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Size Size
        {
            get => new Size(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        public static implicit operator Rectangle(RECT r)
        {
            return new Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator RECT(Rectangle r)
        {
            return new RECT(r);
        }

        public static bool operator ==(RECT r1, RECT r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(RECT r1, RECT r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(RECT r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is RECT)
                return Equals((RECT) obj);
            else if (obj is Rectangle)
                return Equals(new RECT((Rectangle) obj));
            return false;
        }

        public override int GetHashCode()
        {
            return ((Rectangle) this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }
}