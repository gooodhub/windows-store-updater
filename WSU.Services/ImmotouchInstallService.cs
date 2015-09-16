using System.Configuration;

namespace WSU.Services
{
    public class ImmotouchInstallService : PsInstallService
    {
        // TODO ça nous force à avoir un genre de dépendence sur Immotouch (ou au moins à des noms contenant Immotouch) alors qu'on veut que ce soit générique
        public ImmotouchInstallService(string psScriptsFolderPath)
            : base(psScriptsFolderPath, ConfigurationManager.AppSettings["immotouch:process"], ConfigurationManager.AppSettings["immotouch:package"])
        {
        }
    }
}
