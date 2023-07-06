// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NUnitTestLoggerAcceptanceTests
    {
        private const string ExpectedTestCaseCount = "54";
        private const string ExpectedTestCasePassedCount = "26";

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
            DotnetTestFixture.Execute("test-results.xml");
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
            Assert.AreEqual(ExpectedTestCaseCount, node.Attribute(XName.Get("testcasecount")).Value);
            Assert.AreEqual(ExpectedTestCasePassedCount, node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("skipped")).Value);
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
            Assert.AreEqual(ExpectedTestCaseCount, node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual(ExpectedTestCasePassedCount, node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("NUnit.Xml.TestLogger.NetCore.Tests.dll", node.Attribute(XName.Get("name")).Value);
            Assert.AreEqual(DotnetTestFixture.TestAssembly, node.Attribute(XName.Get("fullname")).Value);

            var startTimeStr = node.Attribute(XName.Get("start-time"))?.Value;
            var endTimeStr = node.Attribute(XName.Get("end-time"))?.Value;
            Assert.IsNotNull(startTimeStr);
            Assert.IsNotNull(endTimeStr);

            var startTime = Convert.ToDateTime(startTimeStr);
            var endTime = Convert.ToDateTime(endTimeStr);
            Assert.IsTrue(startTime < endTime, "test suite start time should be before end time");
        }

        [TestMethod]
        public void TestResultFileShouldContainNamespaceTestSuiteForNetFull()
        {
            // Two namespaces in test asset are:
            // NUnit.Xml.TestLogger.NetFull.Tests and NUnit.Xml.TestLogger.Tests2
            var query = string.Format("/test-run//test-suite[@type='TestSuite' and @name='NetFull']");
            var node = this.resultsXml.XPathSelectElement(query);

            Assert.IsNotNull(node);
            Assert.AreEqual("30", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("16", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("7", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("3", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("4", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("NUnit.Xml.TestLogger.NetFull", node.Attribute(XName.Get("fullname")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainNamespaceTestSuiteForTests2()
        {
            // Two namespaces in test asset are:
            // NUnit.Xml.TestLogger.NetFull.Tests and NUnit.Xml.TestLogger.Tests2
            var query = "/test-run//test-suite[@type='TestSuite' and @name='Tests2']";
            var node = this.resultsXml.XPathSelectElement(query);

            Assert.IsNotNull(node);
            Assert.AreEqual("24", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("10", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("7", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("3", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("4", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("NUnit.Xml.TestLogger.Tests2", node.Attribute(XName.Get("fullname")).Value);
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
                var query = $"/test-run//test-suite[@type='TestSuite' and @name='{part}']";
                var node = this.resultsXml.XPathSelectElement(query);
                fullName = fullName == string.Empty ? part : fullName + "." + part;

                Assert.IsNotNull(node);
                Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
                Assert.AreEqual(fullName, node.Attribute(XName.Get("fullname")).Value);
            }
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCasePropertiesForTestWithPropertyAttributes()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.WithProperty']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(1, propertiesElement.Descendants().Count());

            var propertyElement = propertiesElement.Element("property");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);
            Assert.AreEqual("Property value", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        [DataRow("NUnit.Xml.TestLogger.Tests2")]
        public void TestResultFileTestCasesShouldContainValidStartAndEndTimes(string testNamespace)
        {
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.PassTest11']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var startTimeStr = testCaseElement.Attribute(XName.Get("start-time"))?.Value;
            var endTimeStr = testCaseElement.Attribute(XName.Get("end-time"))?.Value;
            Assert.IsNotNull(startTimeStr);
            Assert.IsNotNull(endTimeStr);

            string dateFormat = "yyyy-MM-ddT HH:mm:ssZ";
            var startTime = DateTime.ParseExact(startTimeStr, dateFormat, CultureInfo.InvariantCulture);
            var endTime = DateTime.ParseExact(endTimeStr, dateFormat, CultureInfo.InvariantCulture);

            // Assert that start and end times are in the right order and they are recent (i.e. not default DateTime values)
            Assert.IsTrue(startTime < endTime, "test case start time should be before end time");
            var timeDiff = (DateTime.UtcNow - startTime.ToUniversalTime()).Duration();
            Assert.IsTrue(timeDiff < TimeSpan.FromMinutes(1), "test case start time should not be too far in the past, difference was {0}", timeDiff);
        }

        [TestMethod]
        public void TestResultFileTestCasesShouldContainSeed()
        {
            var query = "/test-run//test-case[@fullname='NUnit.Xml.TestLogger.Tests2.RandomizerTests.Sort_RandomData_IsSorted']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var seedValue = testCaseElement.Attribute(XName.Get("seed"))?.Value;
            Assert.IsNotNull(seedValue);
        }

        [TestMethod]
        public void TestResultFileShouldNotContainTestCasePropertiesForTestWithNoPropertyAttributes()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.NoProperty']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNull(propertiesElement, "properties element");
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCaseCategoryForTestWithCategory()
        {
            var query = "/test-run//test-case[@fullname='NUnit.Xml.TestLogger.Tests2.UnitTest1.PassTest11']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(1, propertiesElement.Descendants().Count());

            var propertyElement = propertiesElement.Element("property");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Description", propertyElement.Attribute("name")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCaseCategoryForTestWithDescription()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.WithCategory']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(1, propertiesElement.Descendants().Count());

            var propertyElement = propertiesElement.Element("property");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Category", propertyElement.Attribute("name")?.Value);
            Assert.AreEqual("Nunit Test Category", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldNotContainTestCaseCategoryForTestWithMultipleCategory()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.MultipleCategories']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(2, propertiesElement.Descendants().Count());

            // Verify first category
            var propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Category2']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Category", propertyElement.Attribute("name")?.Value);

            // Verify second category
            propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Category1']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Category", propertyElement.Attribute("name")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCaseCategoryAndPropertyForTestWithMultipleProperties()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.WithCategoryAndProperty']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(2, propertiesElement.Descendants().Count());

            // Verify first category
            var propertyElement = propertiesElement.XPathSelectElement("descendant::property[@name='Category']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("NUnit Test Category", propertyElement.Attribute("value")?.Value);

            // Verify second property
            propertyElement = propertiesElement.XPathSelectElement("descendant::property[@name='Property name']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property value", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCasePropertyForTestWithMultipleProperties()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.UnitTest1.WithProperties']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(2, propertiesElement.Descendants().Count());

            // Verify first category
            var propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Property value 1']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);

            // Verify second property
            propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Property value 2']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCaseAttachments()
        {
            var testNamespace = "NUnit.Xml.TestLogger.NetFull.Tests";
            var query = $"/test-run//test-case[@fullname='{testNamespace}.AttachmentTest.TestAddAttachment']";
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var attachmentsElement = testCaseElement.Element("attachments");
            Assert.IsNotNull(attachmentsElement, "attachments element");
            Assert.AreEqual(3, attachmentsElement.Descendants().Count());

            var attachmentElement = attachmentsElement.Descendants().First();
            Assert.IsNotNull(attachmentElement, "attachment element");
            StringAssert.Contains(attachmentElement.Descendants().First().Value, "x.txt");
            StringAssert.Contains(attachmentElement.Descendants().Last().Value, "description");
        }
    }
}
