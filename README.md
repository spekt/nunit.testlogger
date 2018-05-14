# Logger Extensions
Reporting extensions for [Visual Studio Test Platform](https://gtihub.com/microsoft/vstest).

[![Build status](https://ci.appveyor.com/api/projects/status/6acdk0kx0smkcktl?svg=true)](https://ci.appveyor.com/project/Faizan2304/loggerextensions)

## Packages
| Logger | Nuget Package |
| ------ | ------------- |
| AppVeyor | [![NuGet](https://img.shields.io/nuget/v/Appveyor.TestLogger.svg)](https://www.nuget.org/packages/Appveyor.TestLogger/) |
| NUnit | [![NuGet](https://img.shields.io/nuget/v/NUnitXml.TestLogger.svg)](https://www.nuget.org/packages/NUnitXml.TestLogger/) |
| Xunit | [![NuGet](https://img.shields.io/nuget/v/XunitXml.TestLogger.svg)](https://www.nuget.org/packages/XunitXml.TestLogger/) |

## Usage
### Appveyor logger
The appveyor logger can report test results automatically to the CI build. See an example: https://ci.appveyor.com/project/Faizan2304/loggerextensions/build/1.0.24/tests.

1. Add a reference to the [AppVeyor Logger](https://www.nuget.org/packages/Appveyor.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --test-adapter-path:. --logger:Appveyor
```
3. Test results are automatically reported to the AppVeyor CI results


### NUnit Logger
NUnit logger can generate xml reports in the NUnit v3 format (https://github.com/nunit/docs/wiki/Test-Result-XML-Format).

1. Add a reference to the [NUnit Logger](https://www.nuget.org/packages/NUnitXml.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --test-adapter-path:. --logger:nunit
```
3. Test results are generated in the `TestResults` directory relative to the `test.csproj`

A path for the report file can be specified as follows:
```
> dotnet test --test-adapter-path:. --logger:nunit;LogFilePath=loggerFile.xml
```

`loggerFile.xml` will be generated in the same directory as `test.csproj`.

4. If you are targeting multiple frameworks in your `test.csproj`, you can enable `AppendTimeStamp` so that `LogFilePath` has `HH:mm:ss:ms` appended.
```
> dotnet test --test-adapter-path:. --logger:nunit;LogFilePath=loggerFile.xml;AppendTimeStamp=true
```
`loggerFile-19:05:30:336.xml` will be generated for each target framework.

### Xunit Logger
Xunit logger can generate xml reports in the xunit v2 format (https://xunit.github.io/docs/format-xml-v2.html).

1. Add a reference to the [Xunit Logger](https://www.nuget.org/packages/XunitXml.TestLogger) nuget package in test project
2. Use the following command line in tests
```
> dotnet test --test-adapter-path:. --logger:xunit
```
3. Test results are generated in the `TestResults` directory relative to the `test.csproj`

A path for the report file can be specified as follows:
```
> dotnet test --test-adapter-path:. --logger:xunit;LogFilePath=loggerFile.xml
```

`loggerFile.xml` will be generated in the same directory as `test.csproj`.

## LICENSE
MIT
