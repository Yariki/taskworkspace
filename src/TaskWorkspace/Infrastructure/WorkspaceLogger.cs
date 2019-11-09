using System;
using NLog;

namespace TaskWorkspace.Infrastructure
{
    public class WorkspaceLogger
    {
        private static readonly Lazy<WorkspaceLogger> _logger = new Lazy<WorkspaceLogger>(() => new WorkspaceLogger());

        private WorkspaceLogger()
        {
        }

        private static Logger Logger => LogManager.GetCurrentClassLogger();

        public static WorkspaceLogger Log => _logger.Value;


        public void Debug(string message)
        {
            Logger?.Debug(message);
        }

        public void Error(string message)
        {
            Logger?.Error(message);
        }


        public void Info(string message)
        {
            Logger?.Info(message);
        }

        public void Warn(string message)
        {
            Logger?.Warn(message);
        }
    }
}