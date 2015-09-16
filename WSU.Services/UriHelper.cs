using System;
using System.IO;
using Microsoft.Win32;

namespace WSU.Services
{
    public static class UriHelper
    {
        public static bool IsUriRegistered(string protocolName, string appPath)
        {
            // On vérifie déjà que l'URI existe
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(protocolName))
            {
                if (key == null)
                    return false;

                // On vérifie qu'elle a une clé "URL Protocol", sinon elle ne permet pas de gérer des URI
                if (key.GetValue("URL Protocol") == null)
                    return false;

                // On vérifie qu'il a les 2 sub key qu'il faut
                if (key.OpenSubKey("shell") == null || key.OpenSubKey("shell\\open") == null)
                    return false;

                using (RegistryKey commandKey = key.OpenSubKey("shell\\open\\command"))
                {
                    if (commandKey == null)
                        return false;

                    var path = GetLaunchPath(appPath);
                    return commandKey.GetValue(null).ToString().Equals(path);
                }
            }
        }

        public static void RegisterUri(string protocolName, string appPath, string description)
        {
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(protocolName))
            {
                key.SetValue(null, description);
                key.SetValue("URL Protocol", string.Empty);

                using (RegistryKey shell = key.CreateSubKey("shell"))
                using (RegistryKey open = shell.CreateSubKey("open"))
                using (RegistryKey command = open.CreateSubKey("command"))
                {
                    command.SetValue(null, GetLaunchPath(appPath));
                }
            }
        }

        private static string GetLaunchPath(string appPath)
        {
            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string rundll32 = Path.Combine(system32, "rundll32.exe");
            string dfshim = Path.Combine(system32, "dfshim.dll");

            return string.Format("\"{0}\" \"{1}\",ShOpenVerbShortcut {2}|%1", rundll32, dfshim, appPath);
        }
    }
}
