using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TaskWorkspace.Services;

namespace TaskWorkspace.Infrastructure
{
    public class CommandManager : IDisposable
    {
        private readonly IDictionary<int, EventHandler> _handlers = new Dictionary<int, EventHandler>();
        private readonly Package _package;
        private readonly WorkspaceService _workspaceService;


        public CommandManager(Package package)
        {
            _package = package;
            _handlers.Add(PkgCmdId.cmdidSave, SaveCommandCallback);
            _handlers.Add(PkgCmdId.cmdidLoad, LoadCommandCallback);
            _handlers.Add(PkgCmdId.cmdidDelete, DeleteCommandCallback);

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService service)
            {
                foreach (var valuePair in _handlers)
                    service.AddCommand(new OleMenuCommand(valuePair.Value,
                        new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, valuePair.Key)));

                var cmdSelectedWorkspaceId = new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, PkgCmdId.cmdidWorkspaces);
                var workspaceCommand = new OleMenuCommand(new EventHandler(SelectedWorkspaceCallback), cmdSelectedWorkspaceId);


                var cmdGetWorkspacesId =
                    new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, PkgCmdId.cmdidWorkspacesGetList);
                var getWorkspacesCommand = new OleMenuCommand(new EventHandler(GetWorkspacesCallback), cmdGetWorkspacesId);

                service.AddCommand(workspaceCommand);
                service.AddCommand(getWorkspacesCommand);
            }

            _workspaceService = new WorkspaceService(ServiceProvider, ServiceProvider.GetService(typeof(IVsSolution)) as IVsSolution,
                ServiceProvider.GetService(typeof(DTE)) as DTE);
        }

        private IServiceProvider ServiceProvider => _package;

        public void Dispose()
        {
            _workspaceService?.Dispose();
        }


        private void SaveCommandCallback(object sender, EventArgs args)
        {
            _workspaceService?.SaveWorkspace();
        }

        private void LoadCommandCallback(object sender, EventArgs args)
        {
            _workspaceService?.LoadWorkspace();
        }

        private void DeleteCommandCallback(object sender, EventArgs args)
        {
            _workspaceService?.DeleteWorkspace();
        }

        private void SelectedWorkspaceCallback(object sender, EventArgs args)
        {
            
            if (!(args is OleMenuCmdEventArgs menuArgs))
                return;

            
            var newValue = menuArgs.InValue as string;
            var vOut = menuArgs.OutValue;

            OutputCommandString($"SelectedWorkspace Callback: {newValue}");

            if (vOut != IntPtr.Zero)
                Marshal.GetNativeVariantForObject(_workspaceService.SelectedWorkspace, vOut);
            else if (newValue != null) _workspaceService.SelectedWorkspace = newValue;
        }

        private void GetWorkspacesCallback(object sender, EventArgs args)
        {
            
            if (!(args is OleMenuCmdEventArgs menuArgs))
                return;

            OutputCommandString($"GetWorkspaces Callback: {args is OleMenuCmdEventArgs}");

            var inParam = menuArgs.InValue;
            var vOut = menuArgs.OutValue;

            if(inParam != null)
            {
                WorkspaceLogger.Log.Error("Input param is illegal");
            }
            else if(vOut != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(_workspaceService.GetWorkspaces().ToArray<string>(), vOut);
            }
            else
            {
                WorkspaceLogger.Log.Error("Output param is required");
            }
        }


        private void OutputCommandString(string text)
        {
            // Build the string to write on the debugger and Output window.
            var outputText = new StringBuilder();
            outputText.Append(" ================================================\n");
            outputText.AppendFormat("  MenuAndCommands: {0}\n", text);
            outputText.Append(" ================================================\n\n");

            var windowPane = (IVsOutputWindowPane) ServiceProvider.GetService(typeof(SVsGeneralOutputWindowPane));
            if (null == windowPane)
            {
                Debug.WriteLine("Failed to get a reference to the Output window General pane");
                return;
            }

            if (ErrorHandler.Failed(windowPane.OutputString(outputText.ToString())))
                Debug.WriteLine("Failed to write on the Output window");
        }
    }
}