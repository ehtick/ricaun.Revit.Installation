﻿using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace ricaun.Revit.Installation.Tests
{
    public class RevitUtils_Tests
    {
        [Test]
        public void RevitUtils_Test_CurrentUser()
        {
            Console.WriteLine(RevitUtils.GetCurrentUserApplicationPluginsFolder());
            Console.WriteLine(RevitUtils.GetCurrentUserAddInFolder());
            Console.WriteLine(RevitUtils.GetCurrentUserAddInFolder(2021));
        }

        [Test]
        public void RevitUtils_Test_AllUsers()
        {
            Console.WriteLine(RevitUtils.GetAllUsersApplicationPluginsFolder());
            Console.WriteLine(RevitUtils.GetAllUsersAddInFolder());
            Console.WriteLine(RevitUtils.GetAllUsersAddInFolder(2021));
        }

        [Test]
        public void RevitUtils_Test_GetReferences()
        {
            var assemblyFile = Assembly.GetExecutingAssembly().Location;
            Assert.IsFalse(RevitUtils.TryGetRevitVersion(assemblyFile, out int revitVersion));
            Assert.That(revitVersion, Is.EqualTo(0));
        }

        [Test]
        public void RevitUtils_Test_GetReferencesRevit()
        {
            var fileInfo = new FileInfo("Files/RevitAddin1.dll");
            var assemblyFile = fileInfo.FullName;
            Console.WriteLine(assemblyFile);
            Assert.IsTrue(RevitUtils.TryGetRevitVersion(assemblyFile, out int revitVersion));
            Assert.That(revitVersion, Is.EqualTo(2017));
            Console.WriteLine(revitVersion);
        }
    }
}