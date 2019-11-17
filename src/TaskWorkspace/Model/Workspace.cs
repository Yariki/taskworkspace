using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace TaskWorkspace.Model
{
    public class Workspace
    {
        public Workspace()
        {
            Documents = new List<Document>();
            Breakpoints = new List<Breakpoint>();
        }

        [BsonId(true)]
        public int Id { get; set; }

        public string Name { get; set; }

        public List<Document> Documents { get; set; }

        public List<Breakpoint> Breakpoints { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append($"{Name}\n");
            str.Append($"Documents:\n");
            foreach (var document in Documents)
            {
                str.Append($"{document.ToString()}");
            }
            str.Append($"Breakpoints:\n");
            foreach (var breakpoint in Breakpoints)
            {
                str.Append($"{breakpoint.ToString()}");
            }

            return str.ToString();
        }
    }
}