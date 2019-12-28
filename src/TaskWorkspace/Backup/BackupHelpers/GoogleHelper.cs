using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.VisualStudio.Debugger.Clr.Cpp;
using TaskWorkspace.Backup.Interfaces;
using TaskWorkspace.Infrastructure;
using TaskWorkspace.Model;
using File = Google.Apis.Drive.v3.Data.File;

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

		public async Task<bool> UploadBackup()
		{
			try
			{
				var service = GetDriveService();
				var parentFolderId = GetDefaultFolderId() ?? CreateOrGetDefaultFolderId(service);

				//remove
				var removeList = RetrieveAllFiles(service,$"name = '{_filename}'");
				foreach(var fileRemove in removeList)
				{
					await service.Files.Delete(fileRemove.Id).ExecuteAsync();
				}

				// upload
				var fileMetadata = new File()
				{
					Name = _filename,
					Parents = new List<string>() { parentFolderId }
				};
				FilesResource.CreateMediaUpload request;
				using(var stream = new System.IO.FileStream(_fullFileName,
										System.IO.FileMode.Open))
				{
					request = service.Files.Create(
						fileMetadata,stream,DefaultMimeType);
					request.Fields = "id";

					await request.UploadAsync();
				}

				return true;
			}
			catch (Exception e)
			{
				WorkspaceLogger.Log.Error(e);
				return false;
			}
		}

		public async Task<bool> DownloadBackup()
		{
			try
			{
				var service = GetDriveService();
				var fileList = RetrieveAllFiles(service,$"name = '{_filename}'");
				if(!fileList.Any())
				{
					WorkspaceLogger.Log.Warn($"The '{_filename}' backup was not found.");
					return false;
				}

				var fileResp = service.Files.Get(fileList.First().Id);
				//download
				var memStream = new MemoryStream();
				fileResp.MediaDownloader.ProgressChanged += progress =>
				{
					switch(progress.Status)
					{
						case DownloadStatus.Completed:
							using(var fileStream = System.IO.File.Create(_fullFileName))
							{
								memStream.WriteTo(fileStream);
								fileStream.Flush();
							}
							break;
						case DownloadStatus.Failed:
							if(progress.Exception != null)
								Console.WriteLine(progress.Exception.Message);
							break;

					}
				};
				await fileResp.DownloadAsync(memStream);

				return true;
			}
			catch (Exception e)
			{
				WorkspaceLogger.Log.Error(e);
				return false;
			}
		}



		private Info GetAuthInfo ()
		{
			var oathfile = Path.Combine(Path.GetDirectoryName(typeof(DropboxHelper).Assembly.Location), GoogleOAuthInfo);
			if(!System.IO.File.Exists(oathfile))
			{
				return null;
			}
			var stream = System.IO.File.Open(oathfile, FileMode.Open, FileAccess.Read, FileShare.Read);
			var dataContractSerializer = new DataContractJsonSerializer(typeof(Info));
			var info = dataContractSerializer.ReadObject(stream) as Info;
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
					ClientId = oauthInfo.Installed.ClientId,
					ClientSecret = oauthInfo.Installed.ClientSecret
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
				using (var stream = isolatedStorageFile.OpenFile(DefaultFolderInfo,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
				{
					var dataContractSerializer = new DataContractJsonSerializer(typeof(Folder));
					dataContractSerializer.WriteObject(stream,new Folder() { Id = folderId });
				}
			}
		}

		private string CreateOrGetDefaultFolderId (DriveService service)
		{
			var filelist = RetrieveAllFiles(service,$"mimeType = 'application/vnd.google-apps.folder' and name = '{DefaultFolder}'");
			string folderId = filelist.Any() ? filelist.First().Id : CreateFolderAndReturnId(service, DefaultFolder);

			SaveDefaultFolderInfo(folderId);

			return folderId;
		}


		private static string CreateFolderAndReturnId ( DriveService service,string name )
		{
			var fileMetadata = new File();
			fileMetadata.Name = "TaskWorkspace";
			fileMetadata.MimeType = "application/vnd.google-apps.folder";

			var file = service.Files.Create(fileMetadata).Execute();
			return file.Id;
		}

		public static List<File> RetrieveAllFiles ( DriveService service,string searchQuery )
		{
			var result = new List<File>();
			var request = service.Files.List();
			//request.Fields = "id, webContentLink";
			request.Q = searchQuery;

			do
			{
				try
				{
					var files = request.Execute();

					result.AddRange(files.Files);
					request.PageToken = files.NextPageToken;
				}
				catch(Exception e)
				{
					WorkspaceLogger.Log.Error(e);
					request.PageToken = null;
				}
			} while(!string.IsNullOrEmpty(request.PageToken));

			return result;
		}

		private void UploadFile ( DriveService service,string fullFilename, string filename,string parentFolderId )
		{
			var removeLIst = RetrieveAllFiles(service,$"name = '{filename}'");
			foreach(var fileRemove in removeLIst)
			{
				service.Files.Delete(fileRemove.Id).Execute();
			}

			// upload
			var fileMetadata = new File()
			{
				Name = filename,
				Parents = new List<string>() { parentFolderId }
			};
			FilesResource.CreateMediaUpload request;
			using(var stream = new System.IO.FileStream(fullFilename,
									System.IO.FileMode.Open))
			{
				request = service.Files.Create(
					fileMetadata,stream,DefaultMimeType);
				request.Upload();
			}
		}

	}
}