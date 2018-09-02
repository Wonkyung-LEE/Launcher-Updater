using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Win32;

namespace LauncherUpdater
{
    class Program
    {
        public static List<string> GetAssociatedLaunchers(string appPath)
        {
            var associatedLaunchers = new List<string>();
            var settingsKey = Registry.CurrentUser.OpenSubKey(@"Software\LineageLauncher", true);

            if (settingsKey == null)
                return associatedLaunchers;

            foreach (var valueName in settingsKey.GetValueNames())
                if (string.Equals(settingsKey.GetValue(valueName).ToString(), appPath, StringComparison.CurrentCultureIgnoreCase))
                    associatedLaunchers.Add(valueName);

            return associatedLaunchers;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                {
                    Console.WriteLine("Incorrect number of arguments passed. Arguments expected: 1.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Starting launcher update process.");

                var updaterPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var updaterDirectory = Path.GetDirectoryName(updaterPath);

                var launcherLocation = args[0];

                var launcherNames = GetAssociatedLaunchers(launcherLocation);

                if (!launcherNames.Any())
                    throw new Exception("No Launchers found to update.");

                var settingsKey = Registry.CurrentUser.OpenSubKey(@"Software\" + launcherNames[0], true);

                if(settingsKey == null)
                    throw new Exception("Unable to find launcher configuration.");

                var launcherUrl = new Uri(settingsKey.GetValue("LanucherUrl").ToString());

                var processName = Path.GetFileNameWithoutExtension(launcherLocation);
                var processes = Process.GetProcesses();

                Console.WriteLine("Closing launcher process.");
                foreach (var process in processes)
                    if (process.ProcessName == processName)
                        process.Kill();

                Console.WriteLine("Downloading new launcher...");
                var newLauncherPath = Path.Combine(updaterDirectory, "Launcher_Updated.exe");

                using (var client = new WebClient())
                    client.DownloadFile(launcherUrl, newLauncherPath);

                if (!File.Exists(newLauncherPath))
                    return;

                File.Delete(launcherLocation);
                File.Move(newLauncherPath, launcherLocation);

                Console.WriteLine("Update Complete! Launcher is re-opening...");

                var info = new ProcessStartInfo(launcherLocation);

                if (Environment.OSVersion.Version.Major >= 6)
                    info.Verb = "runas";

                Process.Start(info);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred. Please pass the error below to the developer.");
                Console.WriteLine("Exception: " + ex.Message);

                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        } //end main
    } //end class
} //end namespace
