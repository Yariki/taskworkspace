using System;
using System.Threading.Tasks;

namespace TaskWorkspace.Helpers
{
	internal static class AsyncExtensions
	{

		public static string ReadLine(this LinesReader reader, int line)
		{
			return RunSync(() => reader.ReadLineAsync(line));
		}


		private static T RunSync<T>(Func<Task<T>> doFunc)
		{
			return Task.Run(doFunc).Result;
		}

	}
}