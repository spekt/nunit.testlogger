namespace Microsoft.VisualStudio.TestPlatform.Extension.NUnit.Xml.TestLogger
{
    using System;
    using System.Collections.Generic;
    using ObjectModel;

    public class TestResultInfo
    {
        private readonly TestResult result;
        public TestCase TestCase => result.TestCase;
        public TestOutcome Outcome => result.Outcome;
        public string AssemblyPath => result.TestCase.Source;
        public readonly string Type;
        public readonly string Method;
        public string Name => result.TestCase.DisplayName;
        public TimeSpan Duration => result.Duration;
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
                TestResultInfo objectToCompare = (TestResultInfo)obj;
                if (string.Compare(ErrorMessage, objectToCompare.ErrorMessage) == 0
                    && string.Compare(ErrorStackTrace, objectToCompare.ErrorStackTrace) == 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}