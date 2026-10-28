using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace DragonfallCameraComfort
{
    [BepInProcess("Dragonfall.exe")]
    [BepInPlugin("local.srdf.camera-comfort", "Dragonfall Camera Comfort", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ConfigEntry<bool> Unlock;
        internal static ConfigEntry<bool> Preserve;
        private Harmony harmony;
        private static CameraControl heldCamera;
        private static float heldZoom;
        private static FieldInfo Actual;
        private static FieldInfo Desired;
        private static FieldInfo Original;
        private static FieldInfo Controlled;
        private static FieldInfo EndZoom;
        private static FieldInfo HardFocus;
        private static FieldInfo Trigger;

        private static FieldInfo Field(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(typeof(CameraControl), name);
            if (field == null || field.FieldType != type)
                throw new MissingFieldException("Unsupported CameraControl field: " + name);
            return field;
        }

        private void Awake()
        {
            try
            {
                Unlock = Config.Bind<bool>("Camera", "UnlockZoom", true,
                    "Keep the game's Ctrl-M zoom range (0.25 to 7) enabled across scenes. Restart after changing settings.");
                Preserve = Config.Bind<bool>("Camera", "PreserveDialogueZoom", true,
                    "Hold the visible zoom during conversations, including interactions using the dialogue UI. Speaker panning remains enabled.");
                harmony = new Harmony("local.srdf.camera-comfort");
                Actual = Field("zoomActual", typeof(float));
                Desired = Field("zoomDesired", typeof(float));
                Original = Field("originalZoomAmount", typeof(float));
                Controlled = Field("isControlledZoom", typeof(bool));
                EndZoom = Field("endZoomAmount", typeof(float));
                HardFocus = Field("zoomedOutDuringHardFocus", typeof(bool));
                Trigger = Field("triggerZoomDesired", typeof(float));
                Patch(typeof(Settings), "ComputeDimension", Type.EmptyTypes, null, "UnlockRange");
                // Reapply before percentages are converted back to zoom values, not just afterwards.
                Patch(typeof(CameraControl), "ApplyCurrentZoomValuesPercentages", Type.EmptyTypes, "UnlockRange", null);
                Patch(typeof(CameraControl), "Reset", Type.EmptyTypes, null, "CameraReset");
                Patch(typeof(RunManager), "InputZoomFree", Type.EmptyTypes, "DebugZoom", null);
                Patch(typeof(CameraControl), "SetConvoTitleSafeAreaChange", new Type[] { typeof(bool) }, "ConversationBegin", "ConversationEnd");
                Patch(typeof(CameraControl), "SetControlledZoom", new Type[] { typeof(float) }, "AllowAutomaticZoom", null);
                Patch(typeof(CameraControl), "UpdateTriggerZoom", Type.EmptyTypes, "AllowAutomaticZoom", null);
                Patch(typeof(CameraControl), "AddCameraTriggerZoom", new Type[] { typeof(float) }, "AllowAutomaticZoom", null);
                Patch(typeof(CameraControl), "UpdateZoom", Type.EmptyTypes, "BeforeZoomRender", null);
                Patch(typeof(CameraControl), "GetPOIZoom", new Type[] { typeof(UnityEngine.Vector3) }, null, "PointOfInterestZoom");
                UnlockRange();
                Logger.LogInfo("Dragonfall Camera Comfort 1.0.0 loaded: UnlockZoom=" + Unlock.Value + "; PreserveDialogueZoom=" + Preserve.Value + ". 10 camera hooks installed.");
            }
            catch (Exception ex)
            {
                heldCamera = null;
                Logger.LogError("Camera Comfort initialization failed: " + ex);
                try
                {
                    if (harmony != null) harmony.UnpatchSelf();
                }
                catch (Exception cleanupEx)
                {
                    Logger.LogError("Camera Comfort rollback failed: " + cleanupEx);
                }
            }
        }

        private void Patch(Type type, string name, Type[] args, string prefix, string postfix)
        {
            MethodInfo target = AccessTools.Method(type, name, args, null);
            if (target == null) throw new MissingMethodException(type.FullName, name);
            HarmonyMethod before = prefix == null ? null : new HarmonyMethod(typeof(Plugin).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic));
            HarmonyMethod after = postfix == null ? null : new HarmonyMethod(typeof(Plugin).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(target, before, after, null, null, null);
        }

        private static void UnlockRange()
        {
            if (Unlock != null && Unlock.Value)
            {
                Settings.ZoomMin = 0.25f;
                Settings.ZoomMax = 7f;
            }
        }

        private static bool DebugZoom()
        {
            if (Unlock == null || !Unlock.Value) return true;
            UnlockRange();
            return false;
        }

        private static void CameraReset(CameraControl __instance)
        {
            if (ReferenceEquals(heldCamera, __instance)) heldCamera = null;
            UnlockRange();
        }

        private static bool Holding(CameraControl camera)
        {
            return Preserve != null && Preserve.Value && ReferenceEquals(heldCamera, camera);
        }

        private static void ConversationBegin(CameraControl __instance, bool __0)
        {
            if (!__0 || Preserve == null || !Preserve.Value || Holding(__instance)) return;
            float value = (float)Actual.GetValue(__instance);
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f) return;
            heldZoom = value;
            heldCamera = __instance;
            RestoreZoom(__instance);
        }

        private static void ConversationEnd(CameraControl __instance, bool __0)
        {
            if (__0 || !Holding(__instance)) return;
            RestoreZoom(__instance);
            heldCamera = null;
        }

        private static void RestoreZoom(CameraControl camera)
        {
            Actual.SetValue(camera, heldZoom);
            Desired.SetValue(camera, heldZoom);
            Original.SetValue(camera, heldZoom);
            EndZoom.SetValue(camera, heldZoom);
            Controlled.SetValue(camera, false);
            HardFocus.SetValue(camera, false);
            // Discard dialogue zoom commands instead of replaying them when dialogue closes.
            Trigger.SetValue(camera, 0f);
        }

        private static bool AllowAutomaticZoom(CameraControl __instance)
        {
            return !Holding(__instance);
        }

        private static void BeforeZoomRender(CameraControl __instance)
        {
            if (Holding(__instance)) RestoreZoom(__instance);
        }

        private static void PointOfInterestZoom(CameraControl __instance, ref float __result)
        {
            if (Holding(__instance)) __result = heldZoom;
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            heldCamera = null;
        }
    }
}
