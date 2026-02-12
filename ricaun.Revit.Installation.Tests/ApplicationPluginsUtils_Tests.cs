using NUnit.Framework;
using NUnit.Framework.Constraints;
using ricaun.Revit.Installation.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ricaun.Revit.Installation.Tests
{
    public class ApplicationPluginsUtils_Tests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Create a temporary folder for the tests
            var tempFolder = Path.Combine(Path.GetTempPath(), "ApplicationPlugins");
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            Directory.CreateDirectory(tempFolder);
        }

        [TestCase("RevitAddin.DA.Tester")]
        public async Task ApplicationPluginsUtils_Test_Download_Async(string projectName)
        {
            var bundleUrl = $@"https://github.com/ricaun-io/{projectName}/releases/latest/download/{projectName}.bundle.zip";

            var applicationPluginsFolder = Path.Combine(Path.GetTempPath(), "ApplicationPlugins");
            var bundleName = Path.GetFileNameWithoutExtension(bundleUrl);

            Console.WriteLine($"DownloadBundle: {bundleName}");

            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(0);
                    ApplicationPluginsUtils.DownloadBundle(applicationPluginsFolder, bundleUrl, (e) =>
                    {
                        Console.WriteLine(e);
                        Assert.Fail(e.Message);
                    }, (log) =>
                    {
                        //Console.WriteLine(log);
                    });
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        [TestCase("RevitAddin.DA.Tester")]
        public void ApplicationPluginsUtils_Test_Download(string projectName)
        {
            var bundleUrl = $@"https://github.com/ricaun-io/{projectName}/releases/latest/download/{projectName}.bundle.zip";

            var applicationPluginsFolder = Path.Combine(Path.GetTempPath(), "ApplicationPlugins");
            var bundleName = Path.GetFileNameWithoutExtension(bundleUrl);

            Console.WriteLine($"DownloadBundle: {bundleName}");

            ApplicationPluginsUtils.DownloadBundle(applicationPluginsFolder, bundleUrl, (e) =>
            {
                Console.WriteLine(e);
                Assert.Fail(e.Message);
            }, (log) =>
            {
                Console.WriteLine(log);
            });

            Console.WriteLine($"Bundle Exists: {Directory.Exists(Path.Combine(applicationPluginsFolder, bundleName))}");
            Assert.IsTrue(Directory.Exists(Path.Combine(applicationPluginsFolder, bundleName)));

            Thread.Sleep(1000);

            ApplicationPluginsUtils.DeleteBundle(applicationPluginsFolder, bundleName);
            Console.WriteLine($"Bundle Exists: {Directory.Exists(Path.Combine(applicationPluginsFolder, bundleName))}");
        }

        [TestCase("FakeBundle", false)]
        [TestCase("FakeBundle", true)]
        [TestCase("FakeBundle", false, false)]
        [TestCase("FakeBundle", true, false)]
        public void ApplicationPluginsUtils_Test_BundleCreatorUtils(string projectName, bool includeBundleDirectory, bool includeContents = true)
        {
            projectName += Guid.NewGuid().ToString("N");
            var bundleUrl = BundleCreatorUtils.CreateBundleZip(projectName, includeBundleDirectory, includeContents);

            var applicationPluginsFolder = Path.Combine(Path.GetTempPath(), "ApplicationPlugins");
            var bundleName = Path.GetFileNameWithoutExtension(bundleUrl);

            Console.WriteLine($"DownloadBundle: {bundleName}");

            ApplicationPluginsUtils.DownloadBundle(applicationPluginsFolder, bundleUrl, (e) =>
            {
                Console.WriteLine(e);
                Assert.Fail(e.Message);
            }, (log) =>
            {
                Console.WriteLine(log);
            });

            var bundleDirectory = Path.Combine(applicationPluginsFolder, bundleName);
            var bundlePackageContentsPath = Path.Combine(bundleDirectory, "PackageContents.xml");

            Console.WriteLine($"Bundle Exists: {Directory.Exists(bundleDirectory)}");
            Assert.IsTrue(Directory.Exists(bundleDirectory));
            Assert.IsTrue(File.Exists(bundlePackageContentsPath));

            Thread.Sleep(1000);

            ApplicationPluginsUtils.DeleteBundle(applicationPluginsFolder, bundleName);
            Console.WriteLine($"Bundle Exists: {Directory.Exists(bundleDirectory)}");

            Assert.IsFalse(Directory.Exists(bundleDirectory));
        }

        [TestCase("FakeBundleUsedByProcess")]
        public void ApplicationPluginsUtils_Test_BundleCreatorUtils_FileUsedByProcess(string projectName)
        {
            projectName += Guid.NewGuid().ToString("N");
            var bundleUrl = BundleCreatorUtils.CreateBundleZip(projectName);

            var applicationPluginsFolder = Path.Combine(Path.GetTempPath(), "ApplicationPlugins");
            var bundleName = Path.GetFileNameWithoutExtension(bundleUrl);

            Console.WriteLine($"DownloadBundle: {bundleName}");

            ApplicationPluginsUtils.DownloadBundle(applicationPluginsFolder, bundleUrl, (e) =>
            {
                Console.WriteLine(e);
                Assert.Fail(e.Message);
            }, (log) =>
            {
                Console.WriteLine(log);
            });

            var bundleDirectory = Path.Combine(applicationPluginsFolder, bundleName);
            var bundlePackageContentsPath = Path.Combine(bundleDirectory, "PackageContents.xml");

            Console.WriteLine($"Bundle Exists: {Directory.Exists(bundleDirectory)}");
            Assert.IsTrue(Directory.Exists(bundleDirectory));
            Assert.IsTrue(File.Exists(bundlePackageContentsPath));

            Thread.Sleep(1000);

            var fileName = "File.xml";
            var files = Directory.GetFiles(bundleDirectory, fileName, SearchOption.AllDirectories);
            Assert.IsTrue(files.Length > 0, $"File '{fileName}' not found in bundle directory.");
            var streams = files.Select(file => File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None)).ToArray();

            ApplicationPluginsUtils.DeleteBundle(applicationPluginsFolder, bundleName);
            Console.WriteLine($"Bundle Exists: {Directory.Exists(bundleDirectory)}");

            Assert.IsTrue(Directory.Exists(bundleDirectory));
            Assert.IsFalse(File.Exists(bundlePackageContentsPath));

            foreach (var stream in streams)
            {
                stream.Dispose();
            }

            ApplicationPluginsUtils.DeleteBundle(applicationPluginsFolder, bundleName);
            Console.WriteLine($"Bundle Exists: {Directory.Exists(bundleDirectory)}");

            Assert.IsFalse(Directory.Exists(bundleDirectory));
        }
    }
}