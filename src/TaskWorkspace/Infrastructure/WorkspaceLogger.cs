using System;
using System.IO;
using Microsoft.VisualStudio.Debugger.Clr.Cpp;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace TaskWorkspace.Infrastructure
{
    public class WorkspaceLogger
    {
        private static readonly Lazy<WorkspaceLogger> _logger = new Lazy<WorkspaceLogger>(() => new WorkspaceLogger());
        private WorkspaceLogger()
        {

            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget()
            {
                Name = "File",
                FileName = "${specialfolder:folder=LocalApplicationData}/TaskWorkspace/logs/${shortdate}.log",
                Layout = "${longdate} ${logger} ${uppercase:${level}} ${message}${exception:format=ToString}",
            };
            config.AddTarget(fileTarget);
            config.AddRuleForAllLevels(fileTarget);
            LogManager.Configuration = config;
            Logger = LogManager.GetLogger("TaskWorkspace");

        }

        private static Logger Logger;

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