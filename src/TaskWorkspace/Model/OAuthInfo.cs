using System.Runtime.Serialization;

namespace TaskWorkspace.Model
{
	[DataContract]
	public class Info
	{
		[DataMember(Name = "installed")]
		public OAuthInfo Installed { get; set; }
	}



	[DataContract]
	public class OAuthInfo
	{
		public OAuthInfo()
		{
			
		}

		[DataMember(Name = "client_id")]
		public string ClientId { get; set; }

		
		[DataMember(Name = "client_secret")]
		public string ClientSecret { get; set; }


	}
}