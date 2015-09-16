using WSU.Models;

namespace WSU.Services
{
    public interface IUpdateService
    {
        UpdateInformation GetLastestUpdateInformation();
        string DownloadLastestUpdate();
        string InstallUpdate();
    }
}
