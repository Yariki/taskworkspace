using System;
using System.Threading.Tasks;
using Microsoft.Build.Tasks;
using TaskWorkspace.Backup.Interfaces;

namespace TaskWorkspace.Backup
{
	public sealed class BackupWorkspace
	{
		private StorageType _storageType;
		private string _fullFileName;
		private string _filename;

		public BackupWorkspace(StorageType storageType, string fullFileName, string filename)
		{
			_storageType = storageType;
			this._fullFileName = fullFileName;
			this._filename = filename;
		}

		public async Task<bool> Backup()
		{
			var helper = BackupHelpersFactory.Instance.GetBackupHelper(_storageType, _fullFileName, _filename);
			return await helper?.UploadBackup();
		}


		public async Task<bool> Restore()
		{
			var helper = BackupHelpersFactory.Instance.GetBackupHelper(_storageType,_fullFileName,_filename);
			return await  helper?.DownloadBackup();
		}

	}
}