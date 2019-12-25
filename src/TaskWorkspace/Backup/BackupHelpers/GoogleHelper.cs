using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.VisualStudio.Debugger.Clr.Cpp;
using TaskWorkspace.Backup.Interfaces;
using TaskWorkspace.Model;

namespace TaskWorkspace.Backup.BackupHelpers
{
	public class GoogleHelper : IBackupHelper
	{
		private static readonly string DefaultFolder = "TaskWorkspace";

		private static readonly string DefaultFolderInfo = "taskworkspace.json";
		private static readonly string GoogleOAuthInfo = "google.json";
		private static readonly string DefaultMimeType = "application/octet-stream";

		private static readonly IsolatedStorageScope Scope = IsolatedStorageScope.User
		                                                     | IsolatedStorageScope.Assembly
		                                                     | IsolatedStorageScope.Domain;

		private readonly string _fullFileName;
		private readonly string _filename;

		public GoogleHelper ( string fullFileName, string filename )
		{
			_fullFileName = fullFileName;
			_filename = filename;
		}

		public Task<bool> UploadBackup()
		{
			throw new System.NotImplementedException();
		}

		public Task<bool> DownloadBackup()
		{
			throw new System.NotImplementedException();
		}



		private OAuthInfo GetAuthInfo ()
		{
			var oathfile = Path.Combine(Path.GetDirectoryName(typeof(DropboxHelper).Assembly.Location), GoogleOAuthInfo);
			if(!File.Exists(oathfile))
			{
				return null;
			}
			var stream = File.Open(oathfile, FileMode.Open, FileAccess.Read, FileShare.Read);
			var dataContractSerializer = new DataContractJsonSerializer(typeof(OAuthInfo));
			var info = dataContractSerializer.ReadObject(stream) as OAuthInfo;
			return info;
		}


		private DriveService GetDriveService ( )
		{
			string[] scopes =
			{
				DriveService.Scope.DriveFile,
				DriveService.Scope.Drive,
				DriveService.Scope.DriveAppdata
			};
			
			var oauthInfo = GetAuthInfo();

			GoogleWebAuthorizationBroker.Folder = DefaultFolder;
			var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
				{
					ClientId = oauthInfo.ClientId,
					ClientSecret = oauthInfo.ClientSecret
				},
				scopes,
				"Admin",
				CancellationToken.None,
				new FileDataStore("Daimto.GoogleDrive.Auth.Store")).Result;

			var service = new DriveService(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = DefaultFolder
			});

			return service;
		}


		private string GetDefaultFolderId ( )
		{
			using(var isolatedStorageFile = IsolatedStorageFile.GetStore(Scope, null, null))
			{
				if(!isolatedStorageFile.FileExists(DefaultFolderInfo))
				{
					return null;
				}
				var stream = isolatedStorageFile.OpenFile(DefaultFolderInfo, FileMode.Open, FileAccess.Read, FileShare.Read);
				var dataContractSerializer = new DataContractJsonSerializer(typeof(Folder));
				var folderInfo = dataContractSerializer.ReadObject(stream) as Folder;
				return folderInfo?.Id;
			}
		}

		private void SaveDefaultFolderInfo ( string folderId )
		{
			using(var isolatedStorageFile = IsolatedStorageFile.GetStore(Scope, null, null))
			{
				if(isolatedStorageFile.FileExists(DefaultFolderInfo))
				{
					isolatedStorageFile.DeleteFile(DefaultFolderInfo);
				}
				var stream = isolatedStorageFile.OpenFile(DefaultFolderInfo, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				var dataContractSerializer = new DataContractJsonSerializer(typeof(Folder));
				dataContractSerializer.WriteObject(stream, new Folder() { Id = folderId });
			}
		}



	}
}