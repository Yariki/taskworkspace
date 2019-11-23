using System;
using System.Runtime.InteropServices;
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
    }
}