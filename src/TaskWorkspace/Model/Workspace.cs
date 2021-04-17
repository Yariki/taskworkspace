using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace TaskWorkspace.Model
{
    public class Workspace
    {
        public Workspace()
        {
            Breakpoints = new List<Breakpoint>();
        }

        [BsonId(true)]
        public int Id { get; set; }

        public string Name { get; set; }

        public List<Breakpoint> Breakpoints { get; set; }

        public string WindowsBase64 {get;set;}

        public string WindowsFull { get; set; }
        
        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append($"{Name}\n");
            str.Append($"Breakpoints:\n");
            foreach (var breakpoint in Breakpoints)
            {
                str.Append($"{breakpoint.ToString()}");
            }
            str.Append($"Windows: {WindowsBase64}");

            return str.ToString();
        }
    }
}