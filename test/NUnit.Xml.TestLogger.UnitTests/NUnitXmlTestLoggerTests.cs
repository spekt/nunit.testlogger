namespace NUnit.Xml.TestLogger.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using TestSuite = Microsoft.VisualStudio.TestPlatform.Extension.NUnit.Xml.TestLogger.NUnitXmlTestLogger.TestSuite;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Extension.NUnit.Xml.TestLogger;
    using System.Linq;

    [TestClass]
    public class NUnitXmlTestLoggerTests
    {
        private string dummyTestResultsDirectory = "/tmp/testresults";

        [TestMethod]
        public void InitializeShouldThrowIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new NUnitXmlTestLogger().Initialize(null, dummyTestResultsDirectory));
        }

        [TestMethod]
        public void CreateTestSuiteShouldReturnEmptyGroupsIfTestSuitesAreExclusive()
        {
            var suite1 = CreateTestSuite("a.b");
            var suite2 = CreateTestSuite("c.d");

            var result = NUnitXmlTestLogger.GroupTestSuites(new[] { suite1, suite2 }).ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("a", result[0].Name);
            Assert.AreEqual("c", result[1].Name);
        }

        [TestMethod]
        public void CreateTestSuiteShouldGroupTestSuitesByName()
        {
            var suites = new[] { CreateTestSuite("a.b.c"), CreateTestSuite("a.b.e"), CreateTestSuite("c.d") };
            var expectedXmlForA = @"<test-suite type=""TestSuite"" name=""a"" fullname=""a"" total=""10"" passed=""2"" failed=""2"" inconclusive=""2"" skipped=""2"" result=""Failed"" duration=""0"">
  <test-suite type=""TestSuite"" name=""b"" fullname=""a.b"" total=""10"" passed=""2"" failed=""2"" inconclusive=""2"" skipped=""2"" result=""Failed"" duration=""0"">
    <test-suite />
    <test-suite />
  </test-suite>
</test-suite>";
            var expectedXmlForC = @"<test-suite type=""TestSuite"" name=""c"" fullname=""c"" total=""5"" passed=""1"" failed=""1"" inconclusive=""1"" skipped=""1"" result=""Failed"" duration=""0"">
  <test-suite />
</test-suite>";

            var result = NUnitXmlTestLogger.GroupTestSuites(suites).ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("c", result[0].Name);
            Assert.AreEqual(expectedXmlForC, result[0].Element.ToString());
            Assert.AreEqual("a", result[1].Name);
            Assert.AreEqual(expectedXmlForA, result[1].Element.ToString());
        }

        private static string CreateTestSuiteXml(string name, string multiplier)
        {
            // return "<test-suite type=\"TestSuite\" id=\"0 - 1042\" name=\"NUnit\" fullname=\"NUnit\" runstate=\"Runnable\" testcasecount=\"27\" result=\"Failed\" site=\"Child\" start-time=\"2018 - 10 - 30 01:06:28Z\" end-time=\"2018 - 10 - 30 01:06:29Z\" duration=\"0.968269\" total=\"26\" passed=\"10\" failed=\"8\" warnings=\"1\" inconclusive=\"4\" skipped=\"4\" asserts=\"10\">";
            return string.Empty;
        }

        private static TestSuite CreateTestSuite(string name)
        {
            return new TestSuite
            {
                Element = new XElement("test-suite"),
                Name = "n",
                FullName = name,
                Total = 5,
                Passed = 1,
                Failed = 1,
                Inconclusive = 1,
                Skipped = 1,
                Error = 1
            };
        }
    }
}
