
namespace Microsoft.VisualStudio.TestPlatform.Extensions.Appveyor.TestLogger
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    [FriendlyName(AppveyorLogger.FriendlyName)]
    [ExtensionUri(AppveyorLogger.ExtensionUri)]
    class AppveyorLogger : ITestLogger
    {
        /// <summary>
        /// Uri used to uniquely identify the Appveyor logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/AppveyorLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the Appveyor logger.
        /// </summary>
        public const string FriendlyName = "Appveyor";

        /// <summary>
        /// Initializes the Test Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDirectory">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            this.NotNull(events, nameof(events));

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
            this.NotNull(sender, "sender");
            this.NotNull(e, "e");

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
            var allArgs = new List<string>();
            string name = e.Result.TestCase.DisplayName;
            string filename = string.IsNullOrEmpty(e.Result.TestCase.Source) ? string.Empty : Path.GetFileName(e.Result.TestCase.Source);
            string outcome = e.Result.Outcome.ToString();

            allArgs.Add("-Name " + name);
            allArgs.Add("-Framework " + "MSTest");
            if (!string.IsNullOrEmpty(filename))
            {
                allArgs.Add("-FileName " + this.AddDoubleQuotes(filename));
            }
            allArgs.Add("-outcome " + outcome);

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


                allArgs.Add("-Duration " + duration);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    allArgs.Add("-ErrorMessage " + this.AddDoubleQuotes(errorMessage));
                }
                if (!string.IsNullOrEmpty(errorStackTrace))
                {
                    allArgs.Add("-ErrorStackTrace " + this.AddDoubleQuotes(errorStackTrace));
                }
                if (!string.IsNullOrEmpty(stdOut.ToString()))
                {
                    allArgs.Add("-StdOut " + this.AddDoubleQuotes(stdOut.ToString()));
                }
                if (!string.IsNullOrEmpty(stdErr.ToString()))
                {
                    allArgs.Add("-StdErr " + this.AddDoubleQuotes(stdErr.ToString()));
                }
            }
            else
            {
                // Handle output type skip, NotFound and None
            }

            this.PublishTestResult(allArgs);
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
            // handle test run complete
        }

        private void PublishTestResult(IEnumerable<string> args)
        {
            string publishTestResultExe = "appveyor";
            string functionToCall = "AddTest";

            var processInfo = new ProcessStartInfo
            {
                FileName = publishTestResultExe,
                Arguments = functionToCall + " " + string.Join(" ", args),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var activeProcess = new Process { StartInfo = processInfo })
            {
                activeProcess.OutputDataReceived += (sender, arg) => Console.WriteLine(arg.Data);
                activeProcess.ErrorDataReceived += (sender, arg) => Console.WriteLine(arg.Data);

                activeProcess.Start();

                activeProcess.BeginOutputReadLine();
                activeProcess.BeginErrorReadLine();
                activeProcess.WaitForExit();
            }
        }

        private T NotNull<T>(T arg, string parameterName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return arg;
        }

        private string AddDoubleQuotes(string x)
        {
            return "\"" + x + "\"";
        }
    }
}
