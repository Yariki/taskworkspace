using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace TaskWorkspace.Helpers
{
	public class BreakpointHelper : IDisposable
	{
		private readonly IDictionary<string, LinesReader> _sourceCache = new Dictionary<string, LinesReader>();

		public void Dispose()
		{
			Clear();
		}

		public  bool CanBreakpointBeSet(string filename, int line)
		{
			if (string.IsNullOrEmpty(filename) || line <= 0) return false;
			var strLine = GetLine(filename, line);
			strLine = strLine.Trim();
			if (string.IsNullOrEmpty(strLine)) return false;
			if (strLine.Length == 1 && (strLine == "{" || strLine == "}")) return true;

			var syntaxTree = CSharpSyntaxTree.ParseText(strLine);
			var childs = syntaxTree.GetRoot().ChildNodes();
			if (childs == null)
				return false;
			if (!childs.Any() && syntaxTree.GetRoot().Kind() == SyntaxKind.CompilationUnit)
				return true;
			var child = childs.First();
			return !(child.Kind() == SyntaxKind.ClassDeclaration || child.Kind() == SyntaxKind.MethodDeclaration);
		}

		private string GetLine(string filename, int line)
		{
			var lineReader = _sourceCache.ContainsKey(filename) ? _sourceCache[filename] : GetLines(filename);

			return lineReader.ReadLine(line);
		}

		private LinesReader GetLines(string filename)
		{
			if (!File.Exists(filename)) throw new FileNotFoundException(filename);
			_sourceCache.Add(filename, new LinesReader(filename));
			return _sourceCache[filename];
		}

		public void Clear()
		{
			foreach (var keyValuePair in _sourceCache)
			{
				keyValuePair.Value.Dispose();
			}
			_sourceCache.Clear();
		}
	}
}