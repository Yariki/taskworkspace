using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using TaskWorkspace.Helpers;

namespace TaskWorkspace.Hooks
{
    public abstract class BaseHookSystem 
    {
        protected static IntPtr _hhook = IntPtr.Zero;

        private static BaseHookSystem _this;


        protected BaseHookSystem()
        {
            _this = this;
        }


        public virtual void StartSystem()
        {
            SetHook();
        }

        public virtual void StopSystem()
        {
            ReleaseHook();
        }

        private void SetHook()
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
            {
                _hhook = NativeHelpers.SetWindowsHookEx(GetHookType(), GetCallback(), IntPtr.Zero, NativeHelpers.GetCurrentThreadId());
            }
        }

        private void ReleaseHook()
        {
            Contract.Requires(_hhook != IntPtr.Zero);
            NativeHelpers.UnhookWindowsHookEx(_hhook);
        }

        public IntPtr GetHookPtr() => _hhook;

        protected abstract Delegate GetCallback();

        protected abstract int GetHookType();

        protected static BaseHookSystem GetHookSystem() => _this;
        
    }
}