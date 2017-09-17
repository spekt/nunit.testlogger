namespace Microsoft.VisualStudio.TestPlatform.Extensions.Appveyor.TestLogger
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    [FriendlyName(AppveyorLogger.FriendlyName)]
    [ExtensionUri(AppveyorLogger.ExtensionUri)]
    public class AppveyorLogger : ITestLogger
    {
        /// <summary>
        /// Uri used to uniquely identify the Appveyor logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/AppveyorLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the Appveyor logger.
        /// </summary>
        public const string FriendlyName = "Appveyor";

        private AppveyorLoggerQueue queue;

        /// <summary>
        /// Initializes the Test Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDirectory">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            NotNull(events, nameof(events));

            string appveyorApiUrl = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");

            if (appveyorApiUrl == null)
            {
                Console.WriteLine("Appveyor.TestLogger: Not an AppVeyor run.  Environment variable 'APPVEYOR_API_URL' not set.");
                return;
            }

#if DEBUG
            Console.WriteLine("Appveyor.TestLogger: Logging to {0}", appveyorApiUrl);
#endif

            queue = new AppveyorLoggerQueue(appveyorApiUrl);

            // Register for the events.
            events.TestRunMessage += this.TestMessageHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;
        }

        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Event args
        /// </param>
        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            NotNull(sender, nameof(sender));
            NotNull(e, nameof(e));

            // Add code to handle message
        }

        /// <summary>
        /// Called when a test result is received.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The eventArgs.
        /// </param>
        private void TestResultHandler(object sender, TestResultEventArgs e)
        {
            string name = e.Result.TestCase.FullyQualifiedName;
            string filename = string.IsNullOrEmpty(e.Result.TestCase.Source) ? string.Empty : Path.GetFileName(e.Result.TestCase.Source);
            string outcome = e.Result.Outcome.ToString();

            var testResult = new Dictionary<string, string>();
            testResult.Add("testName", name);
            testResult.Add("testFramework", e.Result.TestCase.ExecutorUri.ToString());
            testResult.Add("outcome", outcome);

            if (!string.IsNullOrEmpty(filename))
            {
                testResult.Add("fileName", filename);
            }

            if (e.Result.Outcome == TestOutcome.Passed || e.Result.Outcome == TestOutcome.Failed)
            {
                int duration = Convert.ToInt32(e.Result.Duration.TotalMilliseconds);

                string errorMessage = e.Result.ErrorMessage;
                string errorStackTrace = e.Result.ErrorStackTrace;

                StringBuilder stdErr = new StringBuilder();
                StringBuilder stdOut = new StringBuilder();

                foreach (var m in e.Result.Messages)
                {
                    if (TestResultMessage.StandardOutCategory.Equals(m.Category, StringComparison.OrdinalIgnoreCase))
                    {
                        stdOut.AppendLine(m.Text);
                    }
                    else if (TestResultMessage.StandardErrorCategory.Equals(m.Category, StringComparison.OrdinalIgnoreCase))
                    {
                        stdErr.AppendLine(m.Text);
                    }
                }

                testResult.Add("durationMilliseconds", duration.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    testResult.Add("ErrorMessage", errorMessage);
                }
                if (!string.IsNullOrEmpty(errorStackTrace))
                {
                    testResult.Add("ErrorStackTrace", errorStackTrace);
                }
                if (!string.IsNullOrEmpty(stdOut.ToString()))
                {
                    testResult.Add("StdOut", stdOut.ToString());
                }
                if (!string.IsNullOrEmpty(stdErr.ToString()))
                {
                    testResult.Add("StdErr", stdErr.ToString());
                }
            }
            else
            {
                // Handle output type skip, NotFound and None
            }

            PublishTestResult(testResult);
        }


        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Test run complete events arguments.
        /// </param>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            queue.Flush();
        }

        private void PublishTestResult(Dictionary<string, string> testResult)
        {
            var jsonSb = new StringBuilder();
            jsonSb.Append("{");

            bool firstItem = true;
            foreach (var field in testResult)
            {
                if (!firstItem)
                {
                    jsonSb.Append(",");
                }
                firstItem = false;
                jsonSb.Append("\"" + field.Key + "\": ");
                JsonEscape.SerializeString(field.Value, jsonSb);
            }

            jsonSb.Append("}");

            queue.Enqueue(jsonSb.ToString());
        }

        private static T NotNull<T>(T arg, string parameterName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return arg;
        }
    }
}
