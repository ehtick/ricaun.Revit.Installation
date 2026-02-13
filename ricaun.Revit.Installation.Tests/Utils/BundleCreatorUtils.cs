using System;
using System.IO;
using System.IO.Compression;

namespace ricaun.Revit.Installation.Tests.Utils
{
    public static class BundleCreatorUtils
    {
        public static string CreateBundleZip(string projectName, bool includeBundleDirectory = true, bool includeContents = true, int numberFileMax = 5)
        {
            var bundleFileName = $"{projectName}.bundle";
            var zipFileName = $"{bundleFileName}.zip";
            var tempPath = Path.Combine(Path.GetTempPath(), "BundleCreatorUtils");

            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            Directory.CreateDirectory(tempPath);

            var zipFilePath = Path.Combine(tempPath, zipFileName);
            var sourceBundlePath = Path.Combine(tempPath, bundleFileName);
            Directory.CreateDirectory(sourceBundlePath);

            var packageContentsFile = Path.Combine(sourceBundlePath, "PackageContents.xml");
            File.WriteAllText(packageContentsFile, string.Empty);

            if (includeContents)
            {
                var contentsFolder = Path.Combine(sourceBundlePath, "Contents");
                Directory.CreateDirectory(contentsFolder);
                for (int i = 0; i < numberFileMax; i++)
                {
                    var numberFolder = Path.Combine(contentsFolder, $"Number_{i}");
                    Directory.CreateDirectory(numberFolder);
                    var contentFile = Path.Combine(numberFolder, "File.xml");
                    File.WriteAllText(contentFile, string.Empty);
                    var numberFile = Path.Combine(numberFolder, $"Number_{i}.xml");
                    File.WriteAllText(numberFile, string.Empty);
                }
            }

            ZipFile.CreateFromDirectory(sourceBundlePath, zipFilePath, CompressionLevel.NoCompression, includeBundleDirectory);

            return zipFilePath;
        }
    }
}
