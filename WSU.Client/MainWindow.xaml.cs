using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ionic.Zip;
using log4net;
using WSU.Client.ViewModel;
using WSU.Models;
using WSU.Services;

namespace WSU.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _appxPath;
        private Exception _exception;
        private readonly ILog _logger;
        private string _certifPath;

        protected static readonly string Url = ConfigurationManager.AppSettings["api:url"];
        protected static readonly string CdmId = ConfigurationManager.AppSettings["api:id"];
        protected static readonly string ApiKey = ConfigurationManager.AppSettings["api:key"];
        protected static readonly string PackageName = ConfigurationManager.AppSettings["windowsApp:package"];

        private readonly MainWindowViewModel _vm;
        private HmacAuthWebApiClient _client;

        public MainWindow()
        {
            _vm = new MainWindowViewModel();
            DataContext = _vm;

            _logger = LogManager.GetLogger("LogFileAppender");
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            await CheckNewVersion();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "update" && !_vm.VersionOk.GetValueOrDefault(false))
            {
                InstallButton_OnClick(sender, routedEventArgs);
            }
        }

        private async Task CheckNewVersion()
        {
            string scripts = Path.Combine(Environment.CurrentDirectory, "Scripts");
            InstallService service = new InstallService(scripts);

            _vm.CurrentVersion = service.GetInstalledVersion(PackageName);
            _client = new HmacAuthWebApiClient(Url, CdmId, ApiKey);

            try
            {
                UpdateInformation apiVersion = await _client.ReadAsync<UpdateInformation>(!string.IsNullOrWhiteSpace(_vm.CurrentVersion) ? ConfigurationManager.AppSettings["api:downloadUrl"] + _vm.CurrentVersion + ConfigurationManager.AppSettings["api:downloadUrlMethod"] : ConfigurationManager.AppSettings["api:downloadUrlLatestUpdate"]);
                if (apiVersion == null || (!string.IsNullOrWhiteSpace(_vm.CurrentVersion) && new Version(apiVersion.Version) < new Version(_vm.CurrentVersion))) // when current version is disabled or unknown => the provided new version is lower than current one ...
                    _vm.ApiVersion = new UpdateInformation { Version = _vm.CurrentVersion }; // ... do like there is no new version => avoid impossible downgrade
                else
                    _vm.ApiVersion = apiVersion;
            }
            catch (Exception ex)
            {
                _logger.Error("CheckNewVersion() failed on ", ex);
            }
        }

        private async void InstallButton_OnClick(object sender, RoutedEventArgs e)
        {
            var progressHandler = new Progress<int>(v =>
            {
                Bar.Value = v;
                StatusTextBlock.Text = GetTextFromPercentage(v);
            });

            var progress = (IProgress<int>)progressHandler;

            await Task.Run(async () =>
            {
                await DoInstall(progress);
            });

            await CheckNewVersion();
        }

        private string GetTextFromPercentage(int i)
        {
            return i == 100 ? "Installation terminée" : i < 0 ? string.Empty : "Installation en cours...";
        }

        private async Task DoInstall(IProgress<int> progress)
        {
            _logger.Info("Lancement de l'installation");

            string saveFolder = ConfigurationManager.AppSettings["windowsApp:downloadFolder"];
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string savePath = Path.Combine(myDocuments, saveFolder);
            string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            try
            {
                progress.Report(0);
                string applicationDataFolder = Path.Combine(userProfileFolder,
                    ConfigurationManager.AppSettings["windowsApp:pathToBackup"]);
                string applicationDataZip = Path.Combine(myDocuments,
                    ConfigurationManager.AppSettings["windowsApp:backupDestination"]);

                progress.Report(10);

                string folder = Path.GetDirectoryName(applicationDataZip);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                if (Directory.Exists(savePath))
                    Directory.Delete(savePath, true);

                Directory.CreateDirectory(savePath);

                string scripts = Path.Combine(Environment.CurrentDirectory, "Scripts");
                InstallService service = new InstallService(scripts);

                progress.Report(20);

                if (service.IsLaunched())
                {
                    MessageBoxResult result = MessageBox.Show(
                        string.Format(
                            @"L'application {0} est en cours d'exécution. Voulez-vous continuer ?
En cliquant sur Oui, {0} sera fermé automatiquement, assurez-vous préalablement que votre travail ait été sauvegardé.",
                            service.PackageName),
                        "Exécution en cours", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                    if (result == MessageBoxResult.No)
                    {
                        progress.Report(-1);
                        return;
                    }

                    service.StopApplicationProcesses();
                }

                progress.Report(30);

                if (Directory.Exists(applicationDataFolder))
                    service.BackupData(applicationDataFolder, applicationDataZip);

                bool hasValidLicence = false;

                progress.Report(40);

                try
                {
                    hasValidLicence = service.HasLicence();
                }
                catch (Exception ex)
                {
                    _logger.Warn("HasLicence() failed on next error", ex);
                }

                progress.Report(50);
                if (!hasValidLicence)
                    service.RegisterLicense();

                progress.Report(60);

                string downloadedFile = await service.DownloadASync(_client, _vm.ApiVersion.Version, savePath);

                progress.Report(70);

                service.Configure();

                progress.Report(80);

                if (downloadedFile != null)
                    UnzipAndSetFilesPath(downloadedFile);

                progress.Report(85);

                service.RegisterCertificate(_certifPath);

                progress.Report(90);

                service.InstallPackage(_appxPath);

                progress.Report(100);

                MessageBox.Show("Installation Terminée", "Installation", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _exception = ex;
                _logger.Error(ex);

                MessageBox.Show(@"Une erreur s'est produite lors de l'installation." +
                "Consultez le log d'erreurs pour plus d'informations. " + _exception.Message + "",
                _exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                //Directory.Delete(savePath, true);

                if (_exception != null)
                {
                    StatusTextBlock.Text = "Problème ...";
                    Bar.Value = 0;
                    InstallButton.IsEnabled = true;

                    _logger.Error("Erreur lors de l'installation", _exception);

                    MessageBox.Show(
    @"Une erreur s'est produite lors de l'installation.
Consultez le log d'erreurs pour plus d'informations. " + _exception.Message + "", _exception.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                _logger.Info("Fin de l'installation");
            }
        }

        private void UnzipAndSetFilesPath(string downloadedFile)
        {
            string folder = Path.GetDirectoryName(downloadedFile);

            ZipFile file = ZipFile.Read(downloadedFile);

            file.ExtractAll(folder);

            _certifPath = GetCertificationFile(folder);
            _appxPath = GetAppxFile(folder);
        }

        private string GetAppxFile(string folder)
        {
            return FindFileType(folder, "*.appxbundle");
        }

        private static string GetCertificationFile(string folder)
        {
            return FindFileType(folder, "*.cer");
        }

        private static string FindFileType(string folder, string fileType)
        {
            var cers = Directory.GetFiles(folder, fileType);
            if (cers.Any())
                return cers.FirstOrDefault();

            string[] folders = Directory.GetDirectories(folder);
            if (!folders.Any()) return null;

            return folders.Select(fold => FindFileType(fold, fileType)).FirstOrDefault(fileName => fileName != null);
        }

        private async void CheckNewVersion_OnClick(object sender, RoutedEventArgs e)
        {
            string message;
            _vm.ApiVersion = null;
            await CheckNewVersion();

            if (_vm.VersionOk != null && _vm.VersionOk.Value)
                message = "Aucune mise à jour nécessaire";
            else
                message = _vm.CurrentVersion == null ? "Une installation est disponible" : "Une nouvelle mise à jour est disponible";

            MessageBox.Show(message, "Recherche de mises à jour", MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }
    }
}
