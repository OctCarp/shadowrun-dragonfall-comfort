using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DragonfallUIScale
{
    [BepInProcess("Dragonfall.exe")]
    [BepInPlugin("local.srdf.ui-scale", "Dragonfall UI Scale", "1.0.1")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> scale;
        private static ConfigEntry<bool> enabledSetting;
        private static bool bypass;
        private static ConfigEntry<bool> hotkeys;
        private static bool ready;
        private Harmony harmony;

        private void Awake()
        {
            try
            {
                enabledSetting = Config.Bind<bool>("Interface", "Enabled", true, "Scale UI panels, controls and text together.");
                scale = Config.Bind<float>("Interface", "Scale", 1.36f, "1 = current game UI, 1.36 = 36% larger. Supported range 1 to 1.5. F4 reloads this file; F6 toggles original size temporarily. Large values may crop menus.");
                hotkeys = Config.Bind<bool>("Shortcuts", "EnableHotkeys", true, "F4 reloads config; F6 toggles UI size. Set false to disable plugin hotkeys. Original game shortcuts are not intercepted.");
                harmony = new Harmony("local.srdf.ui-scale");
                MethodInfo target = AccessTools.Method(typeof(UIRoot), "Update", Type.EmptyTypes, null);
                if (target == null) throw new MissingMethodException("UIRoot.Update");
                MethodInfo patch = typeof(Plugin).GetMethod("Resize", BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(target, null, null, new HarmonyMethod(patch), null, null);
                ready = true;
                Logger.LogInfo("Dragonfall UI Scale 1.0.1 loaded. Requested multiplier: " + scale.Value + ". F4 reloads; F6 toggles original size. Hotkeys enabled: " + hotkeys.Value + ". Original game shortcuts preserved.");
            }
            catch (Exception ex)
            {
                ready = false;
                Logger.LogError("UI Scale initialization failed: " + ex);
                try
                {
                    if (harmony != null) harmony.UnpatchSelf();
                }
                catch (Exception cleanupEx)
                {
                    Logger.LogError("UI Scale rollback failed: " + cleanupEx);
                }
            }
        }

        // Modify the freshly computed root scale, before its comparison and assignment.
        // This avoids cumulative scaling and avoids resetting/rescaling the root twice per frame.
        private static IEnumerable<CodeInstruction> Resize(IEnumerable<CodeInstruction> input)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(input);
            int count = 0;
            for (int i = 0; i < code.Count; i++) if (code[i].opcode == OpCodes.Div) count++;
            if (count != 1) throw new InvalidOperationException("Unsupported UIRoot.Update: expected one scale division, found " + count);
            int division = code.FindIndex(delegate(CodeInstruction c) { return c.opcode == OpCodes.Div; });
            FieldInfo height = division >= 2 ? code[division - 2].operand as FieldInfo : null;
            if (height == null || height.DeclaringType != typeof(UIRoot) || height.Name != "manualHeight"
                || code[division - 1].opcode != OpCodes.Conv_R4)
                throw new InvalidOperationException("Unsupported UIRoot.Update scale calculation; no changes applied.");
            List<CodeInstruction> result = new List<CodeInstruction>();
            MethodInfo multiplier = typeof(Plugin).GetMethod("Multiplier", BindingFlags.NonPublic | BindingFlags.Static);
            foreach (CodeInstruction instruction in code)
            {
                result.Add(instruction);
                if (instruction.opcode == OpCodes.Div)
                {
                    result.Add(new CodeInstruction(OpCodes.Ldarg_0, null));
                    result.Add(new CodeInstruction(OpCodes.Call, multiplier));
                    result.Add(new CodeInstruction(OpCodes.Mul, null));
                }
            }
            return result;
        }

        private static float Multiplier(UIRoot root)
        {
            if (!ready || bypass || enabledSetting == null || !enabledSetting.Value || scale == null) return 1f;
            // Nested roots inherit their parent's scale; do not enlarge twice.
            Transform parent = root.transform.parent;
            while (parent != null)
            {
                UIRoot ancestor = parent.GetComponent<UIRoot>();
                if (ancestor != null && ancestor.enabled) return 1f;
                parent = parent.parent;
            }
            return ScalePolicy.Clamp(scale.Value);
        }

        private void Update()
        {
            if (!ready || hotkeys == null || !hotkeys.Value) return;
            if (Input.GetKeyDown(KeyCode.F4))
            {
                try { Config.Reload(); Logger.LogInfo("UI config reloaded. Effective multiplier: " + ScalePolicy.Clamp(scale.Value)); }
                catch (Exception ex) { Logger.LogWarning("Could not reload UI config: " + ex.Message); }
            }
            if (Input.GetKeyDown(KeyCode.F6))
            {
                bypass = !bypass;
                Logger.LogInfo(bypass ? "Original UI size temporarily restored." : "Configured UI size restored.");
            }
        }

        private void OnDestroy()
        {
            ready = false;
            if (harmony != null) harmony.UnpatchSelf();
            bypass = false;
        }
    }

    internal static class ScalePolicy
    {
        internal static float Clamp(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 1f;
            return value < 1f ? 1f : (value > 1.5f ? 1.5f : value);
        }
    }
}
