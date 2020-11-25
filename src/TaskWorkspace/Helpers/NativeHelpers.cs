using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dropbox.Api.TeamLog;
using Microsoft.VisualStudio.OLE.Interop;

namespace TaskWorkspace.Helpers
{
    internal class NativeHelpers
    {
        [DllImport("Ole32.dll")]
        internal static extern void CreateStreamOnHGlobal(
            IntPtr hGlobal,
            [MarshalAs(UnmanagedType.Bool)] bool deleteOnRelease,
            out IStream stream);


        private delegate bool EnumWindowsProc(IntPtr hWnd, ref SearchData data);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref SearchData data);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowsProc callback, ref SearchData i);

        [DllImport("user32.dll")]
        internal static extern int GetDlgCtrlID(IntPtr hwndCtl);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindowA(string lpClassName, string lpWindowName);


        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, Delegate callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, int wParam, ref KeyboardHookStruct lParam);

        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        public static IntPtr StructToPtr(object obj)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetDlgItemText(IntPtr hDlg, int nIDDlgItem, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetFocus();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);


        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WH_MOUSE = 7;
        public const int WH_KEYBOARD = 2;

        public class SearchData
        {
            public string Wndclass;
            public string Title;
            public IntPtr hWnd;
            public List<string> list;
            public List<IntPtr> listPtr;
        }

        public static List<IntPtr> GetAllObjectWithClass(string wndClass)
        {
            SearchData sd = new SearchData { Wndclass = wndClass, listPtr = new List<IntPtr>() };
            EnumWindows(new EnumWindowsProc(EnumProc1), ref sd);
            return sd.listPtr;
        }

        public static List<IntPtr> GetAllChildWindowsWithClass(IntPtr hWndParent, string wndClass)
        {
            SearchData sd = new SearchData { Wndclass = wndClass, listPtr = new List<IntPtr>() };
            EnumChildWindows(hWndParent, new EnumWindowsProc(EnumProcChild), ref sd);

            return sd.listPtr;
        }

        public static IntPtr SearchForWindow(string wndclass, string title)
        {
            SearchData sd = new SearchData { Wndclass = wndclass, Title = title };
            EnumWindows(new EnumWindowsProc(EnumProc), ref sd);
            return sd.hWnd;
        }

        public static List<IntPtr> GetChildControlList(string parentClass, string parentName, string childClass)
        {
            var windowHandle = SearchForWindow(parentClass, parentName);
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            SearchData sd = new SearchData { Wndclass = childClass, listPtr = new List<IntPtr>() };
            ProcessChildControls(windowHandle,  ref sd);
            return sd.listPtr;
        }

        private static void ProcessChildControls(IntPtr parent, ref SearchData sd)
        {
            EnumChildWindows(parent, new EnumWindowsProc(ProcessChildWindowCallback) , ref sd);
        }

        private static bool ProcessChildWindowCallback(IntPtr wnd, ref SearchData data)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetClassName(wnd, sb, sb.Capacity);
            var temp = sb.ToString();
            if (temp.StartsWith(data.Wndclass) || temp.Contains(data.Wndclass))
            {
                data.listPtr.Add(wnd);
            }
            ProcessChildControls(wnd, ref data);
            return true;
        }

        private static bool EnumProc1(IntPtr hWnd, ref SearchData data)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);
            var temp = sb.ToString();
            if (temp.StartsWith(data.Wndclass) || temp.Contains(data.Wndclass)) 
            {
                data.listPtr.Add(hWnd);
            }
            return true;
        }

        private static bool EnumProcChild(IntPtr hWnd, ref SearchData data)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);
            var temp = sb.ToString();
            if (temp.StartsWith(data.Wndclass) || temp.Contains(data.Wndclass))
            {
                data.listPtr.Add(hWnd);
            }
            return true;
        }

        public static bool EnumProc(IntPtr hWnd, ref SearchData data)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetClassName(hWnd, sb, sb.Capacity);
            if (sb.ToString().StartsWith(data.Wndclass))
            {
                sb = new StringBuilder(1024);
                GetWindowText(hWnd, sb, sb.Capacity);
                if (sb.ToString().StartsWith(data.Title) || sb.ToString().Contains(data.Title))
                {
                    data.hWnd = hWnd;
                    return false; }
            }
            return true;
        }

        internal static string GetText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

    }
}