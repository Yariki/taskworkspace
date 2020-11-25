using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using EnvDTE;
using TaskWorkspace.Helpers;
using Window = System.Windows.Window;

namespace TaskWorkspace.Hooks
{
    public class KeyboardSystem : BaseHookSystem
    {

        private readonly NativeHelpers.HookProc _proc = KeyboardHookProc;

        public KeyboardSystem()
            : base()
        {
        }

        protected override Delegate GetCallback() => _proc;

        protected override int GetHookType() => NativeHelpers.WH_KEYBOARD;

        private static IntPtr KeyboardHookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code < 0)
                NativeHelpers.CallNextHookEx(((BaseHookSystem) GetHookSystem()).GetHookPtr(), code, wParam, lParam);

            if (code == 0)
            {
                var focusHandle = NativeHelpers.GetFocus();
                var text = NativeHelpers.GetText(focusHandle);
                System.Diagnostics.Debug.WriteLine($"Focused text: {text}");
                var automationElement = AutomationElement.FromHandle(focusHandle);
                System.Diagnostics.Debug.WriteLine($"Automated: {automationElement?.Current.Name}");

                Window window = (Window) HwndSource.FromHwnd(focusHandle).RootVisual;
                System.Diagnostics.Debug.WriteLine($"Window: {window?.Title}");

                if (window != null)
                {
                    var el = window.GetChildren<ToolBar>();
                }
            }

            return NativeHelpers.CallNextHookEx(((BaseHookSystem) GetHookSystem()).GetHookPtr(), code, wParam, lParam);
        }

    }
}