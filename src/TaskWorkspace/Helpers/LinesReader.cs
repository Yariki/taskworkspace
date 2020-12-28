using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskWorkspace.Helpers
{
	internal sealed class LinesReader : IDisposable
	{
		private readonly string _filename;
		private readonly FileStream _fileStream;

		private IDictionary<int, Offsets> _lines = new Dictionary<int,Offsets>();

		struct Offsets
		{
			public Offsets ( int begin,int end )
			{
				Begin = begin;
				End = end;
			}

			public int Begin
			{
				get; private set;
			}

			public int End
			{
				get; private set;
			}

			public int Size => End - Begin;
		}

		public LinesReader(string filename)
		{
			_filename = filename;
			_fileStream = System.IO.File.OpenRead(_filename);
			Init();
		}

		public int Count => _lines.Max(p => p.Key);

		public async Task<string> ReadLineAsync ( int line )
		{
			if(line <= 0 || line > _lines.Max(p => p.Key))
			{
				throw new IndexOutOfRangeException(nameof(line));
			}

			var offsets = _lines[line];
			var buffer = new byte[offsets.Size];
			_fileStream.Seek(offsets.Begin, SeekOrigin.Begin);
			await _fileStream.ReadAsync(buffer, 0, buffer.Length);
			return Encoding.Default.GetString(buffer);
		}

		public void Dispose()
		{
			_fileStream?.Close();
			_lines.Clear();
		}

		private void Init()
		{
			var buffer = new byte[4096];
			var readed = -1;
			var offset = -1;
			var cr = (char) '\r';
			var lf = (char) '\n';
			var line = 0;
			char prevChar = (char) 0;
			var prevOffset = 0;

			while ((readed = _fileStream.Read(buffer, 0, 4096)) > 0)
			{
				for (int i = 0; i < readed - 1; i++)
				{
					offset++;
					var current = (char) buffer[i];

					if (current == cr || current == lf)
					{
						if (prevChar == cr && current == lf)
							continue;

						line++;
						_lines.Add(line, new Offsets(prevOffset, offset));
						prevOffset = offset + 1;
					}

					prevChar = current;
				}
			}
		}
	}
}