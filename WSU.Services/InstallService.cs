using System.Configuration;

namespace WSU.Services
{
    public class InstallService : PsInstallService
    {
        public InstallService(string psScriptsFolderPath)
            : base(psScriptsFolderPath, ConfigurationManager.AppSettings["windowsApp:process"], ConfigurationManager.AppSettings["windowsApp:package"])
        {
        }
    }
}
