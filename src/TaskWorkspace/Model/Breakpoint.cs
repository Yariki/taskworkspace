using LiteDB;
using Break = EnvDTE.Breakpoint;


namespace TaskWorkspace.Model
{
    public class Breakpoint
    {
        [BsonId(true)]
        public int Id { get; set; }

        public string Filename { get; set; }

        public int Line { get; set; }

        public bool Enabled { get; set; }

        public static Breakpoint CreateBreakpoint(Break breakpoint)
        {
            return new Breakpoint
            {
                Filename = breakpoint.File,
                Line = breakpoint.FileLine,
                Enabled = breakpoint.Enabled
            };
        }

        public override string ToString() => $"{Filename} : {Line} : {Enabled}";
    }
}