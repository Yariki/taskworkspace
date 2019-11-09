using System;

namespace TaskWorkspace.EventArguments
{
    public class OpenedSolutionArgs : EventArgs
    {
        public OpenedSolutionArgs(uint newSolutionCookie)
        {
            NewSolutionCookie = newSolutionCookie;
        }

        public uint NewSolutionCookie { get; set; }

    }
}