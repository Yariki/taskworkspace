using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

		public void Error(Exception e)
		{
			var mes = new StringBuilder();
			ApplyMessage(e,mes);
			var inner = e.InnerException;
			while(inner != null)
			{
				ApplyMessage(inner,mes);
				inner = inner.InnerException;
			}
			if(e is AggregateException aggregate)
			{
				foreach (var aggregateInnerException in aggregate.InnerExceptions)
				{
					ApplyMessage(aggregateInnerException,mes);
				}
			}
			Logger?.Error(mes);
		}

		private void ApplyMessage(Exception inner, StringBuilder mes)
		{
			mes.AppendLine($"Type: {inner.GetType().FullName}");
			mes.AppendLine($"\tMessage: {inner.Message}");
		}


		public void Info(string message)
        {
            Logger?.Info(message);
        }

        public void Info(string message,IEnumerable<string> values)
        {
            Logger?.Info(message);
            foreach (var value in values)
            {
                Logger?.Info(value);
            }
        }

        public void Warn(string message)
        {
            Logger?.Warn(message);
        }
    }
}