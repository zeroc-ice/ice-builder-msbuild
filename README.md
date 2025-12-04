# Ice Builder for MSBuild

Ice Builder for MSBuild provides support for compiling Slice source files
(`.ice` files) in MSBuild projects. It compiles these Slice files using
the Slice to C++ compiler (`slice2cpp`) or the Slice to C# compiler (`slice2cs`)
provided by your Ice installation.

Ice Builder compiles or recompiles a Slice file when the corresponding generated
files are missing or when they are out of date. Generated files are out of date
when they are older than the corresponding Slice source file, or when they are older
than any Slice file included directly or indirectly by this Slice source file.

Ice Builder for MSBuild requires Ice 3.6 or higher, and MSBuild 4.0 or higher.
(MSBuild 4.x is included in Visual Studio 2010). You can configure Ice Builder
directly as described below, or within the Visual Studio IDE using the
[Ice Builder for Visual Studio][1] extension.

> [!IMPORTANT]
> Ice 3.8 introduces a new MSBuild-based Slice builder called **Slice Tools**.  
> For C++ projects, Slice Tools is included in the [ZeroC.Ice.Cpp] NuGet package.  
> For C# projects, Slice Tools is provided by the [ZeroC.Ice.Slice.Tools] NuGet package.  
>
> As a result, **Ice Builder for MSBuild is no longer required when using Ice 3.8**.

## Contents

* [Installation](#installation)
* [Selecting your Ice Installation](#selecting-your-ice-installation)
  * [Ice NuGet](#ice-nuget)
  * [Other Ice Installation on Windows](#other-ice-installation-on-windows)
* [Adding Slice Files to your Project](#adding-slice-files-to-your-project)
* [Compiling and Linking your Project with Ice](#compiling-and-linking-your-project-with-ice)
* [Selecting the Slice to C++ Mapping](#selecting-the-slice-to-c-mapping)
* [Customizing the Slice to C++ Compilation](#customizing-the-slice-to-c-compilation)
* [Customizing the Slice to C# Compilation](#customizing-the-slice-to-c-compilation-1)
* [Building Ice Builder from Source](#building-ice-builder-from-source)
  * [Build Requirements](#build-requirements)
  * [Build Instructions](#build-instructions)

## Installation

To install Ice Builder, you just need to add the `zeroc.icebuilder.msbuild` [NuGet package][2]
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

Ice Builder for MSBuild relies on the following MSBuild properties to locate
and validate your Ice installation:

| Property      | Description                             | Used for                                                            |
| --------------|-----------------------------------------|-------------------------------------------------------------------- |
| IceHome       | Root directory of your Ice installation | `$(IceHome)/slice`, the Slice files of your Ice installation        |
| IceToolsPath  | Directory of `slice2cpp` and `slice2cs` | Compiling Slice files into C++ or C#                                |
| IceIntVersion | Ice version as an integer               | Making sure you are using a version of Ice supported by Ice Builder |

### Ice NuGet

If you add a `zeroc.ice.` NuGet package to your project, the NuGet package sets
these properties:

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

The value for these Windows registry keys are set by the [Ice Builder for Visual Studio][3].

## Adding Slice Files to your Project

You need to tell Ice Builder which Slice files (files with a `.ice` extension) to compile,
by adding these files to your project.

You can add all Slice files found in your project's home directory and any of its
sub-directories (and sub-sub directories, recursively) to your project by setting both
`EnableDefaultItems` and `EnableDefaultSliceCompileItems` to true. The default value for
`EnableDefaultSliceCompileItems` is true while the default value for `EnableDefaultItems`
depends on the project's type. The default value of `EnabledDefaultItems` is true for
.NET Core projects, and it's unset (or false) for C++ and .NET Framework projects.

As an alternative, you can add Slice files to your project using the `SliceCompile`
item type, for example:
```
<ItemGroup>
    <SliceCompile Include="../Hello.ice"/>
</ItemGroup>
```

## Compiling and Linking your Project with Ice

If you add an Ice NuGet to your project, the NuGet configures your project to compile
and link your code with Ice. For example, for C++ projects, it adds the correct include
directory to `ClCompile`'s `AdditionalIncludeDirectories` metadata, and more.

If you use an Ice 3.6 MSI or Web Install binary distribution, Ice Builder imports
a property file from the binary distribution, and this property file performs the same setup.

These properties are not set automatically for other Ice installations, such as source builds.

Ice Builder adds automatically the code generated by the Slice compilers (`slice2cpp`
for C++, `slice2cs` for C#) to the `ClCompile` item type (C++) and to the `Compile` item type
(C#).

:memo: **AdditionalIncludeDirectories for C++ Projects**

During C++ compilation, the C++ source code generated by `slice2cpp` and your own C++ source
code need to find the generated C++ header files. With the default settings, these header
files are stored in `$(IntDir)` alongside the generated C++ source files, and you need to
add this directory to `ClCompile`'s `AdditionalIncludeDirectories`:
```
<ItemDefinitionGroup>
    <ClCompile>
        <!-- SliceCompile for C++ puts generated header files in $(IntDir) -->
        <AdditionalIncludeDirectories>$(IntDir)</AdditionalIncludeDirectories>
    </ClCompile>
</ItemDefinitionGroup>
```

## Selecting the Slice to C++ Mapping

As of Ice 3.7, `slice2cpp` generates C++ code for two mappings, the [Slice to C++11 mapping][4]
and the [Slice to C++98 mapping][5]. You select the C++ mapping used by your C++ code by
defining or not defining `ICE_CPP11_MAPPING` during C++ compilation.

Ice Builder selects C++11 as the default mapping when using Visual Studio 2015 or greater,
and C++98 as the default mapping with older versions of Visual Studio. You can overwrite
this default by setting the property `IceCppMapping` to `cpp11` (for the C++11 mapping) or
to `cpp98` (for the C++98 mapping). For example:
```
<PropertyGroup>
    <IceCppMapping>cpp98</IceCppMapping>
</PropertyGroup>
```
Setting `IceCppMapping` to `cpp11` is equivalent to adding `ICE_CPP11_MAPPING` to the
`PreprocessorDefinitions` item metadata of `ClCompile` for all configurations and platforms.
This setting is ignored unless you are using Ice 3.7 with Visual Studio 2015 or greater.

Setting `IceCppMapping` to `cpp98` has currently no effect other than overwriting the
default value with Visual Studio 2015 or greater.

## Customizing the Slice to C++ Compilation

You can customize the options passed by Ice Builder to `slice2cpp` by setting the
following SliceCompile item metadata:

| Item Metadata                    | Default Value | Corresponding `slice2cpp` [Option][6]|
| -------------------------------- | ------------- | ------------------------------------ |
| OutputDir                        | $(IntDir)     | `--output-dir`                       |
| HeaderOutputDir                  |               | (none)                               |
| IncludeDirectories               |               | `-I`                                 |
| BaseDirectoryForGeneratedInclude |               | `--include-dir`                      |
| HeaderExt                        | .h            | `--header-ext`                       |
| SourceExt                        | .cpp          | `--source-ext`                       |
| AdditionalOptions                |               | (any)                                |

`OutputDir`: Ice Builder instructs `slice2cpp` to generate C++ files in this directory.
The value you specify can change depending on the platform and configuration. With a
variable value such as `$(IntDir)` (the default), cleaning a given configuration and
platform with `SliceCompileClean` does not affect other configurations and platforms.

`HeaderOutputDir`: if you set this metadata, Ice Builder moves the header files
generated by `slice2cpp` to the specified directory, otherwise they remain in the same
directory as the generated source files (`OutputDir`).

`IncludeDirectories`: Ice Builder invokes `slice2cpp` with `-I` for all the directories
specified by this metadata, followed by `-I $(IceHome)/slice`. As a result, you never
need to include `$(IceHome)/slice` in this list.

For example, you can set `HeaderOutputDir` as follows:
```
<ItemDefinitionGroup>
    <SliceCompile>
       <HeaderOutputDir>..\include\generated\$(IntDir)</HeaderOutputDir>
    </SliceCompile>
</ItemDefinitionGroup>
```

and then add this directory to `ClCompile`'s `AdditionalIncludeDirectories`:
```
<ItemDefinitionGroup>
    <ClCompile>
        <AdditionalIncludeDirectories>..\include\generated\$(IntDir)</AdditionalIncludeDirectories>
    </ClCompile>
</ItemDefinitionGroup>
```
## Customizing the Slice to C# Compilation

You can customize the options passed by Ice Builder to `slice2cs` by setting the
following SliceCompile item metadata:

| Item Metadata      | Default Value                        | Corresponding `slice2cs` [Option][7]|
| -------------------|------------------------------------- |-------------------------------------|
| OutputDir          | $(MSBuildProjectDirectory)/generated | `--output-dir`                      |
| IncludeDirectories |                                      | `-I`                                |
| AdditionalOptions  |                                      | (any)                               |

`IncludeDirectories`: Ice Builder invokes `slice2cs` with `-I` for all the directories
specified by this metadata, followed by `-I $(IceHome)/slice`. As a result, you never
need to include `$(IceHome)/slice` in this list.

For example, you can set `IncludeDirectories` as follows:
```
<ItemDefinitionGroup>
    <SliceCompile>
        <IncludeDirectories>../shared/slice;.</IncludeDirectories>
    </SliceCompile>
</ItemDefinitionGroup>
```

## Building Ice Builder from Source

### Build Requirements

You need Visual Studio 2017 with the .NET Core cross-development toolset.

### Build Instructions

Open a Visual Studio Command prompt and run the following command:

```
MSBuild msbuild\icebuilder.proj /t:Restore
MSBuild msbuild\icebuilder.proj /t:NuGetPack
```

You can sign the assemblies with Authenticode by setting the environment variable `SIGN_CERTIFICATE` to
the path of your PFX certificate store, and the `SIGN_PASSWORD` environment variable to the password
used by your certificate store.

[1]: https://github.com/zeroc-ice/ice-builder-visualstudio
[2]: https://www.nuget.org/packages/zeroc.icebuilder.msbuild
[3]: https://github.com/zeroc-ice/ice-builder-visualstudio
[4]: https://doc.zeroc.com/pages/viewpage.action?pageId=18255283
[5]: https://doc.zeroc.com/pages/viewpage.action?pageId=18255332
[6]: https://doc.zeroc.com/pages/viewpage.action?pageId=18255322
[7]: https://doc.zeroc.com/display/Ice37/slice2cs+Command-Line+Options
[ZeroC.Ice.Cpp]: https://www.nuget.org/packages/ZeroC.Ice.Cpp/
[ZeroC.Ice.Slice.Tools]: https://www.nuget.org/packages/ZeroC.Ice.Slice.Tools/