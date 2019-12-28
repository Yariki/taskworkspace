using System;
using System.Threading.Tasks;
using TaskWorkspace.Backup.BackupHelpers;
using TaskWorkspace.Backup.Interfaces;

namespace TaskWorkspace.Backup
{
	public class BackupHelpersFactory
	{
		private static Lazy<BackupHelpersFactory> _instance = new Lazy<BackupHelpersFactory>(() => new BackupHelpersFactory());

		public static BackupHelpersFactory Instance => _instance.Value;


		public IBackupHelper GetBackupHelper ( StorageType type, string fullFileName,string filename )
		{
			switch (type)
			{
				case StorageType.Dropbox:
					return new DropboxHelper(fullFileName,filename);
				case StorageType.Google:
					return new GoogleHelper(fullFileName, filename);
			}
			return null;
		}

	}
}