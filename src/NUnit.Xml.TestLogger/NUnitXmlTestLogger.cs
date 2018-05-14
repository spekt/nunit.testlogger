namespace Microsoft.VisualStudio.TestPlatform.Extension.NUnit.Xml.TestLogger
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using ObjectModel;
    using ObjectModel.Client;
    using ObjectModel.Logging;

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

        public const string AppendTimeStamp = "AppendTimeStamp";

        private const string ResultStatusPassed = "Passed";
        private const string ResultStatusFailed = "Failed";

        private string outputFilePath;

        private readonly object resultsGuard = new object();
        private List<TestResultInfo> results;
        private DateTime localStartTime;

        private class TestResultInfo
        {
            private readonly TestResult result;
            public TestCase TestCase => result.TestCase;
            public TestOutcome Outcome => result.Outcome;
            public string AssemblyPath => result.TestCase.Source;
            public readonly string Type;
            public readonly string Method;
            public string Name => result.TestCase.DisplayName;
            public TimeSpan Time => result.Duration;
            public string ErrorMessage => result.ErrorMessage;
            public string ErrorStackTrace => result.ErrorStackTrace;
            public IReadOnlyCollection<TestResultMessage> Messages => result.Messages;
            public TraitCollection Traits => result.Traits;

            public TestResultInfo(
                TestResult result,
                string type,
                string method)
            {
                this.result = result;
                Type = type;
                Method = method;
            }

            public override int GetHashCode()
            {
                return result.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is TestResultInfo)
                {
                    TestResultInfo objectToCompare = (TestResultInfo) obj;
                    if (string.Compare(ErrorMessage, objectToCompare.ErrorMessage) == 0
                        && string.Compare(ErrorStackTrace, objectToCompare.ErrorStackTrace) == 0)
                    {
                        return true;
                    }
                }

                return false;
            }
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
            InitializeImpl(events, outputPath);
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
                if (parameters.TryGetValue(AppendTimeStamp, out appendTimeStamp))
                {
                    outputPath = AppendTimestampToXmlFile(outputPath);
                }
                InitializeImpl(events, outputPath);
            }
            else if (parameters.TryGetValue(DefaultLoggerParameterNames.TestRunDirectory, out string outputDir))
            {
                Initialize(events, outputDir);
            }
            else
            {
                throw new ArgumentException($"Expected {LogFilePathKey} or {DefaultLoggerParameterNames.TestRunDirectory} parameter", nameof(parameters));
            }
        }

        private string AppendTimestampToXmlFile(string filePath)
        {
            // Append HH:mm:ss:ms to outputFilePath to ensure unique output filename
            // This is to prevent the output file from getting overwritten in the case of tests being ran for multiple target frameworks
            var filePathNoExtension = Path.ChangeExtension(filePath, null);
            return $"{filePathNoExtension}{DateTime.Now.ToString("HH:mm:ss:ms")}.xml";
        }

        private void InitializeImpl(TestLoggerEvents events, string outputPath)
        {
            events.TestRunMessage += TestMessageHandler;
            events.TestResult += TestResultHandler;
            events.TestRunComplete += TestRunCompleteHandler;

            outputFilePath = Path.GetFullPath(outputPath);

            lock (resultsGuard)
            {
                results = new List<TestResultInfo>();
            }

            localStartTime = DateTime.UtcNow;
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
                lock (resultsGuard)
                {
                    results.Add(new TestResultInfo(
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
            lock (resultsGuard)
            {
                resultList = results;
                results = new List<TestResultInfo>();
            }

            var doc = new XDocument(CreateAssembliesElement(resultList));

            // Create directory if not exist
            var loggerFileDirPath = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(loggerFileDirPath))
            {
                Directory.CreateDirectory(loggerFileDirPath);
            }

            using (var f = File.Create(outputFilePath))
            {
                doc.Save(f);
            }

            var resultsFileMessage = string.Format(CultureInfo.CurrentCulture, "Results File: {0}", outputFilePath);
            Console.WriteLine(resultsFileMessage);
        }

        private XElement CreateAssembliesElement(List<TestResultInfo> results)
        {
            var testSuites = from result in results
                group result by result.AssemblyPath
                into resultsByAssembly
                orderby resultsByAssembly.Key
                select CreateTestSuiteElement(resultsByAssembly);

            var element = new XElement("test-run", testSuites);

            element.SetAttributeValue("id", 2);

            element.SetAttributeValue("duration", results.Sum(x => x.Time.TotalSeconds));

            var total = testSuites.Sum(x => (int) x.Attribute("total"));

            // TODO test case count is actually count before filtering
            element.SetAttributeValue("testcasecount", total);
            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", testSuites.Sum(x => (int) x.Attribute("passed")));

            var failed = testSuites.Sum(x => (int) x.Attribute("failed"));
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("skipped", testSuites.Sum(x => (int) x.Attribute("skipped")));

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);

            const string dateFormat = "yyyy-MM-ddT HH:mm:ssZ";
            element.SetAttributeValue("start-time", localStartTime.ToString(dateFormat, CultureInfo.InvariantCulture));
            element.SetAttributeValue("end-time", DateTime.UtcNow.ToString(dateFormat, CultureInfo.InvariantCulture));

            return element;
        }

        private XElement CreateTestSuiteElement(IGrouping<string, TestResultInfo> resultsByAssembly)
        {
            var assemblyPath = resultsByAssembly.Key;

            var collections = from resultsInAssembly in resultsByAssembly
                group resultsInAssembly by resultsInAssembly.Type
                into resultsByType
                orderby resultsByType.Key
                select CreateTestSuite(resultsByType);

            int total = 0;
            int passed = 0;
            int failed = 0;
            int skipped = 0;
            int errors = 0;
            var time = TimeSpan.Zero;

            var element = new XElement("test-suite");
            element.SetAttributeValue("type", "Assembly");

            XElement errorsElement = new XElement("errors");
            element.Add(errorsElement);

            foreach (var collection in collections)
            {
                total += collection.total;
                passed += collection.passed;
                failed += collection.failed;
                skipped += collection.skipped;
                errors += collection.error;
                time += collection.time;

                element.Add(collection.element);
            }

            element.SetAttributeValue("name", Path.GetFileName(assemblyPath));
            element.SetAttributeValue("fullname", assemblyPath);

            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", passed);
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("skipped", skipped);
            element.SetAttributeValue("duration", time.TotalSeconds);
            element.SetAttributeValue("errors", errors);
            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);

            return element;
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

        private static (XElement element, int total, int passed, int failed, int skipped, int error, TimeSpan time) CreateTestSuite(IGrouping<string, TestResultInfo> resultsByType
        )
        {
            var element = new XElement("test-suite");

            int total = 0;
            int passed = 0;
            int failed = 0;
            int skipped = 0;
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
                    case TestOutcome.None:
                        skipped++;
                        break;
                }

                total++;
                time += result.Time;

                element.Add(CreateTestElement(result));
            }

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
            element.SetAttributeValue("skipped", skipped);

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);
            element.SetAttributeValue("result", resultString);
            element.SetAttributeValue("duration", time.TotalSeconds);

            return (element, total, passed, failed, skipped, error, time);
        }

        private static XElement CreateTestElement(TestResultInfo result)
        {
            var element = new XElement("test-case",
                new XAttribute("name", result.Name),
                new XAttribute("fullname", result.Type + "." + result.Method),
                new XAttribute("result", OutcomeToString(result.Outcome)),
                new XAttribute("duration", result.Time.TotalSeconds),
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
                element.Add(new XElement("failure",
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

            if (typeNameLength <= 0 || methodNameStart == typeAndMethodName.Length) // No typeName is available
            {
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
            return string.Format(@"\u{0:x4}", (ushort) x);
        }
    }
}