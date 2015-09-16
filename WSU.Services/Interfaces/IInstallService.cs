namespace WSU.Services.Interfaces
{
    public interface IInstallService
    {
        void Configure();
        void RegisterCertificate(string certificationFilePath);
        void InstallPackage(string appxPath);
        bool IsLaunched();
        void StopApplicationProcesses();
        void RegisterLicense();
        bool IsInstalled();
        void Uninstall();

        void BackupData(string pathToBackup, string backupDestination);

        string GetInstalledVersion(string installName);
    }
}
