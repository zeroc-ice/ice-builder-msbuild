## Changes in Ice Builder for MSBuild 5.0.2

- Fix bogus condition that cause SliceCompile items from outside
  the project directory to not show up in Visual Studio solution
  explorer.

- Fix exclude patterns to account for items using a full path.

- Fix typo that prevents C# generated items from being automatically
  included in the project build.

## Changes in Ice Builder for MSBuild 5.0.1
Initial release, was previously part of Ice Builder for Visual Studio
(in [ice-builder-visualstudio](https://github.com/zeroc-ice/ice-builder-visualstudio) repo)
