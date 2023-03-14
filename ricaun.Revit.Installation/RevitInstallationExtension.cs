﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ricaun.Revit.Installation
{
    /// <summary>
    /// RevitInstallationExtension
    /// </summary>
    public static class RevitInstallationExtension
    {
        #region const
        /// <summary>
        /// ExecuteName
        /// </summary>
        public const string ExecuteName = "Revit.exe";
        /// <summary>
        /// ProcessName
        /// </summary>
        public const string ProcessName = "Revit";
        /// <summary>
        /// DefaultArguments
        /// </summary>
        public const string DefaultArguments = "/language ENU";
        #endregion

        #region RevitInstallation
        /// <summary>
        /// Start RevitInstallation
        /// </summary>
        /// <param name="revitInstallation"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static Process Start(this RevitInstallation revitInstallation, string arguments = DefaultArguments)
        {
            var revitExe = Path.Combine(revitInstallation.InstallLocation, ExecuteName);
            var process = Process.Start(revitExe, arguments);
            return process;
        }

        /// <summary>
        /// Start RevitInstallation with Jornal in <paramref name="workingDirectory"/>.
        /// Any .addin in the <paramref name="workingDirectory"/> gonna be loaded.
        /// </summary>
        /// <param name="revitInstallation"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static Process StartWithJornal(this RevitInstallation revitInstallation,
            string workingDirectory,
            string arguments = DefaultArguments)
        {
            var revitExe = Path.Combine(revitInstallation.InstallLocation, ExecuteName);
            string jornalPath = CreateJornalFile(workingDirectory);
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = revitExe,
                WorkingDirectory = workingDirectory,
                Arguments = jornalPath + " " + arguments,
                UseShellExecute = false
            };
            var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            return process;
        }

        /// <summary>
        /// Try get process with the same <paramref name="revitInstallation"/> install location. 
        /// </summary>
        /// <param name="revitInstallation"></param>
        /// <param name="process"></param>
        /// <returns></returns>
        public static bool TryGetProcess(this RevitInstallation revitInstallation, out Process process)
        {
            process = GetProcesses(revitInstallation).FirstOrDefault();
            return process is not null;
        }

        /// <summary>
        /// Get all processes with the same <paramref name="revitInstallation"/> install location.
        /// </summary>
        /// <param name="revitInstallation"></param>
        /// <returns></returns>
        public static Process[] GetProcesses(this RevitInstallation revitInstallation)
        {
            return GetRevitProcesses()
                .Where(e => e.GetMainModuleFileName().Contains(revitInstallation.InstallLocation))
                .ToArray();
        }
        #endregion

        #region RevitInstallations
        /// <summary>
        /// Try get RevitInstallation with the same <paramref name="revitVersion"/>. 
        /// </summary>
        /// <param name="revitInstallations"></param>
        /// <param name="revitVersion"></param>
        /// <param name="revitInstallation"></param>
        /// <returns></returns>
        public static bool TryGetRevitInstallation(this IEnumerable<RevitInstallation> revitInstallations, int revitVersion, out RevitInstallation revitInstallation)
        {
            revitInstallation = revitInstallations.FirstOrDefault(e => e.Version == revitVersion);
            return revitInstallation is not null;
        }

        /// <summary>
        /// Try get RevitInstallation with the same <paramref name="revitVersion"/> or greater. 
        /// </summary>
        /// <param name="revitInstallations"></param>
        /// <param name="revitVersion"></param>
        /// <param name="revitInstallation"></param>
        /// <returns></returns>
        public static bool TryGetRevitInstallationGreater(this IEnumerable<RevitInstallation> revitInstallations, int revitVersion, out RevitInstallation revitInstallation)
        {
            revitInstallation = revitInstallations.FirstOrDefault(e => e.Version >= revitVersion);
            return revitInstallation is not null;
        }
        #endregion

        #region private
        private static Process[] GetRevitProcesses()
        {
            return Process.GetProcessesByName(ProcessName);
        }

        private static string CreateJornalFile(string workingDirectory)
        {
            const string JORNAL_FILE = "journal.txt";
            string jornalPath = Path.Combine(workingDirectory, JORNAL_FILE);
            File.WriteAllText(jornalPath, $"'C {DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss.fff")}; \r\nDim Jrn\r\nSet Jrn = CrsJournalScript");
            return jornalPath;
        }
        #endregion
    }
}
