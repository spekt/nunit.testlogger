# Changelog

## Unreleased (v3.1.x)

## v3.1.20 - 2024/02/10

- Update core testlogger to 3.1.140.
- Fix: exclude testlogger assemblies from code coverage. See
  https://github.com/spekt/junit.testlogger/issues/72 and
  https://github.com/spekt/junit.testlogger/issues/64.
- Fix: unknown namespace reporting for
  nested classes. See https://github.com/spekt/testlogger/pull/41.

## v3.1.15 - 2023/07/06

- Update core testlogger to 3.1.130
- Support for test attachments for test cases. See https://github.com/spekt/nunit.testlogger/pull/101 and #54
- Fix for illegal xml characters in various names. See https://github.com/spekt/testlogger/pull/37
- Various infra fixes: move to net7.0, add github ci, and remove appveyor.

## v3.0.131 - 2023/01/30

- Update core testlogger to 3.0.86
- Fix: issue with numeral parsing in test name parser. See https://github.com/spekt/testlogger/issues/35

## v3.0.127 - 2022/09/10

- Update core testlogger to 3.0.78 for nunit test adapter
- Fix: Explicit tests should be marked as Skipped. See
  https://github.com/spekt/nunit.testlogger/issues/86
- Replace Test Case name parser **Possible Breaking Change**
  - For most or maybe all users the new parser should fix the issues shown below, without introducing new issues. In case you do encounter any new parsing failures a feature flag `Parser=Legacy` has been added to use the prior parser. See [logger config wiki](https://github.com/spekt/testlogger/wiki/Logger-Configuration) for details.
  - Fix: Test case parse error if name contains special characters. See
    https://github.com/spekt/nunit.testlogger/issues/90
  - Fix: Covers several parsing issues. Thanks @becha2 for all the detailed examples.
    https://github.com/spekt/testlogger/issues/28
  - Fix: Log member data. Thanks @BottlecapDave for the issue report and @hach-que for the draft fix.
    https://github.com/spekt/junit.testlogger/issues/50
  - Fix: Issue parsing chars. Thanks @binarycow for the issue report.
    https://github.com/spekt/nunit.testlogger/issues/90
  - Reduce log verbosity: The parser, if it encounters problems, will only output one warning per run to the console instead of one per problem

## v3.0.117 - 2021/11/06

- Upgrade core testlogger to 3.0.47
- Set `classname` for TestFixture element. See
  https://github.com/spekt/nunit.testlogger/issues/87 and
  https://github.com/spekt/nunit.testlogger/issues/88
- Report seed values for tests using `TestContext.CurrentContext.Random`. See
  https://github.com/spekt/nunit.testlogger/issues/78

## v3.0.107 - 2021/05/21

- Upgrade core testlogger to 3.0.37
- Fix: 'nunit' friendly name not found when using multiple loggers. See
  https://github.com/spekt/nunit.testlogger/issues/80

## v3.0.97 - 2021/03/10

- Upgrade core testlogger to 3.0.31
- Fix: test results file must overwrite existing file. See
  https://github.com/spekt/nunit.testlogger/issues/76

## v3.0.91 - 2021/01/31

- Refactored code to use [core testlogger][] for the testplatform logger events
- Document common logger options in
  https://github.com/spekt/testlogger/wiki/Logger-Configuration

[core testlogger]: https://github.com/spekt/testlogger

## v2.1.80 - 2020/10/28

- New logo for nuget package. See
  https://github.com/spekt/nunit.testlogger/pull/68
- Fix extraction of testcase name if it contains backslash. See
  https://github.com/spekt/nunit.testlogger/issues/66
- Fix extraction of tuples in testcase name. See
  https://github.com/spekt/nunit.testlogger/pull/65

Thanks @Siphonophora and @KonH for their contributions to this release.

See https://github.com/spekt/nunit.testlogger/compare/v2.1.62..v2.1.80 for all
changes included in this release.

## v2.1.62 - 2020/05/09

- Update toolset to dotnet 3.0
- Fix missing test cases due to brackets. See
  https://github.com/spekt/nunit.testlogger/issues/57
- Support for `--results-directory` in dotnet test. See
  https://github.com/spekt/nunit.testlogger/issues/46

See https://github.com/spekt/nunit.testlogger/compare/v2.1.41..v2.1.62 for
details.

## v2.1.41 - 2019/05/20
