// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NUnitTestLoggerResultDirectoryAcceptanceTests
    {
        private readonly string resultsFile;
        private readonly XDocument resultsXml;

        public NUnitTestLoggerResultDirectoryAcceptanceTests()
        {
            this.resultsFile = Path.Combine(DotnetTestFixture.ResultDirectory, "test-results.xml");
            this.resultsXml = XDocument.Load(this.resultsFile);
        }

        [ClassInitialize]
        public static void SuiteInitialize(TestContext context)
        {
            DotnetTestFixture.Execute("test-results.xml", "./artifacts");
        }

        [TestMethod]
        public void TestRunWithResultDirectoryAndFileNameShouldCreateResultsFile()
        {
            Assert.IsTrue(File.Exists(this.resultsFile));
        }
    }
}
