using System;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using TaskWorkspace.DataAccess;
using TaskWorkspace.EventArguments;

namespace TaskWorkspace.Services
{
    public class WorkspaceService : IDisposable
    {
        private DTE _dte;
        private readonly SolutionEventsService _eventsService;
        private bool _isSolutionOpened;


        private readonly WorkspaceRepository _repository;

        private readonly IVsSolution _solution;
        private IEnumerable<string> _workspaces;

        public WorkspaceService(IVsSolution solution, DTE dte)
        {
            _solution = solution;
            _dte = dte;


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
        }

        public void LoadWorkspace()
        {
        }

        public void DeleteWorkspace()
        {
        }

        private void SolutionOpened(object sender, OpenedSolutionArgs e)
        {
            _isSolutionOpened = true;
            _workspaces = _repository.GetWorkspaces();
            SelectedWorkspace = null;
        }

        private void SolutionClosed(object sender, EventArgs e)
        {
            _isSolutionOpened = false;
            _workspaces = new List<string>();
            SelectedWorkspace = null;
        }
    }
}