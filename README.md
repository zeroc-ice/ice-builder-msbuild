# Ice Builder for MSBuild

Ice Builder provides support for compiling Slice source files (`.ice` files) within
C++ and C# MSBuild projects. It compiles these Slice files using the Slice to C++ 
compiler (`slice2cpp`) or the Slice to C# compiler (`slice2cs`) provided by your Ice 
installation.

Ice Builder compiles or recompiles a Slice file when the corresponding generated 
files are missing or when they are out of date. Generated files are out of date
when they are older than the corresponding Slice source file, or when they are older
than any Slice file included directly or indirectly by this Slice source file.

Ice Builder for MSBuild requires Ice 3.6.0 or higher, and MSBuild 4.0 or higher.

## Contents
- [Installation](#installation)
- [Ice Home Configuration](#ice-home-configuration)
- [C++ Usage](#c-usage)
- [C# Usage](#c-usage-1)
- [Building from Source](#building-from-source)

## Installation

To install Ice Builder, you just need to add the [`zeroc.icebuilder.msbuild`](1) NuGet package
to your C++ or C# MSBuild project.

For C++ projects, `zeroc.icebuilder.msbuild` inserts its `SliceCompile` task before the
default `Compile` task, and inserts its `SliceClean` task before the default `Clean` task.
`SliceCompile` generates C++ code using `slice2cpp` and adds the generated C++ files to
the `ClCompile` items.

For C# projects, `zeroc.icebuilder.msbuild` inserts its `SliceCompile` task before the
default `Compile` task, and inserts its `SliceClean` task before the default `Clean` task.
`SliceCompile` generates C# code using `slice2cs` and adds the generated C# files to
the C# `Compile` items.

## Ice Home Configuration

The `IceHome` MSBuild property corresponds to the home directory of your Ice installation.
Ice Builder needs this information to locate the Slice to C++ or Slice to C# compiler you 
want to use.

The default value for `IceHome` is often sufficient so you don't need to set `IceHome`
explicitly. This default value depends on your platform and the type of Ice installation 
you're using:

| Platform | Ice Installation                    | Default IceHome                 |
| -------- | ----------------------------------- | ------------------------------- |
| Windows  | NuGet package                       | NuGet installation              |
| Windows  | Source build, Ice 3.6 installation  | Read from the Windows Registry<br>`HKEY_CURRENT_USER\Software\ZeroC\IceBuilder\IceHome`<br>usually set by the [Ice Builder for Visual Studio](2) |
| Linux    | Any                                 | `/usr`                          |
| macOS    | Any                                 | `/usr/local`                    |

## C++ Usage

## C# Usage

## Building from Source

### Build Requirements

You need Visual Studio 2017 and the [Visual Studio 2017 SDK](3).

### Build Instructions

Open Visual Studio Command prompt and run the following command:

```
MSBuild icebuilder.proj /t:NuGetPack
```

You can sign the assembly with Authenticode by setting the environment variable `SIGN_CERTIFICATE` to
the path of your PFX certificate store, and the `SIGN_PASSWORD` environment variable to the password
used by your certificate store.

[1]: https://www.nuget.org/packages/zeroc.icebuilder.msbuild
[2]: https://github.com/zeroc-ice/ice-builder-visualstudio
[3]: https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk
