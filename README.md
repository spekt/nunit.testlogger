# NUnit Test Logger
NUnit xml report extension for [Visual Studio Test Platform](https://gtihub.com/microsoft/vstest).

[![Build Status](https://travis-ci.com/spekt/nunit.testlogger.svg?branch=master)](https://travis-ci.com/spekt/nunit.testlogger)
[![Build Status](https://ci.appveyor.com/api/projects/status/2masybxty5kve2dc?svg=true)](https://ci.appveyor.com/project/spekt/nunit-testlogger)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NunitXml.TestLogger)](https://www.nuget.org/packages/NunitXml.TestLogger/)

## Packages
| Logger | Stable Package | Pre-release Package |
| ------ | -------------- | ------------------- |
| NUnit | [![NuGet](https://img.shields.io/nuget/v/NUnitXml.TestLogger.svg)](https://www.nuget.org/packages/NUnitXml.TestLogger/) | [![MyGet Pre Release](https://img.shields.io/myget/spekt/vpre/nunitxml.testlogger.svg)](https://www.myget.org/feed/spekt/package/nuget/NunitXml.TestLogger) |

If you're looking for `xunit`, `junit` or `appveyor` loggers, visit following repositories:
* <https://github.com/spekt/xunit.testlogger>
* <https://github.com/spekt/junit.testlogger>
* <https://github.com/spekt/appveyor.testlogger>

## Usage
NUnit logger can generate xml reports in the NUnit v3 format (https://github.com/nunit/docs/wiki/Test-Result-XML-Format).

1. Add a reference to the [NUnit Logger](https://www.nuget.org/packages/NUnitXml.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --logger:nunit
```
3. Test results are generated in the `TestResults` directory relative to the `test.csproj`

A path for the report file can be specified as follows:
```
> dotnet test --logger:"nunit;LogFilePath=test-result.xml"
```

`test-result.xml` will be generated in the same directory as `test.csproj`.

**Note:** the arguments to `--logger` should be in quotes since `;` is treated as a command delimiter in shell.

All common options to the logger is documented [in the wiki][config-wiki]. E.g.
token expansion for `{assembly}` or `{framework}` in result file.

[config-wiki]: https://github.com/spekt/testlogger/wiki/Logger-Configuration

## License
MIT
