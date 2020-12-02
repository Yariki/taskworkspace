using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Automation;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using TaskWorkspace.Helpers;

namespace TaskWorkspace.Services
{
    public class WorkspaceNameTextChangedService : IDisposable
    {
        private IntPtr _workSpaceEditBox;
        private static readonly string ClassName = "TextBox";
        private static readonly string ControlName = "Workspace";

        public WorkspaceNameTextChangedService()
        {   
        }

        public void InitializeControls()
        {
            if (_workSpaceEditBox != IntPtr.Zero)
            {
                return;
            }

            var textboxes = NativeHelpers.GetChildControlList("Window", "Visual Studio", "TextBox");
            if (textboxes != null && textboxes.Any())
            {
                Debug.WriteLine(textboxes.Count);
            }
        }

        public void DeinitializeControls()
        {
            _workSpaceEditBox = IntPtr.Zero;
        }



        public void Dispose()
        {
        }
    }
}