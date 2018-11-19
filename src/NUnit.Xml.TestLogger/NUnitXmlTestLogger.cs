// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extension.NUnit.Xml.TestLogger
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    [FriendlyName(FriendlyName)]
    [ExtensionUri(ExtensionUri)]
    public class NUnitXmlTestLogger : ITestLoggerWithParameters
    {
        /// <summary>
        /// Uri used to uniquely identify the logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/NUnitXmlLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "nunit";

        public const string LogFilePathKey = "LogFilePath";

        private const string ResultStatusPassed = "Passed";
        private const string ResultStatusFailed = "Failed";

        private const string DateFormat = "yyyy-MM-ddT HH:mm:ssZ";
        private readonly object resultsGuard = new object();
        private string outputFilePath;

        private List<TestResultInfo> results;
        private DateTime localStartTime;

        public static IEnumerable<TestSuite> GroupTestSuites(IEnumerable<TestSuite> suites)
        {
            var groups = suites;
            var roots = new List<TestSuite>();
            while (groups.Any())
            {
                groups = groups.GroupBy(r =>
                                {
                                    var name = r.FullName.SubstringBeforeDot();
                                    if (string.IsNullOrEmpty(name))
                                    {
                                        roots.Add(r);
                                    }

                                    return name;
                                })
                                .OrderBy(g => g.Key)
                                .Where(g => !string.IsNullOrEmpty(g.Key))
                                .Select(g => AggregateTestSuites(g, "TestSuite", g.Key.SubstringAfterDot(), g.Key))
                                .ToList();
            }

            return roots;
        }

        public void Initialize(TestLoggerEvents events, string testResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (testResultsDirPath == null)
            {
                throw new ArgumentNullException(nameof(testResultsDirPath));
            }

            var outputPath = Path.Combine(testResultsDirPath, "TestResults.xml");
            this.InitializeImpl(events, outputPath);
        }

        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.TryGetValue(LogFilePathKey, out string outputPath))
            {
                this.InitializeImpl(events, outputPath);
            }
            else if (parameters.TryGetValue(DefaultLoggerParameterNames.TestRunDirectory, out string outputDir))
            {
                this.Initialize(events, outputDir);
            }
            else
            {
                throw new ArgumentException($"Expected {LogFilePathKey} or {DefaultLoggerParameterNames.TestRunDirectory} parameter", nameof(parameters));
            }
        }

        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when a test result is received.
        /// </summary>
        internal void TestResultHandler(object sender, TestResultEventArgs e)
        {
            TestResult result = e.Result;

            if (TryParseName(result.TestCase.FullyQualifiedName, out var typeName, out var methodName, out _))
            {
                lock (this.resultsGuard)
                {
                    this.results.Add(new TestResultInfo(
                        result,
                        typeName,
                        methodName));
                }
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            List<TestResultInfo> resultList;
            lock (this.resultsGuard)
            {
                resultList = this.results;
                this.results = new List<TestResultInfo>();
            }

            var doc = new XDocument(this.CreateTestRunElement(resultList));

            // Create directory if not exist
            var loggerFileDirPath = Path.GetDirectoryName(this.outputFilePath);
            if (!Directory.Exists(loggerFileDirPath))
            {
                Directory.CreateDirectory(loggerFileDirPath);
            }

            using (var f = File.Create(this.outputFilePath))
            {
                doc.Save(f);
            }

            var resultsFileMessage = string.Format(CultureInfo.CurrentCulture, "Results File: {0}", this.outputFilePath);
            Console.WriteLine(resultsFileMessage);
        }

        private static TestSuite AggregateTestSuites(
            IEnumerable<TestSuite> suites,
            string testSuiteType,
            string name,
            string fullName)
        {
            var element = new XElement("test-suite");

            int total = 0;
            int passed = 0;
            int failed = 0;
            int skipped = 0;
            int inconclusive = 0;
            int error = 0;
            var time = TimeSpan.Zero;

            foreach (var result in suites)
            {
                total += result.Total;
                passed += result.Passed;
                failed += result.Failed;
                skipped += result.Skipped;
                inconclusive += result.Inconclusive;
                error += result.Error;
                time += result.Time;

                element.Add(result.Element);
            }

            element.SetAttributeValue("type", testSuiteType);
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("fullname", fullName);
            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", passed);
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("inconclusive", inconclusive);
            element.SetAttributeValue("skipped", skipped);

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);
            element.SetAttributeValue("duration", time.TotalSeconds);

            return new TestSuite
            {
                Element = element,
                Name = name,
                FullName = fullName,
                Total = total,
                Passed = passed,
                Failed = failed,
                Inconclusive = inconclusive,
                Skipped = skipped,
                Error = error,
                Time = time
            };
        }

        private static TestSuite CreateFixture(IGrouping<string, TestResultInfo> resultsByType)
        {
            var element = new XElement("test-suite");

            int total = 0;
            int passed = 0;
            int failed = 0;
            int skipped = 0;
            int inconclusive = 0;
            int error = 0;
            var time = TimeSpan.Zero;

            foreach (var result in resultsByType)
            {
                switch (result.Outcome)
                {
                    case TestOutcome.Failed:
                        failed++;
                        break;

                    case TestOutcome.Passed:
                        passed++;
                        break;

                    case TestOutcome.Skipped:
                        skipped++;
                        break;
                    case TestOutcome.None:
                        inconclusive++;
                        break;
                }

                total++;
                time += result.Duration;

                // Create test-case elements
                element.Add(CreateTestCaseElement(result));
            }

            // Create test-suite element for the TestFixture
            var name = resultsByType.Key.SubstringAfterDot();

            element.SetAttributeValue("type", "TestFixture");
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("fullname", resultsByType.Key);

            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", passed);
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("inconclusive", inconclusive);
            element.SetAttributeValue("skipped", skipped);

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);
            element.SetAttributeValue("duration", time.TotalSeconds);

            return new TestSuite
            {
                Element = element,
                Name = name,
                FullName = resultsByType.Key,
                Total = total,
                Passed = passed,
                Failed = failed,
                Inconclusive = inconclusive,
                Skipped = skipped,
                Error = error,
                Time = time
            };
        }

        private static XElement CreateTestCaseElement(TestResultInfo result)
        {
            var element = new XElement(
                "test-case",
                new XAttribute("name", result.Name),
                new XAttribute("fullname", result.Type + "." + result.Method),
                new XAttribute("methodname", result.Method),
                new XAttribute("classname", result.Type),
                new XAttribute("result", OutcomeToString(result.Outcome)),
                new XAttribute("duration", result.Duration.TotalSeconds),
                new XAttribute("asserts", 0),
                CreatePropertiesElement(result.TestCase));

            StringBuilder stdOut = new StringBuilder();
            foreach (var m in result.Messages)
            {
                if (TestResultMessage.StandardOutCategory.Equals(m.Category, StringComparison.OrdinalIgnoreCase))
                {
                    stdOut.AppendLine(m.Text);
                }
            }

            if (!string.IsNullOrWhiteSpace(stdOut.ToString()))
            {
                element.Add(new XElement("output", new XCData(stdOut.ToString())));
            }

            if (result.Outcome == TestOutcome.Failed)
            {
                element.Add(new XElement(
                    "failure",
                    new XElement("message", result.ErrorMessage.ReplaceInvalidXmlChar()),
                    new XElement("stack-trace", result.ErrorStackTrace.ReplaceInvalidXmlChar())));
            }

            return element;
        }

        private static XElement CreatePropertiesElement(TestCase result)
        {
            var traits = result.Traits.Select(CreatePropertyElement).ToList();
#pragma warning disable CS0618 // Type or member is obsolete

            var newTraits = result.Properties.Where(t => t.Attributes.HasFlag(TestPropertyAttributes.Trait));

            foreach (var p in newTraits)
            {
                var propValue = result.GetPropertyValue(p);

                if (propValue is KeyValuePair<string, string>[])
                {
                    var keyValuePairs = (KeyValuePair<string, string>[])propValue;

                    foreach (var kvp in keyValuePairs)
                    {
                        traits.AddRange(CreatePropertyElement(kvp.Key, kvp.Value));
                    }
                }
                else if (p.Id == "NUnit.TestCategory")
                {
                    traits.AddRange(CreatePropertyElement("Category", (string[])propValue));
                }
            }

#pragma warning restore CS0618 // Type or member is obsolete
            var propertyElements = traits;
            return traits.Any()
                ? new XElement("properties", propertyElements.Distinct())
                : null;
        }

        private static XElement CreatePropertyElement(Trait trait)
        {
            return CreatePropertyElement(trait.Name, trait.Value).Single();
        }

        private static IEnumerable<XElement> CreatePropertyElement(string name, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("message", nameof(name));
            }

            foreach (var value in values)
            {
                yield return new XElement(
                "property",
                new XAttribute("name", name),
                new XAttribute("value", value));
            }
        }

        private static bool TryParseName(
            string testCaseName,
            out string metadataTypeName,
            out string metadataMethodName,
            out string metadataMethodArguments)
        {
            // This is fragile. The FQN is constructed by a test adapter.
            // There is no enforcement that the FQN starts with metadata type name.
            string typeAndMethodName;
            var methodArgumentsStart = testCaseName.IndexOf('(');

            if (methodArgumentsStart == -1)
            {
                typeAndMethodName = testCaseName.Trim();
                metadataMethodArguments = string.Empty;
            }
            else
            {
                typeAndMethodName = testCaseName.Substring(0, methodArgumentsStart).Trim();
                metadataMethodArguments = testCaseName.Substring(methodArgumentsStart).Trim();

                if (metadataMethodArguments[metadataMethodArguments.Length - 1] != ')')
                {
                    metadataTypeName = null;
                    metadataMethodName = null;
                    metadataMethodArguments = null;
                    return false;
                }
            }

            var typeNameLength = typeAndMethodName.LastIndexOf('.');
            var methodNameStart = typeNameLength + 1;

            if (typeNameLength <= 0 || methodNameStart == typeAndMethodName.Length)
            {
                // No typeName is available
                metadataTypeName = null;
                metadataMethodName = null;
                metadataMethodArguments = null;
                return false;
            }

            metadataTypeName = typeAndMethodName.Substring(0, typeNameLength).Trim();
            metadataMethodName = typeAndMethodName.Substring(methodNameStart).Trim();
            return true;
        }

        private static string OutcomeToString(TestOutcome outcome)
        {
            switch (outcome)
            {
                case TestOutcome.Failed:
                    return ResultStatusFailed;

                case TestOutcome.Passed:
                    return "Passed";

                case TestOutcome.Skipped:
                    return "Skipped";

                default:
                    return "Inconclusive";
            }
        }

        private void InitializeImpl(TestLoggerEvents events, string outputPath)
        {
            events.TestRunMessage += this.TestMessageHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;

            this.outputFilePath = Path.GetFullPath(outputPath);

            lock (this.resultsGuard)
            {
                this.results = new List<TestResultInfo>();
            }

            this.localStartTime = DateTime.UtcNow;
        }

        private XElement CreateTestRunElement(List<TestResultInfo> results)
        {
            var testSuites = from result in results
                             group result by result.AssemblyPath
                             into resultsByAssembly
                             orderby resultsByAssembly.Key
                             select this.CreateAssemblyElement(resultsByAssembly);

            var element = new XElement("test-run", testSuites);

            element.SetAttributeValue("id", 2);

            element.SetAttributeValue("duration", results.Sum(x => x.Duration.TotalSeconds));

            var total = testSuites.Sum(x => (int)x.Attribute("total"));

            // TODO test case count is actually count before filtering
            element.SetAttributeValue("testcasecount", total);
            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", testSuites.Sum(x => (int)x.Attribute("passed")));

            var failed = testSuites.Sum(x => (int)x.Attribute("failed"));
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("inconclusive", testSuites.Sum(x => (int)x.Attribute("inconclusive")));
            element.SetAttributeValue("skipped", testSuites.Sum(x => (int)x.Attribute("skipped")));

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);

            element.SetAttributeValue("start-time", this.localStartTime.ToString(DateFormat, CultureInfo.InvariantCulture));
            element.SetAttributeValue("end-time", DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture));

            return element;
        }

        private XElement CreateAssemblyElement(IGrouping<string, TestResultInfo> resultsByAssembly)
        {
            var assemblyPath = resultsByAssembly.Key;
            var fixtures = from resultsInAssembly in resultsByAssembly
                           group resultsInAssembly by resultsInAssembly.Type
                           into resultsByType
                           orderby resultsByType.Key
                           select CreateFixture(resultsByType);
            var fixtureGroups = GroupTestSuites(fixtures);
            var suite = AggregateTestSuites(
                fixtureGroups,
                "Assembly",
                Path.GetFileName(assemblyPath),
                assemblyPath);

            XElement errorsElement = new XElement("errors");
            suite.Element.Add(errorsElement);

            return suite.Element;
        }

        public class TestSuite
        {
            public XElement Element { get; set; }

            public string Name { get; set; }

            public string FullName { get; set; }

            public int Total { get; set; }

            public int Passed { get; set; }

            public int Failed { get; set; }

            public int Inconclusive { get; set; }

            public int Skipped { get; set; }

            public int Error { get; set; }

            public TimeSpan Time { get; set; }
        }
    }
}
