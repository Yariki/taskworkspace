using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Dropbox.Api;
using Dropbox.Api.Files;
using TaskWorkspace.Backup.Interfaces;
using TaskWorkspace.Infrastructure;
using TaskWorkspace.Model;

namespace TaskWorkspace.Backup.BackupHelpers
{
	public class DropboxHelper : IBackupHelper
	{
		private static readonly string DropboxFile = "dropbox.json";

        private const string ApiKey = "jiaz0yjc8015g6t";

        private const string LoopbackHost = "http://127.0.0.1:52475/";

        private readonly Uri RedirectUri = new Uri(LoopbackHost + "authorize");

        private readonly Uri JSRedirectUri = new Uri(LoopbackHost + "token");

        private static readonly IsolatedStorageScope Scope = IsolatedStorageScope.User
                                                             | IsolatedStorageScope.Assembly
                                                             | IsolatedStorageScope.Domain;



        private readonly string _fullFileName;
		private readonly string _filename;

		public DropboxHelper(string fullFileName, string filename)
		{
			_fullFileName = fullFileName;
			_filename = filename;
		}

		public async Task<bool> UploadBackup()
		{
			DropboxCertHelper.InitializeCertPinning();
			var accessToken = await GetToken();
            using(var client = new DropboxClient(accessToken))
            {
	            try
	            {
		            using (var mem = new MemoryStream(File.ReadAllBytes(_fullFileName)))
		            {
                        var updated = await client.Files.UploadAsync($"/{_filename}",WriteMode.Overwrite.Instance,body:mem);
                        return updated.Size != 0;
                    }
                }
	            catch (Exception e)
	            {
		            WorkspaceLogger.Log.Error(e);
	            }

	            return false;
            }
		}

		public async Task<bool> DownloadBackup()
		{
            DropboxCertHelper.InitializeCertPinning();
            var accessToken = await GetToken();
            try
            {
                using(var client = new DropboxClient(accessToken))
                {
	                try
	                {
                        var response = await client.Files.DownloadAsync($"/{_filename}");
                        if(response != null && response.Response.Size > 0)
                        {
                            using(var fileStream = File.Create(_fullFileName))
                            {
                                (await response.GetContentAsStreamAsync()).CopyTo(fileStream);
                                return true;
                            }
                        }
                    }
	                catch (Exception e)
	                {
		                WorkspaceLogger.Log.Error(e);
		                return false;
	                }
                }

            }
            catch (Exception e)
            {
	            WorkspaceLogger.Log.Error(e);
            }

            return false;
		}

		private async Task<string> GetToken()
		{
			var token = GetTokenFromFile() ?? (await GetTokenOnline());
			return token;
		}

		private async Task<string> GetTokenOnline()
		{
			var accessToken = string.Empty;
            try
            {
                var state = Guid.NewGuid().ToString("N");
                var authorizeUri =
                    DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, ApiKey, RedirectUri, state: state);
                var http = new HttpListener();
                http.Prefixes.Add(LoopbackHost);

                http.Start();

                System.Diagnostics.Process.Start(authorizeUri.ToString());

                await HandleOAuth2Redirect(http);

                var result = await HandleJSRedirect(http);

                if (result.State != state)
                {
                    return null;
                }
                accessToken = result.AccessToken;
                SaveToken(accessToken);
            }
            catch (Exception e)
            {
                WorkspaceLogger.Log.Error(e);
                return null;
            }

            return accessToken;
		}


		private string GetTokenFromFile()
		{
			using (var isolatedStorageFile = IsolatedStorageFile.GetStore(Scope,null,null))
			{
				if(!isolatedStorageFile.FileExists(DropboxFile)) 
				{
                    WorkspaceLogger.Log.Warn("There is no saved Dropbox token.");
					return null;
				}
				var stream = isolatedStorageFile.OpenFile(DropboxFile,FileMode.Open,FileAccess.Read,FileShare.Read);
				var dataContractSerializer =  new DataContractJsonSerializer(typeof(Token));
				var token = dataContractSerializer.ReadObject(stream) as Token;
				return token != null ? token.AccessToken : null;
			}
		}

		private async Task HandleOAuth2Redirect(HttpListener http)
        {
            var context = await http.GetContextAsync();

            while (context.Request.Url.AbsolutePath != RedirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            context.Response.ContentType = "text/html";
            var indexFile = Path.Combine(Path.GetDirectoryName(typeof(DropboxHelper).Assembly.Location),"index.html");

            using (var file = File.OpenRead(indexFile))
            {
                file.CopyTo(context.Response.OutputStream);
            }

            context.Response.OutputStream.Close();
        }

        private async Task<OAuth2Response> HandleJSRedirect(HttpListener http)
        {
            var context = await http.GetContextAsync();

            while (context.Request.Url.AbsolutePath != JSRedirectUri.AbsolutePath)
            {
                context = await http.GetContextAsync();
            }

            var redirectUri = new Uri(context.Request.QueryString["url_with_fragment"]);

            var result = DropboxOAuth2Helper.ParseTokenFragment(redirectUri);

            return result;
        }

        private void SaveToken ( string token)
        {
            using(var isolatedStorageFile = IsolatedStorageFile.GetStore(Scope,null,null))
            {
                if(isolatedStorageFile.FileExists(DropboxFile))
                {
                    isolatedStorageFile.DeleteFile(DropboxFile);
                }
                var stream = isolatedStorageFile.OpenFile(DropboxFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                var dataContractSerializer = new DataContractJsonSerializer(typeof(Token));
                dataContractSerializer.WriteObject(stream,new Token(){AccessToken = token});
            }
        }

	}
}