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

    /// Code used below authored by Nick Adcock
    /// <see cref="https://github.com/bango/DotnetCore.TeamCityLogger"/>

    [FriendlyName(FriendlyName)]
    [ExtensionUri(ExtensionUri)]
    public class TeamCityTestLogger : ITestLogger
    {
        private const string StartingMessagePattern = @"^\[.+\]\s+Starting:\s+(.+)$";
        private Stack<string> _suiteNames;

        /// <summary>
        /// Uri used to uniquely identify the logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/TeamCityLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "teamcity";

        public const string LogFilePathKey = "LogFilePath";

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

            _suiteNames = new Stack<string>();
            InitializeImpl(events);
        }

        private void InitializeImpl(TestLoggerEvents events)
        {
            events.TestRunMessage += TestMessageHandler;
            events.TestResult += TestResultHandler;
            events.TestRunComplete += TestRunCompleteHandler;
        }

        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            var match = Regex.Match(e.Message, StartingMessagePattern);
            if (!match.Success || match.Groups.Count != 2) return;

            var suiteName = match.Groups[1].Value;
            WriteServiceMessage($"testSuiteStarted name='{suiteName}'");
            _suiteNames.Push(suiteName);
        }

        internal void TestResultHandler(object sender, TestResultEventArgs e)
        {
            string testName = e.Result.TestCase.DisplayName;

            if (e.Result.Outcome == TestOutcome.Skipped)
            {
                WriteServiceMessage($"testIgnored name='{testName}'");
                return;
            }

            WriteServiceMessage($"testStarted name='{testName}'");

            foreach (var message in e.Result.Messages)
            {
                if (message.Category == TestResultMessage.StandardOutCategory)
                {
                    WriteServiceMessage($"testStdOut name='{testName}'] out='{message.Text}'");
                }
                else if (message.Category == TestResultMessage.StandardErrorCategory)
                {
                    WriteServiceMessage($"testStdErr name='{testName}'] out='{message.Text}'");
                }
            }

            if (e.Result.Outcome == TestOutcome.Failed)
            {
                WriteServiceMessage($"testFailed name='{testName}' message='{e.Result.ErrorMessage}' details='{e.Result.ErrorStackTrace}'");
            }

            WriteServiceMessage($"testFinished name='{testName}' duration='{e.Result.Duration.TotalMilliseconds}'");
        }

        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            if (_suiteNames.Count > 0)
            {
                WriteServiceMessage($"testSuiteFinished name='{_suiteNames.Pop()}'");
            }
        }

        private static void WriteServiceMessage(string message)
        {
            Console.WriteLine($"##teamcity[{message.Replace("\r\n", "\\r\\n").Replace("\n", "\\n")}]");
        }
    }
}