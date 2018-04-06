## Changes in Ice Builder for MSBuild 5.0.4

- Fix a bogus comparison that can result in failure loading projects
  if Ice NuGet pakcages are not installed. See #3

## Changes in Ice Builder for MSBuild 5.0.3

- Fix SliceCppTask task to always pass --header-ext and --source-ext
  command line switches to slice2cpp, previously that was only done
  for non default arguments see issue #2

- Fix bogus MSBuildAssemblyVersion check, this property is not set with
  old Visual Studio versions the code must check it is set before do a
  version comparison.

## Changes in Ice Builder for MSBuild 5.0.2

- Fix bogus condition that cause SliceCompile items from outside
  the project directory to not show up in Visual Studio solution
  explorer. This affects only .NET Core projects with Visual Studio
  2017.

- Fix exclude patterns to account for items using a full path.

- Fix typo that prevents C# generated items from being automatically
  included in the project build.

- Fix Ice 3.6 LocalDebuggerEnvironment settings.

## Changes in Ice Builder for MSBuild 5.0.1
Initial release, was previously part of Ice Builder for Visual Studio
(in [ice-builder-visualstudio](https://github.com/zeroc-ice/ice-builder-visualstudio) repo)
