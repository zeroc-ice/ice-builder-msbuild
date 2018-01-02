# Ice Builder for MSBuild

The Ice Builder for MSBuild include MSBuild tasks to automate the compilation of
Slice (`.ice`) files to C++ and C#. It compiles your Slice files with `slice2cpp`
and `slice2cs`, and allows you to specify the parameters provided to these
compilers.

The Ice Builder for MSBuild is a collection of MSBuild Tasks compatible with
MSBuild 4.0 and higher. An Ice installation with `slice2cpp` and `slice2cs`
version 3.6.0 or higher is also required.

## Contents
- [Installation](#installation)
- [Ice Home Configuration](#ice-home-configuration)
- [C++ Usage](#c-usage)
- [C# Usage](#c-usage-1)
- [Building from Source](#building-from-source)

## Installation

The Ice Builder for MSBuild is available as a NuGet package `zeroc.icebuilder.msbuild`
at nuget.org.

Adding the `zeroc.icebuilder.msbuild` NuGet package to a C++ or C# project will add
`SliceCompile` and `SliceClean` task that run before the default `Compile` and `Clean`
tasks.1

With a C# project the `SliceCompile` tasks use the `slice2cs` Slice compiler to compile
Slice files, the generated C# files are add to the `Compile` items set to be compile.

With a C++ project the `SliceCompile` tasks use the `slice2cpp` Slice compiler to compile
Slice files, the generated C++ source files are add to the `ClCompile` items set to be
compile.

## Ice Home Configuration

If you are using ZeroC Ice NuGet packages with your project the builder will use the NuGet
package Ice distribution otherwise if using an Ice source distribution or and Ice MSI binary
distribution you must set `IceHome` MSBuild property to point to the Ice distribution you want
to use.

## C++ Usage

## C# Usage

## Building from Source

### Build Requirements

You need Visual Studio 2017 and [Visual Studio 2017 SDK](https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk)

### Build Instructions

Open Visual Studio Command prompt and run the following command:

```
MSBuild icebuilder.proj /t:NuGetPack
```

You can sign the assembly with Authenticode by setting the environment variable `SIGN_CERTIFICATE` to
the path of your PFX certificate store, and the `SIGN_PASSWORD` environment variable to the password
used by your certificate store.
