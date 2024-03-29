﻿using System;
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
using TaskWorkspace.Backup;
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
            _handlers.Add(PkgCmdId.cmdDropboxBackup, DropboxBackupCallback);
            _handlers.Add(PkgCmdId.cmdDropboxRestore,DropboxRestoreCallback);
            _handlers.Add(PkgCmdId.cmdGoogleBackup, GoogleBackupCallback);
            _handlers.Add(PkgCmdId.cmdGoogleRestore,GoogleRestoreCallback);
            _handlers.Add(PkgCmdId.cmdidAdd, AddWorkspace);


            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService service)
            {
                foreach (var valuePair in _handlers)
                    service.AddCommand(new OleMenuCommand(valuePair.Value,
                        new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, valuePair.Key)));

                var cmdSelectedWorkspaceId = new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, PkgCmdId.cmdidWorkspaces);
                var workspaceCommand = new OleMenuCommand(new EventHandler(SelectedWorkspaceCallback), new EventHandler(ChangedSelectedWorkspaceCallback), new EventHandler(BeforeQueryStatusChangedWorkspaceCallback), cmdSelectedWorkspaceId);
                

                var cmdGetWorkspacesId =
                    new CommandID(PkgGuids.GuidTaskWorkspaceCmdSet, PkgCmdId.cmdidWorkspacesGetList);
                var getWorkspacesCommand = new OleMenuCommand(new EventHandler(GetWorkspacesCallback), cmdGetWorkspacesId);

                service.AddCommand(workspaceCommand);
                service.AddCommand(getWorkspacesCommand);
            }

            _workspaceService = new WorkspaceService(ServiceProvider, ServiceProvider.GetService(typeof(IVsSolution)) as IVsSolution,
                ServiceProvider.GetService(typeof(DTE)) as DTE,ServiceProvider.GetService(typeof(IVsUIShellDocumentWindowMgr)) as IVsUIShellDocumentWindowMgr);
        }

        private void BeforeQueryStatusChangedWorkspaceCallback(object sender, EventArgs args)
        {
            dynamic ctrl = sender;
            WorkspaceLogger.Log.Debug($"New value: {ctrl.Text}");

        }

        private void ChangedSelectedWorkspaceCallback(object sender, EventArgs args)
        {
            if (!(args is OleMenuCmdEventArgs menuArgs))
                return;


            var newValue = menuArgs.InValue as string;
            var vOut = menuArgs.OutValue;

            WorkspaceLogger.Log.Debug($"New value: {newValue}");
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

        private void AddWorkspace(object sender, EventArgs args)
		{
            _workspaceService?.AddWorkspace();
		}

        private void SelectedWorkspaceCallback(object sender, EventArgs args)
        {
            
            if (!(args is OleMenuCmdEventArgs menuArgs))
                return;

            
            var newValue = menuArgs.InValue as string;
            var vOut = menuArgs.OutValue;

            WorkspaceLogger.Log.Debug($"New value: {newValue}");

            if (vOut != IntPtr.Zero)
                Marshal.GetNativeVariantForObject(_workspaceService.SelectedWorkspace, vOut);
            else if (newValue != null) _workspaceService.SelectedWorkspace = newValue;
        }

        private void GetWorkspacesCallback(object sender, EventArgs args)
        {
            
            if (!(args is OleMenuCmdEventArgs menuArgs))
                return;

            var inParam = menuArgs.InValue;
            var vOut = menuArgs.OutValue;

            if(inParam != null)
            {
                WorkspaceLogger.Log.Error("Input param is illegal");
            }
            else if(vOut != IntPtr.Zero)
            {

                var workspaces = _workspaceService.GetWorkspaces().ToArray<string>();
                Marshal.GetNativeVariantForObject(workspaces, vOut);
            }
            else
            {
                WorkspaceLogger.Log.Error("Output param is required");
            }
        }

        private void DropboxBackupCallback ( object sender,EventArgs args )
        {
            _workspaceService?.BackupWorkspace(StorageType.Dropbox);
        }

        private void DropboxRestoreCallback ( object sender,EventArgs args )
        {
            _workspaceService?.RestoreWorkspace(StorageType.Dropbox);
        }

        private void GoogleBackupCallback ( object sender,EventArgs args )
        {
            _workspaceService?.BackupWorkspace(StorageType.Google);
        }

        private void GoogleRestoreCallback ( object sender,EventArgs args )
        {
            _workspaceService?.RestoreWorkspace(StorageType.Google);
        }



        private void OutputCommandString(string text)
        {
            var windowPane = (IVsOutputWindowPane) ServiceProvider.GetService(typeof(SVsGeneralOutputWindowPane));
            if(windowPane != null && ErrorHandler.Failed(windowPane.OutputString(text)))
            {
                WorkspaceLogger.Log.Error("Failed to write on the Output Window");
            }
        }
    }
}