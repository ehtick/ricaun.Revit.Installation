﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ricaun.Revit.Installation
{
    /// <summary>
    /// ApplicationPluginsUtils
    /// <code>Based: https://github.com/ricaun-io/ricaun.Revit.Github/blob/master/ricaun.Revit.Github/Services/DownloadBundleService.cs</code>
    /// </summary>
    public class ApplicationPluginsUtils
    {
        #region const
        private const string CONST_BUNDLE = ".bundle";
        #endregion

        #region Delete
        /// <summary>
        /// DeleteBundle
        /// </summary>
        /// <param name="applicationPluginsFolder"></param>
        /// <param name="bundleName"></param>
        /// <exception cref="Exception"></exception>
        public static void DeleteBundle(string applicationPluginsFolder, string bundleName)
        {
            if (bundleName.EndsWith(CONST_BUNDLE) == false)
                throw new Exception(string.Format("BundleName {0} does not end with {0}", bundleName, CONST_BUNDLE));

            DeleteDirectoryAndFiles(Path.Combine(applicationPluginsFolder, bundleName));

            void DeleteDirectoryAndFiles(string directory)
            {
                DirectoryInfo dir = new DirectoryInfo(directory);
                if (!dir.Exists) return;
                foreach (FileInfo fi in dir.GetFiles())
                {
                    try
                    {
                        fi.Delete();
                    }
                    catch { }
                }
                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    DeleteDirectoryAndFiles(di.FullName);
                    try
                    {
                        di.Delete();
                    }
                    catch { }
                }
                try
                {
                    dir.Delete();
                }
                catch { }
            }
        }
        #endregion

        #region Download
        /// <summary>
        /// Download and unzip Bundle
        /// </summary>
        /// <param name="applicationPluginsFolder">Folder of the ApplicationPlugins or the Application.bundle</param>
        /// <param name="address"></param>
        /// <param name="downloadFileException"></param>
        /// <param name="logFileConsole"></param>
        /// <returns></returns>
        public static bool DownloadBundle(string applicationPluginsFolder, string address, Action<Exception> downloadFileException = null, Action<string> logFileConsole = null)
        {
            var task = Task.Run(async () =>
            {
                return await DownloadBundleAsync(applicationPluginsFolder, address, downloadFileException, logFileConsole);
            });
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Download and unzip Bundle Async
        /// </summary>
        /// <param name="applicationPluginsFolder">Folder of the ApplicationPlugins or the Application.bundle</param>
        /// <param name="address"></param>
        /// <param name="downloadFileException"></param>
        /// <param name="logFileConsole"></param>
        /// <returns></returns>
        public static async Task<bool> DownloadBundleAsync(string applicationPluginsFolder, string address, Action<Exception> downloadFileException = null, Action<string> logFileConsole = null)
        {
            if (!Directory.Exists(applicationPluginsFolder))
                Directory.CreateDirectory(applicationPluginsFolder);

            var fileName = Path.GetFileName(address);
            var zipPath = Path.Combine(applicationPluginsFolder, fileName);
            var result = false;

            using (var client = new WebClient())
            {
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
                client.Headers[HttpRequestHeader.UserAgent] = nameof(ApplicationPluginsUtils);
                try
                {
                    await client.DownloadFileTaskAsync(new Uri(address), zipPath);
                    ExtractBundleZipToDirectory(zipPath, applicationPluginsFolder, downloadFileException, logFileConsole);
                    result = true;
                }
                catch (Exception ex)
                {
                    downloadFileException?.Invoke(ex);
                }
                if (File.Exists(zipPath)) File.Delete(zipPath);
            }

            return result;
        }
        #endregion

        #region BundleZip
        /// <summary>
        /// ExtractToDirectory with overwrite enable
        /// </summary>
        /// <param name="archiveFileName"></param>
        /// <param name="destinationDirectoryName"></param>
        /// <param name="extractFileException"></param>
        /// <param name="logFileConsole"></param>
        private static void ExtractBundleZipToDirectory(string archiveFileName, string destinationDirectoryName, Action<Exception> extractFileException = null, Action<string> logFileConsole = null)
        {
            if (Path.GetExtension(archiveFileName) != ".zip") return;

            // If destination does not have .bundle in the end
            if (destinationDirectoryName.EndsWith(CONST_BUNDLE) == false)
                destinationDirectoryName = Path.Combine(destinationDirectoryName, Path.GetFileNameWithoutExtension(archiveFileName));

            using (var archive = ZipFile.OpenRead(archiveFileName))
            {
                string baseDirectory = null;

                // Check if first file is inside the bundle folder, to ignore when extract.
                var firstFile = archive.Entries.FirstOrDefault();
                if (firstFile is not null)
                {
                    var firstDirectory = Path.GetDirectoryName(firstFile.FullName);
                    if (firstDirectory.EndsWith(CONST_BUNDLE, StringComparison.InvariantCultureIgnoreCase))
                        baseDirectory = firstDirectory;
                }

                foreach (var file in archive.Entries.Reverse())
                {
                    var fileFullName = file.FullName.Substring(baseDirectory.Length).TrimStart('/');

                    var completeFileName = Path.Combine(destinationDirectoryName, fileFullName);
                    var directory = Path.GetDirectoryName(completeFileName);

                    Debug.WriteLine($"{fileFullName} |\t {baseDirectory} |\t {completeFileName}");

                    logFileConsole?.Invoke($"{fileFullName} |\t {baseDirectory} |\t {completeFileName}");

                    if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    if (file.Name != "")
                    {
                        try
                        {
                            file.ExtractToFile(completeFileName, true);
                        }
                        catch (Exception ex)
                        {
                            if (extractFileException is null) throw;
                            extractFileException.Invoke(ex);
                        }
                    }
                }
            }
        }
        private static void ExtractBundleZipToDirector2y(string archiveFileName, string destinationDirectoryName, Action<Exception> extractFileException = null, Action<string> logFileConsole = null)
        {
            if (Path.GetExtension(archiveFileName) != ".zip") return;

            // If destination does not have .bundle in the end
            if (destinationDirectoryName.EndsWith(CONST_BUNDLE) == false)
                destinationDirectoryName = Path.Combine(destinationDirectoryName, Path.GetFileNameWithoutExtension(archiveFileName));

            using (var archive = ZipFile.OpenRead(archiveFileName))
            {
                string baseDirectory = null;
                foreach (var file in archive.Entries)
                {
                    if (baseDirectory == null)
                        baseDirectory = Path.GetDirectoryName(file.FullName);
                    if (baseDirectory.EndsWith(CONST_BUNDLE) == false)
                        baseDirectory = "";

                    var fileFullName = file.FullName.Substring(baseDirectory.Length).TrimStart('/');

                    var completeFileName = Path.Combine(destinationDirectoryName, fileFullName);
                    var directory = Path.GetDirectoryName(completeFileName);

                    Debug.WriteLine($"{fileFullName} |\t {baseDirectory} |\t {completeFileName}");

                    logFileConsole?.Invoke($"{fileFullName} |\t {baseDirectory} |\t {completeFileName}");

                    if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    if (file.Name != "")
                    {
                        try
                        {
                            file.ExtractToFile(completeFileName, true);
                        }
                        catch (Exception ex)
                        {
                            if (extractFileException is null) throw;
                            extractFileException.Invoke(ex);
                        }
                    }
                }
            }

        }
        #endregion
    }
}