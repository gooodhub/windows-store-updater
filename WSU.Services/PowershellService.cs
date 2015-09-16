using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace WSU.Services
{
    public static class PowershellService
    {
        public static Collection<PSObject> ExecuteScript(string scriptFilePath, params KeyValuePair<string, object>[] args)
        {
            return ExecuteScript<PSObject>(scriptFilePath, args);
        }

        public static Collection<T> ExecuteScript<T>(string scriptFilePath, params KeyValuePair<string, object>[] args)
        {
            if (string.IsNullOrWhiteSpace(scriptFilePath))
                throw new ArgumentNullException("scriptFilePath");
            if (!File.Exists(scriptFilePath))
                throw new FileNotFoundException("Le script n'est pas accessible avec le chemin '" + scriptFilePath + "'.");

            scriptFilePath = string.Format("&(\"{0}\")", scriptFilePath);

            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();
                using (RunspaceInvoke runspaceInvoker = new RunspaceInvoke(runspace))
                {
                    runspaceInvoker.Invoke("Set-ExecutionPolicy Unrestricted -Scope Process -force");

                    using (PowerShell powerShell = PowerShell.Create())
                    {
                        powerShell.Runspace = runspace;

                        // when running script ps1, need to concact params to script => cannot use AddParameter method
                        string argsString = " ";
                        if (args != null && args.Any())
                            argsString = args.Aggregate(argsString, (current, keyValuePair) => current + (keyValuePair.Value != null && !string.IsNullOrWhiteSpace(keyValuePair.Value.ToString()) ? string.Format("-{0} '{1}' ", keyValuePair.Key, keyValuePair.Value) : "-" + keyValuePair.Key));
                        powerShell.AddScript(scriptFilePath + argsString.TrimEnd());

                        Collection<T> results = powerShell.Invoke<T>();
                        Collection<ErrorRecord> errors = powerShell.Streams.Error.ReadAll();
                        if (errors != null && errors.Any())
                            throw new Exception(String.Join(Environment.NewLine, errors));
                        return results;
                    }
                }
            }
        }
    }
}
