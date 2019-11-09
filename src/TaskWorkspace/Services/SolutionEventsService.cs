using System;
using Microsoft.VisualStudio.Shell.Interop;
using TaskWorkspace.EventArguments;

namespace TaskWorkspace.Services
{
    public class SolutionEventsService : IDisposable, IVsSolutionEvents
    {
        private bool _disposed;
        private readonly IVsSolution _solution;
        private uint _solutionCookie;

        public EventHandler SolutionClosed;

        public EventHandler<OpenedSolutionArgs> SolutionOpened;

        public SolutionEventsService(IVsSolution solution)
        {
            _solution = solution;
            _solution.AdviseSolutionEvents(this, out _solutionCookie);
        }

        public void Dispose()
        {
            if (_disposed || _solutionCookie == 0) return;

            _disposed = true;
            _solution.UnadviseSolutionEvents(_solutionCookie);
            _solutionCookie = 0;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return 0;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return 0;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return 0;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return 0;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            SolutionOpened?.Invoke(this, new OpenedSolutionArgs((uint) fNewSolution));
            return 0;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return 0;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return 0;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            SolutionClosed?.Invoke(this, new EventArgs());
            return 0;
        }
    }
}