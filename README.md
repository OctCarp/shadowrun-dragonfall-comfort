# Shadowrun Dragonfall Comfort

**English** | [简体中文](README_zh.md)

Two independent BepInEx plugins for **Shadowrun: Dragonfall – Director's Cut**. They adjust UI scaling and camera zoom through runtime patches, without replacing game DLLs or translation files. Either plugin can be installed on its own.

| Plugin | Features |
| --- | --- |
| **SRDFUIScale** | Scales the UI as a whole. Defaults to 1.36, with a supported range of 1.0–1.5. |
| **SRDFCameraComfort** | Extends the camera zoom range and optionally preserves zoom during dialogue. |

## Installation

Requires the Windows version of the game and [BepInEx 5.x for Windows x86](https://github.com/BepInEx/BepInEx/releases).

1. Close the game and install BepInEx in the game directory.
2. Set the following options in `BepInEx/config/BepInEx.cfg`. If the file already exists, update only the relevant entries:

   ```ini
   [Preloader.Entrypoint]
   Assembly = UnityEngine.dll
   Type = Camera
   Method = .cctor

   [Chainloader]
   HideManagerGameObject = true
   ```

3. Copy the desired plugin DLLs or their folders into `BepInEx/plugins`.
4. Start the game. Each plugin creates its configuration file in `BepInEx/config`.

If you downloaded the source only, build the plugins using the instructions below. Check `BepInEx/LogOutput.log` for loading errors.

## Configuration

### UI scaling

Edit `local.srdf.ui-scale.cfg`:

```ini
[Interface]
Enabled = true
Scale = 1.36

[Shortcuts]
EnableHotkeys = true
```

- `Scale` multiplies the game's current UI size. Use `1.0` for the original size.
- **F4** reloads the saved UI configuration.
- **F6** temporarily switches between the original and configured sizes without changing the configuration file.
- Set `EnableHotkeys = false` to disable plugin shortcuts. The game's original shortcuts are unaffected. With plugin shortcuts disabled, restart the game to apply configuration changes.

Scaling does not rearrange the layout. Larger values may cause clipping or blurry text.

### Camera

Edit `local.srdf.camera-comfort.cfg`, then restart the game:

```ini
[Camera]
UnlockZoom = true
PreserveDialogueZoom = true
```

- `UnlockZoom` extends the internal zoom range to 0.25–7 and reapplies it after scene changes.
- `PreserveDialogueZoom` holds the zoom level used when dialogue begins while allowing the camera to pan toward speakers. At close zoom, characters may leave the frame. Disable this option to restore normal dialogue zoom.

## Updating and uninstalling

Close the game before replacing plugin DLLs. Keep your configuration files when updating. If upgrading from an older filename, remove the old DLL to avoid duplicate loading. Rename old `local.dragonfall.*.cfg` files to `local.srdf.*.cfg` to retain their settings.

To uninstall, remove the corresponding plugin from `BepInEx/plugins`. Configuration files can be kept or deleted. No game DLLs need to be restored.

## Building from source

Requires Windows, PowerShell, a .NET Framework C# compiler, and the game and BepInEx assemblies. The .NET SDK is not required.

Run from the repository directory, for example:

```powershell
.\build.ps1 -GamePath 'D:\Games\Dragonfall'
```

By default, the script uses BepInEx from the game installation and writes output to `artifacts`. Copy the generated plugin folders under `BepInEx/plugins` into the game's matching directory. You can omit `GamePath` if the repository is directly inside the game directory.

Use `-BepInExCorePath`, `-CompilerPath`, and `-OutputPath` to specify the dependency directory, compiler executable, and output directory.

## Credits

Adapted from [SRR Camera Comfort](https://www.nexusmods.com/shadowrunreturns/mods/283) and [UI Scaler](https://www.nexusmods.com/shadowrunreturns/mods/284) for Shadowrun Returns.
