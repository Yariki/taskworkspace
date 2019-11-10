using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TaskWorkspace.DataAccess;
using TaskWorkspace.EventArguments;
using TaskWorkspace.Infrastructure;
using WorkspaceDocument = TaskWorkspace.Model.Document;
using WorkspaceBreakpoint = TaskWorkspace.Model.Breakpoint;

namespace TaskWorkspace.Services
{
    public class WorkspaceService : IDisposable
    {
        private readonly DTE _dte;
        private readonly SolutionEventsService _eventsService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isSolutionOpened;


        private readonly WorkspaceRepository _repository;

        private readonly IVsSolution _solution;
        private IEnumerable<string> _workspaces;

        public WorkspaceService(IServiceProvider serviceProvider, IVsSolution solution, DTE dte)
        {
            _solution = solution;
            _dte = dte;
            _serviceProvider = serviceProvider;

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
        }

        private (List<WorkspaceDocument>, List<WorkspaceBreakpoint>) GetWorkspaceItem()
        {
            var documents = new List<WorkspaceDocument>();
            foreach (Document dteDocument in _dte.Documents)
            {
                documents.Add(new WorkspaceDocument() {Filename = dteDocument.FullName});
            }

            var breakpoints = new List<WorkspaceBreakpoint>();
            foreach (Breakpoint breakpoint in _dte.Debugger.Breakpoints)
            {
                breakpoints.Add(new WorkspaceBreakpoint()
                {
                    Filename = breakpoint.File,
                    Line = breakpoint.FileLine
                });
            }

            return (documents, breakpoints);
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
            OpenDocuments(workspace.Documents);
            AddBreakpoints(workspace.Breakpoints);
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
        }

        private void SolutionOpened(object sender, OpenedSolutionArgs e)
        {
            _isSolutionOpened = true;
            var workspaces = _repository.GetWorkspaces();
            _workspaces = workspaces.Any() ? workspaces :   new List<string>();
        }

        private void SolutionClosed(object sender, EventArgs e)
        {
            _isSolutionOpened = false;
            _workspaces = new List<string>();
            SelectedWorkspace = null;
        }

        private void OpenDocuments(List<WorkspaceDocument> documents)
        {
            documents.ForEach(d => _dte.Documents.Open(d.Filename));
        }

        private void AddBreakpoints(List<WorkspaceBreakpoint> breakpoints)
        {
            breakpoints.ForEach(b => _dte.Debugger.Breakpoints.Add(File: b.Filename, Line: b.Line));
        }


        private void ClearWorkspace()
        {
            CloseDocuments();
            ClearBreakpoint();
        }

        private void CloseDocuments()
        {
            var list = new List<Document>();
            foreach (Document dteDocument in _dte.Documents)
            {
                list.Add(dteDocument);
            }
            list.ForEach(d => d.Close());
        }

        private void ClearBreakpoint()
        {
            var list = new List<Breakpoint>();
            foreach (Breakpoint debuggerBreakpoint in _dte.Debugger.Breakpoints)
            {
                list.Add(debuggerBreakpoint);
            }
            list.ForEach(b => b.Delete());
        }

    }
}