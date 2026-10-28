# Shadowrun Dragonfall Comfort

[English](README.md) | **简体中文**

适用于《Shadowrun: Dragonfall – Director's Cut》的两个独立 BepInEx 插件，可单独安装。通过运行时补丁调整 UI 和镜头，无需替换游戏 DLL，可保留现有汉化文件。

| 插件 | 功能 |
| --- | --- |
| **SRDFUIScale** | 整体缩放 UI，默认 1.36 倍，可调范围 1.0～1.5 |
| **SRDFCameraComfort** | 扩大镜头缩放范围，并可保持进入对话时的缩放 |

## 安装

需要 Windows 版游戏和 [BepInEx 5.x Windows x86](https://github.com/BepInEx/BepInEx/releases)。

1. 关闭游戏，将 BepInEx 安装到游戏根目录。
2. 在 `BepInEx/config/BepInEx.cfg` 中设置以下选项；已有配置时只修改对应项：

   ```ini
   [Preloader.Entrypoint]
   Assembly = UnityEngine.dll
   Type = Camera
   Method = .cctor

   [Chainloader]
   HideManagerGameObject = true
   ```

3. 将所需插件的 DLL 或文件夹放入 `BepInEx/plugins`。
4. 启动游戏，插件会在 `BepInEx/config` 中生成配置文件。

仅下载源码时，需要先按下文编译。加载问题可查看 `BepInEx/LogOutput.log`。

## 使用与配置

### UI 缩放

编辑 `local.srdf.ui-scale.cfg`：

```ini
[Interface]
Enabled = true
Scale = 1.36

[Shortcuts]
EnableHotkeys = true
```

- `Scale` 是相对于当前游戏 UI 的倍率，`1.0` 为原尺寸。
- **F4**：重新读取已保存的 UI 配置。
- **F6**：临时切换原尺寸与配置尺寸，不修改配置文件。
- 设置 `EnableHotkeys = false` 可关闭插件快捷键；原游戏快捷键不受影响。关闭后可通过重启游戏应用配置。

UI 放大不会重新排列布局，较大倍率可能导致裁切或文字模糊。

### 摄像机

编辑 `local.srdf.camera-comfort.cfg`，修改后重启游戏：

```ini
[Camera]
UnlockZoom = true
PreserveDialogueZoom = true
```

- `UnlockZoom`：解锁内部缩放范围 0.25～7，切换场景后重新应用。
- `PreserveDialogueZoom`：对话期间保持进入时的倍率，仍允许镜头朝说话者平移。近距离对话可能使角色出框；关闭此项可恢复原有对话缩放。

## 更新与卸载

关闭游戏后替换插件 DLL，保留配置即可更新。若从旧名称版本升级，请移除旧 DLL，避免重复加载；旧的 `local.dragonfall.*.cfg` 可重命名为 `local.srdf.*.cfg` 以沿用设置。

卸载时将对应插件移出 `BepInEx/plugins`，配置文件可自行保留或删除。无需恢复游戏 DLL。

## 从源码构建

需要 Windows、PowerShell、.NET Framework C# 编译器，以及游戏和 BepInEx 的程序集。不需要 .NET SDK。

在仓库目录运行，例如：

```powershell
.\build.ps1 -GamePath 'D:\Games\Dragonfall'
```

默认使用游戏已安装的 BepInEx，输出到仓库的 `artifacts` 目录；将其中 `BepInEx/plugins` 下的插件复制到游戏即可。仓库位于游戏目录下时，可省略 `GamePath`。

构建脚本支持 `-BepInExCorePath`、`-CompilerPath` 和 `-OutputPath`，分别指定依赖、编译器和输出位置。

## 致谢

移植参考 Shadowrun Returns 的 [SRR Camera Comfort](https://www.nexusmods.com/shadowrunreturns/mods/283) 和 [UI Scaler](https://www.nexusmods.com/shadowrunreturns/mods/284)。
