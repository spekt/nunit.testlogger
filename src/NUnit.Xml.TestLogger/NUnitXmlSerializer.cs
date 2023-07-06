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
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Spekt.TestLogger.Core;
    using Spekt.TestLogger.Utilities;

    public class NUnitXmlSerializer : ITestResultSerializer
    {
        private const string DateFormat = "yyyy-MM-ddT HH:mm:ssZ";
        private const string ResultStatusPassed = "Passed";
        private const string ResultStatusFailed = "Failed";

        public IInputSanitizer InputSanitizer { get;  } = new InputSanitizerXml();

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

        public string Serialize(
            LoggerConfiguration loggerConfiguration,
            TestRunConfiguration runConfiguration,
            List<TestResultInfo> results,
            List<TestMessageInfo> messages)
        {
            var doc = new XDocument(this.CreateTestRunElement(results, runConfiguration));
            return doc.ToString();
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
            DateTime? startTime = null;
            DateTime? endTime = null;

            foreach (var result in suites)
            {
                total += result.Total;
                passed += result.Passed;
                failed += result.Failed;
                skipped += result.Skipped;
                inconclusive += result.Inconclusive;
                error += result.Error;
                time += result.Time;

                if (result.StartTime.HasValue && (!startTime.HasValue || result.StartTime.Value < startTime.Value))
                {
                    startTime = result.StartTime;
                }

                if (result.EndTime.HasValue && (!endTime.HasValue || result.EndTime.Value > endTime.Value))
                {
                    endTime = result.EndTime;
                }

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

            if (startTime.HasValue)
            {
                element.SetAttributeValue("start-time", startTime.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
            }

            if (endTime.HasValue)
            {
                element.SetAttributeValue("end-time", endTime.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
            }

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
                StartTime = startTime,
                EndTime = endTime,
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
            DateTime? startTime = null;
            DateTime? endTime = null;

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

                if (!startTime.HasValue || result.StartTime < startTime)
                {
                    startTime = result.StartTime;
                }

                if (!endTime.HasValue || result.EndTime > endTime)
                {
                    endTime = result.EndTime;
                }

                // Create test-case elements
                element.Add(CreateTestCaseElement(result));
            }

            // Create test-suite element for the TestFixture
            var name = resultsByType.Key.SubstringAfterDot();

            // Return the type name after removing the parameters for parametrized tests.
            var parameterStart = resultsByType.Key.IndexOf('(');
            var className = parameterStart > 0 ? resultsByType.Key.Substring(0, parameterStart) : resultsByType.Key;

            element.SetAttributeValue("type", "TestFixture");
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("fullname", resultsByType.Key);
            element.SetAttributeValue("classname", className);

            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", passed);
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("inconclusive", inconclusive);
            element.SetAttributeValue("skipped", skipped);

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);

            if (startTime.HasValue)
            {
                element.SetAttributeValue("start-time", startTime.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
            }

            if (endTime.HasValue)
            {
                element.SetAttributeValue("end-time", endTime.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
            }

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
                StartTime = startTime,
                EndTime = endTime,
                Time = time
            };
        }

        private static XElement CreateTestCaseElement(TestResultInfo result)
        {
            var element = new XElement(
                "test-case",
                new XAttribute("name", result.DisplayName),
                new XAttribute("fullname", result.FullTypeName + "." + result.Method),
                new XAttribute("methodname", result.Method),
                new XAttribute("classname", result.Type),
                new XAttribute("result", OutcomeToString(result.Outcome)),
                new XAttribute("start-time", result.StartTime.ToString(DateFormat, CultureInfo.InvariantCulture)),
                new XAttribute("end-time", result.EndTime.ToString(DateFormat, CultureInfo.InvariantCulture)),
                new XAttribute("duration", result.Duration.TotalSeconds),
                new XAttribute("asserts", 0),
                CreateSeedAttribute(result),
                CreatePropertiesElement(result));

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
                    new XElement("message", result.ErrorMessage),
                    new XElement("stack-trace", result.ErrorStackTrace)));
            }

            // Add attachments if available
            if (result.Attachments.Any())
            {
                // See spec here: https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html#attachments
                var attachmentElement = new XElement("attachments");
                foreach (var attachment in result.Attachments)
                {
                    attachmentElement.Add(new XElement(
                                "attachment",
                                new XElement("filePath", attachment.FilePath),
                                new XElement("description", new XCData(attachment.Description))));
                }

                element.Add(attachmentElement);
            }

            return element;
        }

        private static XAttribute CreateSeedAttribute(TestResultInfo result)
        {
            var seed = result.Properties.SingleOrDefault(p => p.Key == "NUnit.Seed");
            return seed.Key == "NUnit.Seed"
                ? new XAttribute("seed", seed.Value)
                : null;
        }

        private static XElement CreatePropertiesElement(TestResultInfo result)
        {
            var propertyElements = new HashSet<XElement>(result.Traits.Select(CreatePropertyElement));

#pragma warning disable CS0618 // Type or member is obsolete

            // Required since TestCase.Properties is a superset of TestCase.Traits
            // Unfortunately not all NUnit properties are available as traits
            var traitProperties = result.Properties;

#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var p in traitProperties)
            {
                if (p.Key == "NUnit.TestCategory")
                {
                    var propValue = p.Value;
                    var elements = CreatePropertyElement("Category", (string[])propValue);

                    foreach (var element in elements)
                    {
                        propertyElements.Add(element);
                    }
                }
            }

            return propertyElements.Any()
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

        private XElement CreateTestRunElement(List<TestResultInfo> results, TestRunConfiguration runConfiguration)
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

            element.SetAttributeValue("start-time", runConfiguration.StartTime.ToString(DateFormat, CultureInfo.InvariantCulture));
            element.SetAttributeValue("end-time", runConfiguration.EndTime.ToString(DateFormat, CultureInfo.InvariantCulture));

            return element;
        }

        private XElement CreateAssemblyElement(IGrouping<string, TestResultInfo> resultsByAssembly)
        {
            var assemblyPath = resultsByAssembly.Key;
            var fixtures = from resultsInAssembly in resultsByAssembly
                           group resultsInAssembly by resultsInAssembly.FullTypeName
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

            public DateTime? StartTime { get; set; }

            public DateTime? EndTime { get; set; }
        }
    }
}
