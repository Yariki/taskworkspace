using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using EnvDTE;
using Microsoft.Build.Tasks.Deployment.ManifestUtilities;
using TaskWorkspace.Services;

namespace TaskWorkspace.Infrastructure
{
    public class CommandManager : IDisposable
    {

        private IDictionary<int,EventHandler> _handlers = new Dictionary<int,EventHandler>();
        private Package _package;
        private WorkspaceService _workspaceService;
        
        private IServiceProvider ServiceProvider  => _package as IServiceProvider;

        
        public CommandManager(Package package)
        {
            _package = package;
            _handlers.Add(PkgCmdId.cmdidSave, new EventHandler(this.SaveCommandCallback));
            _handlers.Add(PkgCmdId.cmdidLoad,new EventHandler(this.LoadCommandCallback));
            _handlers.Add(PkgCmdId.cmdidDelete,new EventHandler(this.DeleteCommandCallback));

            if(ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService service) 
            {
                foreach (var valuePair in _handlers)
                {
                    service.AddCommand(new MenuCommand(valuePair.Value,
                        new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, valuePair.Key)));
                }

                var cmdSelectedWorkspaceId = new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet,PkgCmdId.cmdidWorkspaces);
                var workspaceCommand = new MenuCommand(this.SelectedWorkspaceCallback, cmdSelectedWorkspaceId);


                var cmdGetWorkspacesId = new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, PkgCmdId.cmdidWorkspacesGetList);
                var getWorkspacesCommand = new MenuCommand(this.GetWorkspacesCallback, cmdGetWorkspacesId);

                service.AddCommand(workspaceCommand);
                service.AddCommand(getWorkspacesCommand);
            }

            _workspaceService = new WorkspaceService(ServiceProvider.GetService(typeof(IVsSolution)) as IVsSolution, 
                ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE);

        }

        public void Dispose()
        {
            _workspaceService?.Dispose();
        }


        private void SaveCommandCallback(object sender, EventArgs args)
        {
            var dte = ServiceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            OutputCommandString("Save Command");

            foreach (Document dteDocument in dte.Documents)
            {
                //OutputCommandString(dteDocument.FullName);
            }
            
            //dte.Documents.Open("c:\\Users\\Yariki\\source\\repos\\WindowsFormsApp1\\WindowsFormsApp1\\Program.cs");

            foreach (Breakpoint breakpoint in dte.Debugger.Breakpoints)
            {
                //OutputCommandString($"File: {breakpoint.File}, Line: {breakpoint.FileLine}");
            }


            dte.Debugger.Breakpoints.Add(File: "c:\\Users\\Yariki\\source\\repos\\WindowsFormsApp1\\WindowsFormsApp1\\Program.cs", Line: 16);

            dte.Debugger.Breakpoints.Add(File: "c:\\Users\\Yariki\\source\\repos\\WindowsFormsApp1\\WindowsFormsApp1\\Program.cs", Line: 18);



            _workspaceService?.SaveWorkspace();
        }

        private void LoadCommandCallback(object sender, EventArgs args) 
        {
            _workspaceService?.LoadWorkspace();
        }

        private void  DeleteCommandCallback(object sender, EventArgs args) 
        {
            _workspaceService?.DeleteWorkspace();
        }

        private void SelectedWorkspaceCallback(object sender, EventArgs args) 
        {
            if(!(args is OleMenuCmdEventArgs menuArgs))
                return;

            var newValue = menuArgs.InValue as string;
            var vOut = menuArgs.OutValue;

            if(vOut != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(_workspaceService.SelectedWorkspace,vOut);
            }
            else if (newValue != null)
            {
                _workspaceService.SelectedWorkspace = newValue;
            }
        }

        private void GetWorkspacesCallback(object sender, EventArgs args) 
        {
            if(!(args is OleMenuCmdEventArgs menuArgs))
                return;
            var inParam = menuArgs.InValue;
            var vOut = menuArgs.OutValue;

            if(inParam != null)
            {
                // TODO log inparameter is illegal
            }
            else if(vOut != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(_workspaceService.GetWorkspaces(),vOut);
            }
            else
            {
                // TODO log Out Param is required
            }
        }


        private void OutputCommandString(string text)
        {
            // Build the string to write on the debugger and Output window.
            StringBuilder outputText = new StringBuilder();
            outputText.Append(" ================================================\n");
            outputText.AppendFormat("  MenuAndCommands: {0}\n", text);
            outputText.Append(" ================================================\n\n");

            IVsOutputWindowPane windowPane = (IVsOutputWindowPane)ServiceProvider.GetService(typeof(SVsGeneralOutputWindowPane));
            if (null == windowPane)
            {
                Debug.WriteLine("Failed to get a reference to the Output window General pane");
                return;
            }
            if (Microsoft.VisualStudio.ErrorHandler.Failed(windowPane.OutputString(outputText.ToString())))
            {
                Debug.WriteLine("Failed to write on the Output window");
            }
        }


    }
}