using System.Collections.Generic;
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
    }
}