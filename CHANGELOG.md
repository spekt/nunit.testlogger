# Changelog

## Unreleased (v3.1.x)

* Update core testlogger to 3.0.54 for nunit test adapter
* Fix: Explicit tests should be marked as Skipped. See
  https://github.com/spekt/nunit.testlogger/issues/86
* Fix: Test case parse error if name contains special characters. See
  https://github.com/spekt/nunit.testlogger/issues/90

## v3.0.117 - 2021/11/06

* Upgrade core testlogger to 3.0.47
* Set `classname` for TestFixture element. See
  https://github.com/spekt/nunit.testlogger/issues/87 and
  https://github.com/spekt/nunit.testlogger/issues/88
* Report seed values for tests using `TestContext.CurrentContext.Random`. See
  https://github.com/spekt/nunit.testlogger/issues/78
  
## v3.0.107 - 2021/05/21

* Upgrade core testlogger to 3.0.37
* Fix: 'nunit' friendly name not found when using multiple loggers. See
  https://github.com/spekt/nunit.testlogger/issues/80

## v3.0.97 - 2021/03/10

* Upgrade core testlogger to 3.0.31
* Fix: test results file must overwrite existing file. See
  https://github.com/spekt/nunit.testlogger/issues/76

## v3.0.91 - 2021/01/31

* Refactored code to use [core testlogger][] for the testplatform logger events
* Document common logger options in
  https://github.com/spekt/testlogger/wiki/Logger-Configuration

[core testlogger]: https://github.com/spekt/testlogger

## v2.1.80 - 2020/10/28

* New logo for nuget package. See
  https://github.com/spekt/nunit.testlogger/pull/68
* Fix extraction of testcase name if it contains backslash. See
  https://github.com/spekt/nunit.testlogger/issues/66
* Fix extraction of tuples in testcase name. See
  https://github.com/spekt/nunit.testlogger/pull/65

Thanks @Siphonophora and @KonH for their contributions to this release.

See https://github.com/spekt/nunit.testlogger/compare/v2.1.62..v2.1.80 for all
changes included in this release.

## v2.1.62 - 2020/05/09

* Update toolset to dotnet 3.0
* Fix missing test cases due to brackets. See
  https://github.com/spekt/nunit.testlogger/issues/57
* Support for `--results-directory` in dotnet test. See
  https://github.com/spekt/nunit.testlogger/issues/46

See https://github.com/spekt/nunit.testlogger/compare/v2.1.41..v2.1.62 for
details.

## v2.1.41 - 2019/05/20
