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
- [Installation](#installation)
- [Selecting your Ice Installation](#selecting-your-ice-installation)
- [C++ Usage](#c-usage)
- [C# Usage](#c-usage-1)
- [Building from Source](#building-from-source)

## Installation

To install Ice Builder, you just need to add the `zeroc.icebuilder.msbuild` [NuGet package][1]
to your C++ or C# MSBuild project.

For C++ projects, `zeroc.icebuilder.msbuild` inserts its `SliceCompile` task before the
default `Compile` task, and inserts its `SliceClean` task before the default `Clean` task.
`SliceCompile` generates C++ code using `slice2cpp` and adds the generated C++ files to
the `ClCompile` items.

For C# projects, `zeroc.icebuilder.msbuild` inserts its `SliceCompile` task before the
default `Compile` task, and inserts its `SliceClean` task before the default `Clean` task.
`SliceCompile` generates C# code using `slice2cs` and adds the generated C# files to
the C# `Compile` items.

## Selecting your Ice Installation 

The Ice Builder for MSBuild relies on the following MSBuild properties to locate
and validate your Ice installation:

| Property      | Description                             | Used for                                                            |
| --------------|-----------------------------------------|-------------------------------------------------------------------- |
| IceHome       | Root directory of your Ice installation | `$(IceHome)/slice`, the Slice files of your Ice installation        |
| IceToolsPath  | Directory of `slice2cpp` and `slice2cs` | Compiling Slice files into C++ or C#                                |
| IceIntVersion | Ice version as an integer               | Making sure you are using a version of Ice supported by Ice Builder |

If you added a `zeroc.ice.` NuGet package to your project, the NuGet package sets `IceHome` 
automatically to the NuGet's root directory.

Otherwise, on Windows, Ice Builder reads the value for `IceHome` from the Windows
registry using the key `HKEY_CURRENT_USER\Software\ZeroC\IceBuilder\IceHome`. The value for 
this key is set by the [Ice Builder for Visual Studio][2].

You typically need to set `IceHome` if you want to use a source build as your Ice installation.

The default value for `IceToolsPath` is usually correct and does not need to be set explicitly:

| Ice Installation (from `IceHome`)                    | Default IceToolsPath                                             |
| ---------------------------------------------------- | ---------------------------------------------------------------- |
| Ice source build (Ice 3.7 or greater)                | Set by an Ice source tree props file to its `cpp/bin` or similar |
| Ice NuGet on Windows                                 | The NuGet's tools folder                                         |
| Other installation on Windows                        | `$(IceHome)\cpp\bin` if it exists, otherwise `$(IceHome)\bin`    |
| Ice NuGet plus standard binary installation on Linux | `/usr/bin`                                                       |
| Ice NuGet plus homebrew installation on macOS        | `/usr/local/bin`                                                 |

## Adding Slice Files to your Project

Ice Builder automatically adds to your project all Slice files (files with an `.ice` extension)
found in project's directory and any of its sub-directories and sub-sub directories, recursively.
This automatic addition of Slice files is controlled by the property `EnableDefaultSliceCompileItems`,
which is `true` by default. Set this property to `false` to disable this behavior.

As an alternative, you can add Slice files explicitly to your project by setting the `SliceCompile`
property, for example:
```
TODO - example, add file in project's parent directory
```

Setting `SliceCompile` as shown above overwrites any Slice files added to `SliceCompile`
through `EnableDefaultSliceCompileItems`.

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
