using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AipptPlayerAddIn
{
    internal static class Win32WindowHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public static Rectangle GetClientScreenBounds(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            {
                return Rectangle.Empty;
            }

            RECT rect;
            if (!GetClientRect(hwnd, out rect))
            {
                return Rectangle.Empty;
            }

            var point = new POINT { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref point))
            {
                return Rectangle.Empty;
            }

            return new Rectangle(
                point.X,
                point.Y,
                Math.Max(0, rect.Right - rect.Left),
                Math.Max(0, rect.Bottom - rect.Top));
        }
    }
}
