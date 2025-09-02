using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using ResoniteModLoader;

namespace BetterInspectorCheckbox;

public class BetterInspectorCheckbox : ResoniteMod
{
    internal const string VERSION_CONSTANT = "1.0.2";
    public override string Name => "BetterInspectorCheckbox";
    public override string Author => "NepuShiro";
    public override string Version => VERSION_CONSTANT;
    public override string Link => "https://github.com/NepuShiro/BetterInspectorCheckbox/";

    [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> Checkbox = new ModConfigurationKey<bool>("Checkbox", "Enable the Checkbox?", () => true);
    [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> BetterVisual = new ModConfigurationKey<bool>("BetterVisual", "Use the better Visual?", () => true);

    private static ModConfiguration _config;

    public override void OnEngineInit()
    {
        _config = GetConfiguration();
        _config!.Save(true);

        Harmony harmony = new Harmony("net.NepuShiro.BetterInspectorCheckbox");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(SlotInspector), "OnChanges")]
    public static class SlotInspector_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            FieldInfo rootSlot = AccessTools.Field(typeof(SlotInspector), "_rootSlot");
            MethodInfo booleanEditor = AccessTools.Method(typeof(UIBuilderEditors), nameof(UIBuilderEditors.BooleanMemberEditor), new Type[] { typeof(UIBuilder), typeof(IField), typeof(string) });
            MethodInfo customMethod = AccessTools.Method(typeof(SlotInspector_Patch), nameof(SlotInspector_Patch.BooleanMemberEditorChanger));

            CodeMatcher matcher = new CodeMatcher(instructions, generator);
            matcher.MatchStartForward(new CodeMatch(OpCodes.Ldloc_0), new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld, rootSlot));

            int start = matcher.Pos;

            matcher.MatchEndForward(new CodeMatch(OpCodes.Call, booleanEditor), new CodeMatch(OpCodes.Pop));

            int end = matcher.Pos + 1;

            matcher.Start().Advance(start).RemoveInstructions(end - start).Insert(new CodeInstruction(OpCodes.Ldloc_0), new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, customMethod));

            return matcher.InstructionEnumeration();
        }

        public static void BooleanMemberEditorChanger(UIBuilder uIBuilder, SlotInspector inspector)
        {
            if (!_config.GetValue(Checkbox)) return;

            if (inspector.GetSyncMember("_rootSlot") is not SyncRef<Slot> rootSlot) return;

            if (_config.GetValue(BetterVisual))
            {
                uIBuilder.Style.MinHeight = 24f;
                uIBuilder.Style.MinWidth = 24f;
                uIBuilder.Style.FlexibleHeight = 1f;
            }

            uIBuilder.BooleanMemberEditor(rootSlot.Target.ActiveSelf_Field);
            uIBuilder.Style.FlexibleWidth = 100f;
        }
    }
}