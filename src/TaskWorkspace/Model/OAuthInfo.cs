using System.Runtime.Serialization;

namespace TaskWorkspace.Model
{
	[DataContract]
	public class OAuthInfo
	{
		public OAuthInfo()
		{
			
		}

		[DataMember]
		public string ClientId { get; set; }

		[DataMember]
		public string ClientSecret { get; set; }


	}
}