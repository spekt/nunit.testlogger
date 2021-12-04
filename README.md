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
NUnit logger can generate xml reports in the [NUnit v3 format](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html).

1. Add a reference to the [NUnit Logger](https://www.nuget.org/packages/NUnitXml.TestLogger) NuGet package in test project
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

**NUnit test framework settings**

- If your scenario requires test case properties like `Description` in the xml, please enable internal properties for the nunit adapter:

`dotnet test --logger:nunit -- NUnit.ShowInternalProperties=true`

- NUnit test adapter also provides a mechanism to emit test result xml from the NUnit engine. You may use following commandline for the same:

`dotnet test --logger:nunit -- NUnit.TestOutputXml=<foldername relative to test binary directory>`

## Release Checklist

A note to self on how to make releases:

- [ ] Create changelog entry with tentative version.
- [ ] Verify the version on Spekt myget (remember to update version in command below).
```sh
> dotnet new nunit
> dotnet add package NunitXml.TestLogger --version 3.0.109 --source https://www.myget.org/F/spekt/api/v3/index.json
> dotnet test --logger:nunit
```
- [ ] Push the version on Spekt myget to Nuget.
- [ ] Create a github release with above version tag. Link to the changelog section.
- [ ] Thank the issue authors and notify them about the released version.

## License
MIT
