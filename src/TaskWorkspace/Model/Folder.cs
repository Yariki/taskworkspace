using System.Runtime.Serialization;

namespace TaskWorkspace.Model
{
	[DataContract]
	public class Folder
	{
		public Folder()
		{
			
		}

		[DataMember]
		public string Id { get; set; }

	}
}