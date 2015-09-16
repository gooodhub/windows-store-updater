using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CLT.Utils.Powershell;
using Ionic.Zip;
using WSU.Services.Interfaces;

namespace WSU.Services
{
    public class PsInstallService : IInstallService
    {
        public string ProcessName { get; private set; }
        public string PackageName { get; private set; }
        private IDictionary<string, string> PsScriptsFolderPath { get; set; }

        public PsInstallService(string psScriptsFolderPath, string processName, string packageName)
        {
            if (string.IsNullOrWhiteSpace(psScriptsFolderPath))
                throw new ArgumentNullException("psScriptsFolderPath");
            if (!Directory.Exists(psScriptsFolderPath))
                throw new DirectoryNotFoundException("Impossible d'accéder au dossier de script avec le chemin : '" + psScriptsFolderPath + "'.");

            if (string.IsNullOrWhiteSpace(processName))
                throw new ArgumentNullException("processName");
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentNullException("packageName");

            ProcessName = processName;
            PackageName = packageName;
            PsScriptsFolderPath = generateScriptDictionary(psScriptsFolderPath);
        }

        private IDictionary<string, string> generateScriptDictionary(string psScriptsFolderPath)
        {
            if (string.IsNullOrWhiteSpace(psScriptsFolderPath))
                throw new ArgumentNullException("psScriptsFolderPath");
            if (!Directory.Exists(psScriptsFolderPath))
                throw new DirectoryNotFoundException("Impossible d'accéder au dossier de script avec le chemin : '" + psScriptsFolderPath + "'.");

            string[] files = Directory.GetFiles(psScriptsFolderPath, "*.ps1");
            if (files == null || !files.Any())
                throw new InvalidOperationException("Le dossier '" + psScriptsFolderPath + "' ne contient aucun script.");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string[] scriptName = { "RegisterCertificate", "Configure", "Uninstall", "Install", "RegisterLicense", "CheckInstall", "CheckLicence" };
            foreach (string script in scriptName)
            {
                string filePath = Path.Combine(psScriptsFolderPath, script + ".ps1");
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Le script '" + script + "' n'est pas accessible ou n'est pas présent dans le dossier '" + psScriptsFolderPath + "'.");

                dictionary.Add(script, filePath);
            }
            return dictionary;
        }

        public void Configure()
        {
            PowershellService.ExecuteScript(PsScriptsFolderPath["Configure"]);
        }

        public void RegisterCertificate(string certificationFilePath)
        {
            if (string.IsNullOrWhiteSpace(certificationFilePath))
                throw new ArgumentNullException("certificationFilePath");
            if (!File.Exists(certificationFilePath))
                throw new FileNotFoundException("Impossible d'accéder au certificat avec le chemin : '" + certificationFilePath + "'.");

            PowershellService.ExecuteScript(PsScriptsFolderPath["RegisterCertificate"], new KeyValuePair<string, object>("certPath", certificationFilePath));
        }

        public void InstallPackage(string appxPath)
        {
            if (string.IsNullOrWhiteSpace(appxPath))
                throw new ArgumentNullException("appxPath");
            if (!File.Exists(appxPath))
                throw new FileNotFoundException("Impossible d'accéder au package avec le chemin : '" + appxPath + "'.");

            PowershellService.ExecuteScript(PsScriptsFolderPath["Install"], new KeyValuePair<string, object>("packagePath", appxPath));
        }
        
        public bool IsLaunched()
        {
            return GetProcessesByName().Any();
        }

        public void StopApplicationProcesses()
        {
            foreach (Process process in GetProcessesByName())
            {
                process.Kill();
            }
        }

        private IEnumerable<Process> GetProcessesByName()
        {
            return Process.GetProcessesByName(ProcessName);
        }

        public void RegisterLicense()
        {
            PowershellService.ExecuteScript(PsScriptsFolderPath["RegisterLicense"]);
        }

        public bool HasLicence()
        {
            IEnumerable<PSObject> resultats = 
                PowershellService
                    .ExecuteScript(PsScriptsFolderPath["CheckLicence"]);

            return resultats
                .Any(r => bool.Parse(r.ToString()));
        }

        public bool IsInstalled()
        {
            Collection<PSObject> psObjects = PowershellService.ExecuteScript(PsScriptsFolderPath["CheckInstall"], new KeyValuePair<string, object>("packageName", PackageName));
            return psObjects.Any();
        }

        public void Uninstall()
        {
            PowershellService.ExecuteScript(PsScriptsFolderPath["Uninstall"], 
                new KeyValuePair<string, object>("packageName", PackageName));
        }

        public void BackupData(string pathToBackup, string backupDestination)
        {
            if (string.IsNullOrWhiteSpace(pathToBackup))
                throw new ArgumentNullException("pathToBackup");
            if (!Directory.Exists(pathToBackup))
                throw new DirectoryNotFoundException("Le chemin à sauvegarder est inaccessible : '" + pathToBackup + "'.");

            if (string.IsNullOrWhiteSpace(backupDestination))
                throw new ArgumentNullException("backupDestination");

            using (ZipFile zip = new ZipFile())
            {
                string localState = Path.Combine(pathToBackup, "LocalState");
                if (Directory.Exists(localState))
                    zip.AddDirectory(localState, "LocalState");

                string settings = Path.Combine(pathToBackup, "Settings");
                if (Directory.Exists(settings))
                    zip.AddDirectory(settings, "Settings");

                zip.Save(backupDestination);
            }
        }

        public string GetInstalledVersion(string installName)
        {
            using (PowerShell pws = PowerShell.Create())
            {
                pws.AddScript("param($installName) Get-AppxPackage -Name $installName | select Version;");
                pws.AddParameter("installName", installName);

                Collection<PSObject> psOutput = pws.Invoke();

                if (psOutput.Any())
                {
                    return psOutput.FirstOrDefault(p => p.Properties.Any()).Properties["Version"].Value.ToString();
                }
            }

            return "";
        }

        public async Task<string> DownloadASync(HmacAuthWebApiClient client, string version, string destination)
        {
            string versionDl = string.Format("{0}{1}{2}",ConfigurationManager.AppSettings["api:downloadUrl"], version,ConfigurationManager.AppSettings["api:downloadMethod"]);

            HttpResponseMessage requestMessage = await client.GetAsync(versionDl);
            string fileName = requestMessage.Headers.FirstOrDefault(h => h.Key == "x-filename").Value.First();
            string fileDestination = Path.Combine(destination, fileName);

            using (FileStream a = new FileStream(fileDestination, FileMode.OpenOrCreate))
            {
                await requestMessage.Content.CopyToAsync(a);    
            }

            return fileDestination;
        }
    }
}
