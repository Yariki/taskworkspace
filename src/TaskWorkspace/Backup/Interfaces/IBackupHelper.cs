using System.Threading.Tasks;

namespace TaskWorkspace.Backup.Interfaces
{
	public interface IBackupHelper
	{
		Task<bool> UploadBackup();

		Task<bool> DownloadBackup();

	}
}