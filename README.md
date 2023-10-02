# ricaun.Revit.Installation

[![Visual Studio 2022](https://img.shields.io/badge/Visual%20Studio-2022-blue)](../..)
[![Nuke](https://img.shields.io/badge/Nuke-Build-blue)](https://nuke.build/)
[![License MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](../../actions/workflows/Build.yml/badge.svg)](../../actions)
[![.NET Framework 4.5](https://img.shields.io/badge/.NET%20Framework%204.5-blue.svg)](../..)
[![.NET Standard 2.0](https://img.shields.io/badge/-.NET%20Standard%202.0-blue)](../..)
[![.NET 5.0](https://img.shields.io/badge/-.NET%205.0-blue)](../..)

## Features
### ApplicationPluginsUtils
```C#
ApplicationPluginsUtils.DownloadBundle(applicationPluginsFolder, bundleUrl);
ApplicationPluginsUtils.DownloadBundleAsync(applicationPluginsFolder, bundleUrl);
ApplicationPluginsUtils.DeleteBundle(applicationPluginsFolder, bundleName);
```

### RevitInstallationUtils
```C#
RevitInstallationUtils.InstalledRevit;
```

### RevitUtils
```C#
RevitUtils.GetCurrentUserApplicationPluginsFolder();
RevitUtils.GetCurrentUserAddInFolder();
RevitUtils.GetCurrentUserAddInFolder(version);
```
```C#
RevitUtils.GetAllUsersApplicationPluginsFolder();
RevitUtils.GetAllUsersAddInFolder();
RevitUtils.GetAllUsersAddInFolder(version);
```
```C#
RevitUtils.TryGetRevitVersion(assemblyFile, out int revitVersion);
```

## Release

* [Latest release](../../releases/latest)

## License

This project is [licensed](LICENSE) under the [MIT Licence](https://en.wikipedia.org/wiki/MIT_License).

---

Do you like this project? Please [star this project on GitHub](../../stargazers)!