# Ice Builder for MSBuild

The Ice Builder for MSBuild provides support for compiling Slice source files
(`.ice` files) within C++ and C# MSBuild projects. It compiles these Slice files
using the Slice to C++ compiler (`slice2cpp`) or the Slice to C# compiler (`slice2cs`)
provided by your Ice  installation.

Ice Builder compiles or recompiles a Slice file when the corresponding generated
files are missing or when they are out of date. Generated files are out of date
when they are older than the corresponding Slice source file, or when they are older
than any Slice file included directly or indirectly by this Slice source file.

The Ice Builder for MSBuild requires Ice 3.6.0 or higher, and MSBuild 4.0 or higher.

## Contents
* [Installation](#installation)
* [Selecting your Ice Installation](#selecting-your-ice-installation)
  * [Ice NuGet](#ice-nuget)
  * [Other Ice Installation on Windows](#other-ice-installation-on-windows)
* [Adding Slice Files to your Project](#adding-slice-files-to-your-project)
* [Compiling and Linking your Project with Ice](#compiling-and-linking-your-project-with-ice)
* [Customizing the Slice to C++ Compilation](#customizing-the-slice-to-c-compilation)
* [Customizing the Slice to C# Compilation](#customizing-the-slice-to-c-compilation-1)
* [Building Ice Builder from Source](#building-ice-builder-from-source)
  * [Build Requirements](#build-requirements)
  * [Build Instructions](#build-instructions)

## Installation

To install Ice Builder, you just need to add the `zeroc.icebuilder.msbuild` [NuGet package][1]
to your C++ or C# MSBuild project.

For C++ projects, `zeroc.icebuilder.msbuild` inserts its `SliceCompile` task before the
default `Compile` task, and inserts its `SliceClean` task before the default `Clean` task.
`SliceCompile` generates C++ code using `slice2cpp` and adds the generated C++ files to
the `ClCompile` items. All the Slice files in a given project are compiled through a single 
`slice2cpp` invocation.

For C# projects, `zeroc.icebuilder.msbuild` inserts its `SliceCompile` task before the
default `Compile` task, and inserts its `SliceClean` task before the default `Clean` task.
`SliceCompile` generates C# code using `slice2cs` and adds the generated C# files to
the C# `Compile` items. All the Slice files in a given project are compiled through a 
single `slice2cs` invocation.

## Selecting your Ice Installation

The Ice Builder for MSBuild relies on the following MSBuild properties to locate
and validate your Ice installation:

| Property      | Description                             | Used for                                                            |
| --------------|-----------------------------------------|-------------------------------------------------------------------- |
| IceHome       | Root directory of your Ice installation | `$(IceHome)/slice`, the Slice files of your Ice installation        |
| IceToolsPath  | Directory of `slice2cpp` and `slice2cs` | Compiling Slice files into C++ or C#                                |
| IceIntVersion | Ice version as an integer               | Making sure you are using a version of Ice supported by Ice Builder |

### Ice NuGet

If you add a `zeroc.ice.` NuGet package to your project, the NuGet package sets
all these properties:

| Property      | Value with Ice NuGet                                                                |
| --------------|-------------------------------------------------------------------------------------|
| IceHome       | The Ice NuGet's root installation folder                                            |
| IceToolsPath  | The NuGet's tools folder on Windows, `/usr/bin` on Linux, `/usr/local/bin` on macOS |
| IceIntVersion | The version of Ice installed by this NuGet package                                  |

### Other Ice Installation on Windows

If you don't install Ice as a NuGet package and you don't set `IceHome` in your project file,
Ice Builder reads the value for `IceHome` from the Windows registry using the key
`HKEY_CURRENT_USER\Software\ZeroC\IceBuilder\IceHome`. It also reads `IceIntVersion` from the
Windows registry key `HKEY_CURRENT_USER\Software\ZeroC\IceBuilder\IceIntVersion`.

The value for these Windows registry keys are set by the [Ice Builder for Visual Studio][2].

If you don't set `IceToolsPath` in your project file, Ice Builder sets `IceToolsPath` to
`$(IceHome)\bin`.

## Adding Slice Files to your Project

You need to tell Ice Builder which Slice files (files with a `.ice` extension) to compile,
by adding these files to your project.

You can add all Slice files found in in your project's home directory and any of its 
sub-directories (and sub-sub directories, recursively) to your project by setting both
`EnableDefaultItems` and `EnableDefaultSliceCompileItems` to true. The default value for 
`EnableDefaultSliceCompileItems` is true while the default value for `EnableDefaultItems` 
depends on the project's type. The default value of `EnabledDefaultItems` is true for 
.NET Core projects, and it's unset (or false) for C++ and .NET Framework projects.

As an alternative, you can add Slice files explicitly to your project using the `SliceCompile`
item type, for example:
```
<ItemGroup>
    <SliceCompile Include="../Hello.ice"/>
</ItemGroup>
```

## Compiling and Linking your Project with Ice

If you add an Ice NuGet to your project, the NuGet configures your project to compile
and link your code with Ice. For example, for C++ projects, it adds the correct include directory 
to the `AdditionalIncludeDirectories` property, and more.

If you use an Ice 3.6 MSI or Web Install binary distribution, Ice Builder imports
a property file from the binary distribution, and this property file performs the same setup.

These properties are not set automatically for other Ice installations, such as source builds. 

## Customizing the Slice to C++ Compilation

You can customize the options passed by Ice Builder to `slice2cpp` with the
following properties:

| Property                                     | Default Value            | Corresponding `slice2cpp` [option][3]|
| -------------------------------------------- | ------------------------ | ------------------------------------ |
| SliceCompileOutputDir                        | $(ProjectDir)\generated  | `--output-dir`                       |
| SliceCompileHeaderOutputDir                  | $(SliceCompileOutputDir) | (none)                               |
| SliceCompileIncludeDirectories               |                          | `-I`                                 |
| SliceCompileBaseDirectoryForGeneratedInclude |                          | `--include-dir`                      |
| SliceCompileHeaderExt                        | .h                       | `--header-ext`                       |
| SliceCompileSourceExt                        | .cpp                     | `--source-ext`                       |
| SliceCompileAdditionalOptions                |                          | (any)                                |

`SliceCompileHeaderOutputDir`: if you set this property, Ice Builder moves header
files generated by `slice2cpp` to the specified directory.

`SliceCompileIncludeDirectories`: Ice Builder invokes `slice2cpp` with `-I` for all
the directories specified by this property, followed by `-I $(IceHome)/slice`. As
a result, you never need to include `$(IceHome)/slice` in this list.

`SliceCompileBaseDirectoryForGeneratedInclude`: if you leave this property unset,
Ice Builder automatically adds `SliceCompileHeaderOutputDir` (when set) or
`SliceCompileOutputDir` to the include directories used during the C++ compilation
of your project (the `AdditionalIncludeDirectories` property).

## Customizing the Slice to C# Compilation

You can customize the options passed by Ice Builder to `slice2cs` with the
following properties:

| Property                       | Default Value           | Corresponding `slice2cs` [option][4]|
| -------------------------------|------------------------ |-------------------------------------|
| SliceCompileOutputDir          | $(ProjectDir)/generated | `--output-dir`                      |
| SliceCompileIncludeDirectories |                         | `-I`                                |
| SliceCompileAdditionalOptions  |                         | (any)                               |

`SliceCompileIncludeDirectories`: Ice Builder invokes `slice2cs` with `-I` for all
the directories specified by this property, followed by `-I $(IceHome)/slice`. As
a result, you never need to include `$(IceHome)/slice` in this list.

## Building Ice Builder from Source

### Build Requirements

You need Visual Studio 2017 and the [Visual Studio 2017 SDK][5].

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
[3]: https://doc.zeroc.com/pages/viewpage.action?pageId=18255322
[4]: https://doc.zeroc.com/display/Ice37/slice2cs+Command-Line+Options
[5]: https://docs.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk
