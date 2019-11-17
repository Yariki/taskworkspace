using System.Text;
using LiteDB;

namespace TaskWorkspace.Model
{
    public class Document
    {
        [BsonId(true)]
        public int Id { get; set; }

        public string Filename { get; set; }

        public override string ToString() => Filename;
       
    }
}