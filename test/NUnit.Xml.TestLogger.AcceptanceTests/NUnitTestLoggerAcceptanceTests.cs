// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NUnitTestLoggerAcceptanceTests
    {
        private readonly string resultsFile;
        private readonly XDocument resultsXml;

        public NUnitTestLoggerAcceptanceTests()
        {
            this.resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "test-results.xml");
            this.resultsXml = XDocument.Load(this.resultsFile);
        }

        [ClassInitialize]
        public static void SuiteInitialize(TestContext context)
        {
            DotnetTestFixture.Execute();
        }

        [TestMethod]
        public void TestRunWithLoggerAndFilePathShouldCreateResultsFile()
        {
            Assert.IsTrue(File.Exists(this.resultsFile));
        }

        [TestMethod]
        public void TestResultFileShouldContainTestRunInformation()
        {
            var node = this.resultsXml.XPathSelectElement("/test-run");

            Assert.IsNotNull(node);
            Assert.AreEqual("46", node.Attribute(XName.Get("testcasecount")).Value);
            Assert.AreEqual("18", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);

            // Start time and End time should be valid dates
            Convert.ToDateTime(node.Attribute(XName.Get("start-time")).Value);
            Convert.ToDateTime(node.Attribute(XName.Get("end-time")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainAssemblyTestSuite()
        {
            var node = this.resultsXml.XPathSelectElement("/test-run/test-suite[@type='Assembly']");

            Assert.IsNotNull(node);
            Assert.AreEqual("46", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("18", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("NUnit.Xml.TestLogger.NetCore.Tests.dll", node.Attribute(XName.Get("name")).Value);
            Assert.AreEqual(DotnetTestFixture.TestAssembly, node.Attribute(XName.Get("fullname")).Value);
        }

        [TestMethod]
        [DataRow("NetFull")]
        [DataRow("Tests2")]
        public void TestResultFileShouldContainNamespaceTestSuite(string testNamespace)
        {
            // Two namespaces in test asset are:
            // NUnit.Xml.TestLogger.NetFull.Tests and NUnit.Xml.TestLogger.Tests2
            var query = string.Format("/test-run//test-suite[@type='TestSuite' and @name='{0}']", testNamespace);
            var node = this.resultsXml.XPathSelectElement(query);

            Assert.IsNotNull(node);
            Assert.AreEqual("23", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("9", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("7", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("4", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("3", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("NUnit.Xml.TestLogger." + testNamespace, node.Attribute(XName.Get("fullname")).Value);
        }

        [TestMethod]
        [DataRow("NUnit.Xml.TestLogger.NetFull.Tests")]
        [DataRow("NUnit.Xml.TestLogger.Tests2")]
        public void TestResultFileShouldContainPartsOfNamespaceTestSuite(string testNamespace)
        {
            // Two namespaces in test asset are:
            // NUnit.Xml.TestLogger.NetFull.Tests and NUnit.Xml.TestLogger.Tests2
            var fullName = string.Empty;
            foreach (var part in testNamespace.Split("."))
            {
                var query = string.Format("/test-run//test-suite[@type='TestSuite' and @name='{0}']", part);
                var node = this.resultsXml.XPathSelectElement(query);
                fullName = fullName == string.Empty ? part : fullName + "." + part;

                Assert.IsNotNull(node);
                Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
                Assert.AreEqual(fullName, node.Attribute(XName.Get("fullname")).Value);
            }
        }

        [TestMethod]
        [DataRow("NUnit.Xml.TestLogger.NetFull.Tests")]
        [DataRow("NUnit.Xml.TestLogger.Tests2")]
        public void TestResultFileShouldContainTestCasePropertiesForTestWithPropertyAttributes(string testNamespace)
        {
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.WithProperty']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");

            var propertyElement = propertiesElement.Element("property");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);
            Assert.AreEqual("Property value", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        [DataRow("NUnit.Xml.TestLogger.NetFull.Tests")]
        [DataRow("NUnit.Xml.TestLogger.Tests2")]
        public void TestResultFileShouldNotContainTestCasePropertiesForTestWithNoPropertyAttributes(string testNamespace)
        {
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.NoProperty']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNull(propertiesElement, "properties element");
        }
    }
}
