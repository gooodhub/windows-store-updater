using WSU.Models;

namespace WSU.Services.Interfaces
{
    public interface IUpdateService
    {
        UpdateInformation GetLastestUpdateInformation();
        string DownloadLastestUpdate();
        string InstallUpdate();
    }
}
