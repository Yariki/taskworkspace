using System.Collections.Generic;

namespace TaskWorkspace.Model
{
    public class Workspace
    {
        public Workspace()
        {
            Documents = new List<Document>();
            Breakpoints = new List<Breakpoint>();
        }

        public string Name { get; set; }

        public List<Document> Documents { get; set; }

        public List<Breakpoint> Breakpoints { get; set; }
    }
}