using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NLog.LayoutRenderers.Wrappers;
using TaskWorkspace.Backup;
using TaskWorkspace.DataAccess;
using TaskWorkspace.EventArguments;
using TaskWorkspace.Helpers;
using TaskWorkspace.Infrastructure;
using TaskWorkspace.Model;
using Breakpoint = EnvDTE.Breakpoint;
using Document = EnvDTE.Document;
using FILETIME = Microsoft.VisualStudio.OLE.Interop.FILETIME;
using IServiceProvider = System.IServiceProvider;
using WorkspaceDocument = TaskWorkspace.Model.Document;
using WorkspaceBreakpoint = TaskWorkspace.Model.Breakpoint;

namespace TaskWorkspace.Services
{
    public class WorkspaceService : IDisposable
    {
        private readonly DTE _dte;
        private readonly SolutionEventsService _eventsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsUIShellDocumentWindowMgr _documentWindowMgr;
        private bool _isSolutionOpened;


        private readonly WorkspaceRepository _repository;

        private readonly IVsSolution _solution;
        private IEnumerable<string> _workspaces;

        public WorkspaceService(IServiceProvider serviceProvider, IVsSolution solution, DTE dte,IVsUIShellDocumentWindowMgr documentWindowMgr)
        {
            _solution = solution;
            _dte = dte;
            _serviceProvider = serviceProvider;
            _documentWindowMgr = documentWindowMgr;

            _eventsService = new SolutionEventsService(_solution);
            _repository = new WorkspaceRepository(_solution);

            _eventsService.SolutionOpened += SolutionOpened;
            _eventsService.SolutionClosed += SolutionClosed;
        }

        public string SelectedWorkspace { get; set; }

        public void Dispose()
        {
            if (_eventsService != null)
            {
                _eventsService.SolutionOpened -= SolutionOpened;
                _eventsService.SolutionClosed -= SolutionClosed;
                _eventsService.Dispose();
            }
        }

        public IEnumerable<string> GetWorkspaces()
        {
            return _isSolutionOpened ? _workspaces : new string[1];
        }

        public void SaveWorkspace()
        {
            if(string.IsNullOrEmpty(SelectedWorkspace))
            {
                WorkspaceLogger.Log.Info("Selected workspace is empty");
                return;
            }
            if(_repository.IsExist(SelectedWorkspace))
            {
                if(VsShellUtilities.ShowMessageBox(_serviceProvider,
                   $"Do you want to overwrite the '{SelectedWorkspace}' workspace?",
                   "Workspace Manager",
                   OLEMSGICON.OLEMSGICON_QUERY,
                   OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                   OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND) != 6)
                    return;

                var updateitems = GetWorkspaceItem();
                _repository.UpdateWorkspace(SelectedWorkspace,updateitems.Item1, updateitems.Item2);
            }
            else
            {
                var items = GetWorkspaceItem();
                _repository.SaveWorkspace(SelectedWorkspace,items.Item1, items.Item2);
            }
            _workspaces = _repository.GetWorkspaces();
        }

        private (List<WorkspaceBreakpoint>,string) GetWorkspaceItem()
        {
            var breakpoints = new List<WorkspaceBreakpoint>();
            foreach (Breakpoint breakpoint in _dte.Debugger.Breakpoints)
            {
                breakpoints.Add(new WorkspaceBreakpoint()
                {
                    Filename = breakpoint.File,
                    Line = breakpoint.FileLine,
                    Enabled = breakpoint.Enabled
                });
            }

            IStream stream;
            NativeHelpers.CreateStreamOnHGlobal(IntPtr.Zero, true,out stream);
            ErrorHandler.ThrowOnFailure(_documentWindowMgr.SaveDocumentWindowPositions(0U,stream));
            stream.Rewind();
            var windowsBase64 = System.Convert.ToBase64String(stream.ToByteArray());

            return (breakpoints,windowsBase64);
        }

        public void LoadWorkspace()
        {
            if(string.IsNullOrEmpty(SelectedWorkspace))
            {
                WorkspaceLogger.Log.Info("Selected workspace is empty");
                return;
            }
            ClearWorkspace();

            var workspace = _repository.GetWorkspace(SelectedWorkspace);
            AddBreakpoints(workspace.Breakpoints);
            RestoreWindows(workspace.WindowsBase64);
        }

        private void RestoreWindows(string workspaceWindowsBase64)
        {
            var byteWindows = GetWindowsArray(workspaceWindowsBase64);

            IStream stream;
            NativeHelpers.CreateStreamOnHGlobal(IntPtr.Zero, true, out stream);
            uint pcbWritten;
            stream.Write(byteWindows,(uint)byteWindows.Length,out pcbWritten);
            stream.Rewind();
            ErrorHandler.ThrowOnFailure(_documentWindowMgr.ReopenDocumentWindows(stream));
        }

        private byte[] GetWindowsArray(string base64)
        {
            var bytes = System.Convert.FromBase64String(base64);
            return bytes;
        }

        public void DeleteWorkspace()
        {
            if(string.IsNullOrEmpty(SelectedWorkspace))
            {
                WorkspaceLogger.Log.Info("Selected workspace is empty");
                return;
            }
            ClearWorkspace();
            _repository.DeleteWorkspace(SelectedWorkspace);
            SelectedWorkspace = null;
			_workspaces = _repository.GetWorkspaces();
        }

        public async System.Threading.Tasks.Task RestoreWorkspace(StorageType storageType)
        {
	        var fullFileName = $"{_repository.SolutionFolder}\\{_repository.Filename}";
            var backupWorkspace = new BackupWorkspace(storageType,fullFileName,_repository.Filename);
            RemoveExistingWorkspaceFile(fullFileName);
            await backupWorkspace.Restore();
        }

        private static void RemoveExistingWorkspaceFile(string fullFileName)
        {
	        if (System.IO.File.Exists(fullFileName))
	        {
		        System.IO.File.Delete(fullFileName);
	        }
        }

        public async System.Threading.Tasks.Task BackupWorkspace (StorageType storageType)
        {
            var fullFileName = $"{_repository.SolutionFolder}\\{_repository.Filename}";
            var backupWorkspace = new BackupWorkspace(storageType,fullFileName,_repository.Filename);
            await backupWorkspace.Backup();
        }

        private void SolutionOpened(object sender, OpenedSolutionArgs e)
        {
            _isSolutionOpened = true;
            var workspaces = _repository.GetWorkspaces();
            _workspaces = workspaces ?? new List<string>();
        }

        private void SolutionClosed(object sender, EventArgs e)
        {
            _isSolutionOpened = false;
            _workspaces = new List<string>();
            SelectedWorkspace = null;
        }

        private void AddBreakpoints(List<WorkspaceBreakpoint> breakpoints)
        {
            using(var breakpointHelper = new BreakpointHelper())
            {
	            breakpoints.ForEach(b =>
	            {
		            try
		            {
						if(breakpointHelper.CanBreakpointBeSet(b.Filename,b.Line))
						{
							_dte.Debugger.Breakpoints.Add(File: b.Filename, Line: b.Line);
						}
						else
						{
							WorkspaceLogger.Log.Info($"Breakpoint skipped: {b.Filename}; {b.Line}");							
						}

		            }
		            catch (COMException e)
		            {
						WorkspaceLogger.Log.Info($"Filename: {b.Filename}; Line: {b.Line}");
						WorkspaceLogger.Log.Error(e);
		            }
                    catch(Exception e)
                    {
						WorkspaceLogger.Log.Error(e);
                    }
	            });

            }
            foreach (Breakpoint breakpoint in _dte.Debugger.Breakpoints)
            {
                var wsBreak = breakpoints.FirstOrDefault(b => b.Filename == breakpoint.File && b.Line == breakpoint.FileLine);
                if(wsBreak != null)
                {
                    breakpoint.Enabled = wsBreak.Enabled;
                }
            }
        }


        private void ClearWorkspace()
        {
            CloseDocuments();
            ClearBreakpoint();
        }

        private void CloseDocuments()
        {
	        try
	        {
				var list = new List<Document>();
	            foreach (Document dteDocument in _dte.Documents)
	            {
	                list.Add(dteDocument);
	            }
	            list.ForEach(d => d.Close());
	        }
	        catch (Exception e)
	        {
				WorkspaceLogger.Log.Error(e);		        
	        }
        }

        private void ClearBreakpoint()
        {
	        try
	        {
	            var list = new List<Breakpoint>();
	            foreach (Breakpoint debuggerBreakpoint in _dte.Debugger.Breakpoints)
	            {
	                list.Add(debuggerBreakpoint);
	            }
	            list.ForEach(b => b.Delete());
	        }
	        catch (Exception e)
	        {
		        WorkspaceLogger.Log.Error(e);
	        }
        }

    }
}