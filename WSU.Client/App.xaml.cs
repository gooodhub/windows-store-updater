using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;
using log4net.Config;
using WSU.Services;

namespace WSU.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ILog _logger;

        public App()
        {
            XmlConfigurator.Configure();
            _logger = LogManager.GetLogger("LogFileAppender");

            _logger.Debug("Paramètres : " + string.Join(" ", Environment.GetCommandLineArgs()));

            // Check if user is NOT admin
            if (!IsRunningAsAdministrator())
            {
                _logger.Debug("Application non lancée entant qu'admin ...");
                // Setting up start info of the new process of the same application
                ProcessStartInfo processStartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().CodeBase);

                // Using operating shell and setting the ProcessStartInfo.Verb to “runas” will let it run as admin
                processStartInfo.UseShellExecute = true;
                processStartInfo.Verb = "runas";
                processStartInfo.Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1)); // pass args

                // Start the application as new process
                Process.Start(processStartInfo);

                // Shut down the current (old) process
                //System.Windows.Forms.Application.Exit();
                Environment.Exit(0); // Changement en Environment.Exit(0) parce qu'avec l'autre méthode, il me restait un process
            }

            Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            string startup = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string uriName = ConfigurationManager.AppSettings["uri:name"];
            string uriDescription = ConfigurationManager.AppSettings["uri:description"];
            string clickOncePublisher = ConfigurationManager.AppSettings["clickonce:publisher"];
            string clickOncePackageName = ConfigurationManager.AppSettings["clickonce:packageName"];
            string clickOnceFileName = ConfigurationManager.AppSettings["clickonce:fileName"];
            string appPath = Path.Combine(startup, clickOncePublisher, clickOncePackageName, clickOnceFileName);
            //if (!UriHelper.IsUriRegistered(uriName, appPath))
            //{
                //_logger.Info("Installation de l'URI...");
                //UriHelper.RegisterUri(uriName, appPath, uriDescription);
            //}

            _logger.Info("Lancement de WSU");
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            _logger.Error("Exception on TaskSchedulerOnUnobservedTaskException", args.Exception);
            MessageBox.Show("Une erreur est survenue. Veuillez réessayer.", "WSU - Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            _logger.Error("Exception on CurrentOnDispatcherUnhandledException", args.Exception);
            MessageBox.Show("Une erreur est survenue. Veuillez réessayer.", "WSU - Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger.Info("Fermeture de WSU");
            base.OnExit(e);
        }

        /// <summary>
        /// Function that check's if current user is in Aministrator role
        /// </summary>
        /// <returns></returns>
        public static bool IsRunningAsAdministrator()
        {
#if DEBUG
            return true;
#endif
            // Get current Windows user
            WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();

            // Get current Windows user principal
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(windowsIdentity);

            // Return TRUE if user is in role "Administrator"
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
