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
                                    var name = GetFirstPartOf(r.FullName);
                                    if (string.IsNullOrEmpty(name))
                                    {
                                        roots.Add(r);
                                    }

                                    return name;
                                })
                                .OrderBy(g => g.Key)
                                .Where(g => !string.IsNullOrEmpty(g.Key))
                                .Select(CreateTestSuite)
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

        private static XElement CreateErrorElement(TestResultInfo result)
        {
            string errorMessage = result.ErrorMessage;

            int indexOfErrorType = errorMessage.IndexOf('(');
            errorMessage = errorMessage.Substring(indexOfErrorType + 1);

            int indexOfName = errorMessage.IndexOf(')');
            string name = errorMessage.Substring(0, indexOfName);
            errorMessage = errorMessage.Substring(indexOfName + 4);

            int indexOfExceptionType = errorMessage.IndexOf(':');
            string exceptionType = errorMessage.Substring(0, indexOfExceptionType - 1);

            XElement errorElement = new XElement("error");
            errorElement.SetAttributeValue("name", name);

            errorElement.Add(CreateFailureElement(exceptionType, errorMessage, result.ErrorStackTrace));

            return errorElement;
        }

        private static XElement CreateFailureElement(string exceptionType, string message, string stackTrace)
        {
            XElement failureElement = new XElement("failure", new XAttribute("exception-type", exceptionType));
            failureElement.Add(new XElement("message", RemoveInvalidXmlChar(message)));
            failureElement.Add(new XElement("stack-trace", RemoveInvalidXmlChar(stackTrace)));

            return failureElement;
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
            var name = resultsByType.Key;
            var idx = name.LastIndexOf('.');
            if (idx != -1)
            {
                name = name.Substring(idx + 1);
            }

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

        private static string GetFirstPartOf(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var idx = name.LastIndexOf(".");
            if (idx != -1)
            {
                return name.Substring(0, idx);
            }

            return string.Empty;
        }

        private static TestSuite CreateTestSuite(IGrouping<string, TestSuite> suites)
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

            // Create test-suite element for the TestSuite
            var fullName = suites.Key;
            var name = fullName;
            var idx = fullName.LastIndexOf('.');
            if (idx != -1)
            {
                name = fullName.Substring(idx + 1);
            }

            element.SetAttributeValue("type", "TestSuite");
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
                new XAttribute("asserts", 0));

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
                    new XElement("message", RemoveInvalidXmlChar(result.ErrorMessage)),
                    new XElement("stack-trace", RemoveInvalidXmlChar(result.ErrorStackTrace))));
            }

            return element;
        }

        private static bool TryParseName(string testCaseName, out string metadataTypeName, out string metadataMethodName, out string metadataMethodArguments)
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

        private static string RemoveInvalidXmlChar(string str)
        {
            if (str != null)
            {
                // From xml spec (http://www.w3.org/TR/xml/#charsets) valid chars:
                // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]

                // we are handling only #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
                // because C# support unicode character in range \u0000 to \uFFFF
                MatchEvaluator evaluator = new MatchEvaluator(ReplaceInvalidCharacterWithUniCodeEscapeSequence);
                string invalidChar = @"[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD]";
                return Regex.Replace(str, invalidChar, evaluator);
            }

            return str;
        }

        private static string ReplaceInvalidCharacterWithUniCodeEscapeSequence(Match match)
        {
            char x = match.Value[0];
            return string.Format(@"\u{0:x4}", (ushort)x);
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

            int total = 0;
            int passed = 0;
            int failed = 0;
            int skipped = 0;
            int inconclusive = 0;
            int errors = 0;
            var time = TimeSpan.Zero;

            var element = new XElement("test-suite");
            element.SetAttributeValue("type", "Assembly");

            XElement errorsElement = new XElement("errors");
            element.Add(errorsElement);

            foreach (var suite in fixtureGroups)
            {
                total += suite.Total;
                passed += suite.Passed;
                failed += suite.Failed;
                inconclusive += suite.Inconclusive;
                skipped += suite.Skipped;
                errors += suite.Error;
                time += suite.Time;

                element.Add(suite.Element);
            }

            element.SetAttributeValue("name", Path.GetFileName(assemblyPath));
            element.SetAttributeValue("fullname", assemblyPath);

            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", passed);
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("inconclusive", inconclusive);
            element.SetAttributeValue("skipped", skipped);
            element.SetAttributeValue("duration", time.TotalSeconds);
            element.SetAttributeValue("errors", errors);
            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);

            return element;
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
