using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace TaskWorkspace.Helpers
{
	public class BreakpointHelper : IDisposable
	{
		private readonly IDictionary<string, List<string>> _sourceCache = new Dictionary<string, List<string>>();


		public void Dispose()
		{
			Clear();
		}

		public bool CanBreakpointBeSet(string filename, int line)
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
			var lines = _sourceCache.ContainsKey(filename) ? _sourceCache[filename] : GetLines(filename);
			if (lines == null || line > lines.Count) return string.Empty;
			return lines[line - 1];
		}

		private List<string> GetLines(string filename)
		{
			if (!File.Exists(filename)) return null;
			_sourceCache.Add(filename, new List<string>(File.ReadAllLines(filename)));
			return _sourceCache[filename];
		}

		public void Clear()
		{
			_sourceCache.Clear();
		}
	}
}