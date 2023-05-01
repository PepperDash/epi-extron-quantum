# Essentials Extron Quantum Plugin

## License

Provided under MIT license

## Overview

This plugin controls input switching and preset recall over IP or RS-232 for Extron Quantum Video Wall processors.


## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

Dependencies will be installed automatically by Visual Studio on opening. Use the Nuget Package Manager in
Visual Studio to manage nuget package dependencies. All files will be output to the `output` directory at the root of
repository.

### Installing Different versions of PepperDash Core

If a different version of PepperDash Core is needed, use the Visual Studio Nuget Package Manager to install the desired
version.

# Usage

## Join Map

### Digitals

| Join Number | JoinSpan | Description | Capabilities |
| ----------- | -------- | ----------- | ------------ |
| 1 | 1 | Is Online | To SIMPL |

### Analogs

| Join Number | JoinSpan | Description | Capabilities |
| ----------- | -------- | ----------- | ------------ |
| 1 | 1 | Socket Status | To SIMPL |
| 2 | 1 | Preset Select. If a canvas is NOT selected, the preset will be recalled on ALL canvases | From SIMPL |
| 3 | 1 | Canvas select. Valid values are 0 - 10 | To/From SIMPL |
| 11 | 40 | Input select. Each analog corresponds to a window. Windows are based on canvases. If a Canvas is NOT selected or set to 0, the route will be made to the corresponding window on Canvas 1" | From SIMPL |

### Serials

| Join Number | JoinSpan | Description | Capabilities |
| ----------- | -------- | ----------- | ------------ |
| 1 | 1 | Device Name. Set from Config | To SIMPL |
| 11 | 20 | Preset name. Set from Config | To SIMPL |
