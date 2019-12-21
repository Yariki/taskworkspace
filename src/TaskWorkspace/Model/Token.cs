using System.Runtime.Serialization;

namespace TaskWorkspace.Model
{
	[DataContract]
	public class Token
	{

		public Token()
		{	
		}

		[DataMember]
		public string AccessToken { get; set; }
	}
}