using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;

namespace ricaun.Revit.Installation
{
    /// <summary>
    /// Utility helpers to manage Revit application plugins bundles.
    /// </summary>
    /// <remarks>
    /// Provides functionality to delete bundles from an ApplicationPlugins folder,
    /// download bundle ZIP files from an address and extract them, and several
    /// internal helpers related to ZIP extraction and path handling.
    /// Based on: https://github.com/ricaun-io/ricaun.Revit.Github/blob/master/ricaun.Revit.Github/Services/DownloadBundleService.cs
    /// </remarks>
    public class ApplicationPluginsUtils
    {
        #region const
        /// <summary>
        /// File-system suffix used to identify Revit bundle folders.
        /// </summary>
        private const string CONST_BUNDLE = ".bundle";

        /// <summary>
        /// Timeout (in milliseconds) used when waiting for a named <see cref="Mutex"/>.
        /// </summary>
        private const int MutexMillisecondsTimeout = 10000;

        /// <summary>
        /// File name used to identify the bundle manifest file inside a bundle.
        /// </summary>
        private const string CONST_PACKAGE_CONTENTS = "PackageContents.xml";
        #endregion

        #region Delete
        /// <summary>
        /// Deletes a bundle folder and removes its bundle manifest files.
        /// </summary>
        /// <param name="applicationPluginsFolder">
        /// The root folder that contains application plugin bundles or the full path to a single `.bundle` folder.
        /// </param>
        /// <param name="bundleName">
        /// The name of the bundle directory to delete. Must end with <c>".bundle"</c>.
        /// </param>
        /// <param name="logFileConsole">
        /// Optional callback invoked with human-readable log messages for file and directory operations.
        /// </param>
        /// <exception cref="Exception">
        /// Thrown if <paramref name="bundleName"/> does not end with the expected <c>".bundle"</c> suffix.
        /// </exception>
        public static void DeleteBundle(string applicationPluginsFolder, string bundleName, Action<string> logFileConsole = null)
        {
            if (bundleName.EndsWith(CONST_BUNDLE) == false)
                throw new Exception(string.Format("BundleName {0} does not end with {0}", bundleName, CONST_BUNDLE));

            using (var mutex = new Mutex(false, bundleName))
            {
                mutex.WaitOne(MutexMillisecondsTimeout);
                var bundleDirectory = Path.Combine(applicationPluginsFolder, bundleName);
                DeleteDirectories(bundleDirectory);
                DeletePackageContents(bundleDirectory);
                mutex.ReleaseMutex();
            }

            void DeletePackageContents(string directory)
            {
                if (!Directory.Exists(directory)) return;
                foreach (var file in Directory.GetFiles(directory, CONST_PACKAGE_CONTENTS, SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                        logFileConsole?.Invoke($"Deleted File: '{file}'");
                    }
                    catch { logFileConsole?.Invoke($"Not Deleted File: '{file}'"); }
                }
            }

            void DeleteDirectories(string directory)
            {
                DirectoryInfo dir = new DirectoryInfo(directory);
                if (!dir.Exists) return;

                // Start deleting the subdirectories first to avoid checking the same files multiple times.
                // And if the directory contains subdirectories, it will not be deleted until all the subdirectories are deleted.
                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    DeleteDirectories(di.FullName);
                }

                try
                {
                    if (Directory.GetDirectories(dir.FullName).Length > 0)
                    {
                        throw new IOException($"Directory '{dir.FullName}' contains subdirectories and cannot be deleted.");
                    }
                    if (HasAnyFileInUse(dir.FullName))
                    {
                        throw new IOException($"Directory '{dir.FullName}' contains files that are currently in use and cannot be deleted.");
                    }
                    dir.Delete(true);
                    logFileConsole?.Invoke($"Deleted Directory: '{dir.FullName}'");
                    return;
                }
                catch (Exception ex) { logFileConsole?.Invoke($"Not Deleted Directory: {ex.Message}"); }
            }

            bool HasAnyFileInUse(string directory)
            {
                if (!Directory.Exists(directory)) return false;
                foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        using (File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    }
                    catch { return true; }
                }
                return false;
            }
        }
        #endregion

        #region Download
        /// <summary>
        /// Downloads a bundle ZIP from the specified <paramref name="address"/> and extracts it into the
        /// provided <paramref name="applicationPluginsFolder"/> (or into a subfolder named after the bundle).
        /// </summary>
        /// <param name="applicationPluginsFolder">
        /// The destination folder that contains ApplicationPlugins bundles or the full path to a single
        /// `.bundle` folder. If the folder does not exist it will be created.
        /// </param>
        /// <param name="address">
        /// The source address (typically an HTTP(S) URL) of the bundle ZIP file to download. The file name
        /// portion of the address is used to create a temporary ZIP file inside <paramref name="applicationPluginsFolder"/>.
        /// </param>
        /// <param name="downloadFileException">
        /// Optional callback invoked when an exception occurs during download or extraction. The exception
        /// instance is provided to the callback for logging or handling. If this callback is null, exceptions
        /// are caught silently and the method will return <c>false</c>.
        /// </param>
        /// <param name="logFileConsole">
        /// Optional callback invoked with human-readable log messages for file and directory operations
        /// performed during download and extraction.
        /// </param>
        /// <returns>
        /// <c>true</c> if the bundle was successfully downloaded and extraction was attempted; otherwise <c>false</c>.
        /// Note: extraction may be a no-op if the downloaded file is not a ZIP; the method still returns <c>true</c> when
        /// no exceptions were raised during the operation.
        /// </returns>
        /// <remarks>
        /// - A named <see cref="Mutex"/> based on the bundle name (derived from the ZIP file name) is used to
        ///   avoid concurrent operations on the same bundle.
        /// - TLS 1.2 is enabled via <see cref="System.Net.ServicePointManager"/> to ensure secure downloads.
        /// - The temporary ZIP file is deleted after extraction attempt regardless of success or failure.
        /// - Any exceptions that occur are forwarded to <paramref name="downloadFileException"/> if provided.
        /// </remarks>
        public static bool DownloadBundle(string applicationPluginsFolder, string address, Action<Exception> downloadFileException = null, Action<string> logFileConsole = null)
        {
            if (!Directory.Exists(applicationPluginsFolder))
                Directory.CreateDirectory(applicationPluginsFolder);

            var fileName = Path.GetFileName(address);
            var zipPath = Path.Combine(applicationPluginsFolder, fileName);
            var result = false;

            var bundleName = Path.GetFileNameWithoutExtension(fileName);
            using (var mutex = new Mutex(false, bundleName))
            {
                mutex.WaitOne(MutexMillisecondsTimeout);
                using (var client = new WebClient())
                {
                    System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
                    client.Headers[HttpRequestHeader.UserAgent] = nameof(ApplicationPluginsUtils);
                    try
                    {
                        client.DownloadFile(new Uri(address), zipPath);
                        ExtractBundleZipToDirectory(zipPath, applicationPluginsFolder, downloadFileException, logFileConsole);
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        downloadFileException?.Invoke(ex);
                    }
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                }
                mutex.ReleaseMutex();
            }

            return result;
        }
        #endregion

        #region BundleZip
        private static bool IsEntryPackageContents(ZipArchiveEntry entry)
        {
            return entry.Name.Equals(CONST_PACKAGE_CONTENTS, StringComparison.InvariantCultureIgnoreCase);
        }
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
                string baseDirectory = string.Empty;

                // Check if first file is inside the bundle folder, to ignore when extract.
                var firstFile = archive.Entries.FirstOrDefault(IsEntryPackageContents);
                if (firstFile is not null)
                {
                    var firstDirectory = Path.GetDirectoryName(firstFile.FullName);
                    if (firstDirectory.EndsWith(CONST_BUNDLE, StringComparison.InvariantCultureIgnoreCase))
                        baseDirectory = firstDirectory;
                }

                foreach (var file in archive.Entries.OrderBy(IsEntryPackageContents))
                {
                    var fileFullName = file.FullName.Substring(baseDirectory.Length).TrimStart('/').TrimStart('\\');

                    var completeFileName = Path.Combine(destinationDirectoryName, fileFullName);
                    var directory = Path.GetDirectoryName(completeFileName);

                    Debug.WriteLine($"{fileFullName} -\t {baseDirectory} -\t {completeFileName}");

                    logFileConsole?.Invoke($"{fileFullName} -\t {baseDirectory} -\t {completeFileName}");

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

        internal static string GetBaseDirectory(string fullPath)
        {
            if (Path.IsPathRooted(fullPath))
                return Path.GetPathRoot(fullPath);

            var baseDirectory = Path.GetDirectoryName(fullPath);
            while (!string.IsNullOrEmpty(Path.GetDirectoryName(baseDirectory)))
            {
                baseDirectory = Path.GetDirectoryName(baseDirectory);
            }
            return baseDirectory;
        }

        #endregion
    }
}