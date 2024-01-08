using AetherBox.FeaturesSetup;
using AetherBox.Helpers;
using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#nullable disable
namespace AetherBox.Features;

public abstract class BaseFeature
{
    public FeatureProvider Provider { get; private set; }
    protected AetherBox Plugin;
    protected DalamudPluginInterface Pi;
    protected Configuration config;
    protected TaskManager TaskManager;
    private uint? jobID = Svc.ClientState.LocalPlayer?.ClassJob.Id;
    public static readonly SeString PandoraPayload;

    public virtual bool Enabled { get; protected set; }

    public abstract string Name { get; }

    public virtual string Key => this.GetType().Name;

    public abstract string Description { get; }

    public uint? JobID
    {
        get => this.jobID;
        set
        {
            if (!value.HasValue)
                return;
            var jobId = this.jobID;
            var nullable = value;
            if ((int)jobId.GetValueOrDefault() == (int)nullable.GetValueOrDefault() && jobId.HasValue == nullable.HasValue)
                return;
            this.jobID = value;
            var onJobChanged = this.OnJobChanged;
            if (onJobChanged == null)
                return;
            onJobChanged(value);
        }
    }

    public event OnJobChangeDelegate OnJobChanged;

    public virtual void Draw()
    {
    }

    public virtual bool Ready { get; protected set; }

    public virtual FeatureType FeatureType { get; }

    public virtual bool isDebug { get; }

    public void InterfaceSetup(AetherBox plugin, DalamudPluginInterface pluginInterface, Configuration config, FeatureProvider fp)
    {
        this.Plugin = plugin;
        this.Pi = pluginInterface;
        this.config = config;
        this.Provider = fp;
        this.TaskManager = new TaskManager();
    }

    public virtual void Setup()
    {
        this.TaskManager.TimeoutSilently = true;
        this.Ready = true;
    }

    public virtual void Enable()
    {
        Svc.Log.Debug("Enabling " + this.Name);
        Svc.Framework.Update += new IFramework.OnUpdateDelegate(this.CheckJob);
        this.Enabled = true;
    }

    private void CheckJob(IFramework framework)
    {
        if (Svc.ClientState.LocalPlayer == null)
            return;
        this.JobID = new uint?(Svc.ClientState.LocalPlayer.ClassJob.Id);
    }

    public virtual void Disable()
    {
        Svc.Framework.Update -= new IFramework.OnUpdateDelegate(this.CheckJob);
        this.Enabled = false;
    }

    public virtual void Dispose() => this.Ready = false;

    protected T LoadConfig<T>() where T : FeatureConfig => LoadConfig<T>(Key);

    protected T LoadConfig<T>(string key) where T : FeatureConfig
    {
        try
        {
            var path = Path.Combine(AetherBox.PluginInterface.GetPluginConfigDirectory(), key + ".json");
            return !File.Exists(path) ? default(T) : JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            var messageTemplate = "Failed to load config for feature " + Name;
            var objArray = Array.Empty<object>();
            Svc.Log.Error(ex, messageTemplate, objArray);
            return default(T);
        }
    }

    protected void SaveConfig<T>(T config) where T : FeatureConfig
    {
        SaveConfig(config, this.Key);
    }

    protected void SaveConfig<T>(T config, string key) where T : FeatureConfig
    {
        try
        {
            File.WriteAllText(Path.Combine(AetherBox.PluginInterface.GetPluginConfigDirectory(), key + ".json"), JsonConvert.SerializeObject((object)config, Formatting.Indented));
        }
        catch (Exception ex)
        {
            var messageTemplate = "Feature failed to write config " + Name;
            var objArray = Array.Empty<object>();
            Svc.Log.Error(ex, messageTemplate, objArray);
        }
    }

    private void DrawAutoConfig()
    {
        var flag1 = false;
        try
        {
            var config =  
                 GetType().GetProperties()
                .FirstOrDefault( p => p.PropertyType.IsSubclassOf(typeof (FeatureConfig)))
                .GetValue( this);
            var orderedEnumerable =  config.GetType().GetFields().Where( f => f.GetCustomAttribute(typeof (FeatureConfigOptionAttribute)) != null).Select((Func<FieldInfo, (FieldInfo, FeatureConfigOptionAttribute)>) (f => (f, (FeatureConfigOptionAttribute) f.GetCustomAttribute(typeof (FeatureConfigOptionAttribute))))).OrderBy( a => a.Item2.Priority).ThenBy( a => a.Item2.Name);
            var num = 0;
            foreach ((var fieldInfo, var configOptionAttribute) in (IEnumerable<(FieldInfo, FeatureConfigOptionAttribute)>)orderedEnumerable)
            {
                if (configOptionAttribute.ConditionalDisplay)
                {
                    var method = config.GetType().GetMethod("ShouldShow" + fieldInfo.Name, BindingFlags.Instance | BindingFlags.Public);
                    if (method != null && !(bool)(method.Invoke(config, []) ?? true))
                        continue;
                }
                if (configOptionAttribute.SameLine)
                    ImGui.SameLine();
                DefaultInterpolatedStringHandler interpolatedStringHandler;
                if (configOptionAttribute.Editor != null)
                {
                    var obj = fieldInfo.GetValue(config);
                    var objArray = new object[2];
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 4);
                    interpolatedStringHandler.AppendFormatted(configOptionAttribute.Name);
                    interpolatedStringHandler.AppendLiteral("##");
                    interpolatedStringHandler.AppendFormatted(fieldInfo.Name);
                    interpolatedStringHandler.AppendLiteral("_");
                    interpolatedStringHandler.AppendFormatted(this.GetType().Name);
                    interpolatedStringHandler.AppendLiteral("_");
                    interpolatedStringHandler.AppendFormatted(num++);
                    objArray[0] = (object)interpolatedStringHandler.ToStringAndClear();
                    objArray[1] = obj;
                    var parameters = objArray;
                    if ((bool)configOptionAttribute.Editor.Invoke(null, parameters))
                    {
                        flag1 = true;
                        fieldInfo.SetValue(config, parameters[1]);
                    }
                }
                else if (fieldInfo.FieldType == typeof(bool))
                {
                    var v = (bool) fieldInfo.GetValue(config);
                    interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 4);
                    interpolatedStringHandler.AppendFormatted(configOptionAttribute.Name);
                    interpolatedStringHandler.AppendLiteral("##");
                    interpolatedStringHandler.AppendFormatted(fieldInfo.Name);
                    interpolatedStringHandler.AppendLiteral("_");
                    interpolatedStringHandler.AppendFormatted(GetType().Name);
                    interpolatedStringHandler.AppendLiteral("_");
                    interpolatedStringHandler.AppendFormatted(num++);
                    if (ImGui.Checkbox(interpolatedStringHandler.ToStringAndClear(), ref v))
                    {
                        flag1 = true;
                        fieldInfo.SetValue(config, (object)v);
                    }
                }
                else if (fieldInfo.FieldType == typeof(int))
                {
                    var v = (int) fieldInfo.GetValue(config);
                    ImGui.SetNextItemWidth(configOptionAttribute.EditorSize == -1 ? -1f : (float)configOptionAttribute.EditorSize * ImGui.GetIO().FontGlobalScale);
                    bool flag2;
                    switch (configOptionAttribute.IntType)
                    {
                        case FeatureConfigOptionAttribute.NumberEditType.Slider:
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 4);
                            interpolatedStringHandler.AppendFormatted(configOptionAttribute.Name);
                            interpolatedStringHandler.AppendLiteral("##");
                            interpolatedStringHandler.AppendFormatted(fieldInfo.Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(this.GetType().Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(num++);
                            flag2 = ImGui.SliderInt(interpolatedStringHandler.ToStringAndClear(), ref v, configOptionAttribute.IntMin, configOptionAttribute.IntMax);
                            break;
                        case FeatureConfigOptionAttribute.NumberEditType.Drag:
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 4);
                            interpolatedStringHandler.AppendFormatted(configOptionAttribute.Name);
                            interpolatedStringHandler.AppendLiteral("##");
                            interpolatedStringHandler.AppendFormatted(fieldInfo.Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(this.GetType().Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(num++);
                            flag2 = ImGui.DragInt(interpolatedStringHandler.ToStringAndClear(), ref v, 1f, configOptionAttribute.IntMin, configOptionAttribute.IntMax);
                            break;
                        default:
                            flag2 = false;
                            break;
                    }
                    var flag3 = flag2;
                    if (v % configOptionAttribute.IntIncrements != 0)
                    {
                        v = v.RoundOff(configOptionAttribute.IntIncrements);
                        if (v < configOptionAttribute.IntMin)
                            v = configOptionAttribute.IntMin;
                        if (v > configOptionAttribute.IntMax)
                            v = configOptionAttribute.IntMax;
                    }
                    if (configOptionAttribute.EnforcedLimit && v < configOptionAttribute.IntMin)
                    {
                        v = configOptionAttribute.IntMin;
                        flag3 = true;
                    }
                    if (configOptionAttribute.EnforcedLimit && v > configOptionAttribute.IntMax)
                    {
                        v = configOptionAttribute.IntMax;
                        flag3 = true;
                    }
                    if (flag3)
                    {
                        fieldInfo.SetValue(config, (object)v);
                        flag1 = true;
                    }
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    var v = (float) fieldInfo.GetValue(config);
                    ImGui.SetNextItemWidth(configOptionAttribute.EditorSize == -1 ? -1f : configOptionAttribute.EditorSize * ImGui.GetIO().FontGlobalScale);
                    bool flag4;
                    switch (configOptionAttribute.IntType)
                    {
                        case FeatureConfigOptionAttribute.NumberEditType.Slider:
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 4);
                            interpolatedStringHandler.AppendFormatted(configOptionAttribute.Name);
                            interpolatedStringHandler.AppendLiteral("##");
                            interpolatedStringHandler.AppendFormatted(fieldInfo.Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(GetType().Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(num++);
                            flag4 = ImGui.SliderFloat(interpolatedStringHandler.ToStringAndClear(), ref v, configOptionAttribute.FloatMin, configOptionAttribute.FloatMax, configOptionAttribute.Format);
                            break;
                        case FeatureConfigOptionAttribute.NumberEditType.Drag:
                            interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 4);
                            interpolatedStringHandler.AppendFormatted(configOptionAttribute.Name);
                            interpolatedStringHandler.AppendLiteral("##");
                            interpolatedStringHandler.AppendFormatted(fieldInfo.Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(this.GetType().Name);
                            interpolatedStringHandler.AppendLiteral("_");
                            interpolatedStringHandler.AppendFormatted(num++);
                            flag4 = ImGui.DragFloat(interpolatedStringHandler.ToStringAndClear(), ref v, 1f, configOptionAttribute.FloatMin, configOptionAttribute.FloatMax, configOptionAttribute.Format);
                            break;
                        default:
                            flag4 = false;
                            break;
                    }
                    var flag5 = flag4;
                    if ((double)v % (double)configOptionAttribute.FloatIncrements != 0.0)
                    {
                        v = v.RoundOff(configOptionAttribute.FloatIncrements);
                        if ((double)v < (double)configOptionAttribute.FloatMin)
                            v = configOptionAttribute.FloatMin;
                        if ((double)v > (double)configOptionAttribute.FloatMax)
                            v = configOptionAttribute.FloatMax;
                    }
                    if (configOptionAttribute.EnforcedLimit && (double)v < (double)configOptionAttribute.FloatMin)
                    {
                        v = configOptionAttribute.FloatMin;
                        flag5 = true;
                    }
                    if (configOptionAttribute.EnforcedLimit && (double)v > (double)configOptionAttribute.FloatMax)
                    {
                        v = configOptionAttribute.FloatMax;
                        flag5 = true;
                    }
                    if (flag5)
                    {
                        fieldInfo.SetValue(config, (object)v);
                        flag1 = true;
                    }
                }
                else
                    ImGui.Text("Invalid Auto Field Type: " + fieldInfo.Name);
                if (configOptionAttribute.HelpText != null)
                    ImGuiComponents.HelpMarker(configOptionAttribute.HelpText);
            }
            if (!flag1)
                return;
            this.SaveConfig((FeatureConfig)config);
        }
        catch (Exception ex)
        {
            ImGui.Text("Error with AutoConfig: " + ex.Message);
            ImGui.TextWrapped(ex.StackTrace ?? "");
        }
    }

    public virtual bool UseAutoConfig => false;

    public string LocalizedName => this.Name;

    public bool DrawConfig(ref bool hasChanged)
    {
        var flag = false;
        if ((this.UseAutoConfig || this.DrawConfigTree != null) && this.Enabled)
        {
            var cursorPosX = ImGui.GetCursorPosX();
            if (ImGui.TreeNode(this.Name + "##treeConfig_" + this.GetType().Name))
            {
                flag = true;
                ImGui.SetCursorPosX(cursorPosX);
                ImGui.BeginGroup();
                if (this.UseAutoConfig)
                    this.DrawAutoConfig();
                else
                    this.DrawConfigTree(ref hasChanged);
                ImGui.EndGroup();
                ImGui.TreePop();
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0U);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0U);
            ImGui.TreeNodeEx(this.LocalizedName, ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Leaf);
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
        }
        if (hasChanged && this.Enabled)
            this.ConfigChanged();
        return flag;
    }

    protected virtual DrawConfigDelegate DrawConfigTree
    {
        get => (DrawConfigDelegate)null;
    }

    protected virtual void ConfigChanged()
    {
        if (this == null)
            return;
        var propertyInfo = ((IEnumerable<PropertyInfo>) this.GetType().GetProperties()).FirstOrDefault((Func<PropertyInfo, bool>) (p => p.PropertyType.IsSubclassOf(typeof (FeatureConfig))));
        if (!(propertyInfo != (PropertyInfo)null))
            return;
        var config = propertyInfo.GetValue((object) this);
        if (config == null)
            return;
        this.SaveConfig((FeatureConfig)config);
    }

    public unsafe bool IsRpWalking()
    {
        if ((GameObject)Svc.ClientState.LocalPlayer == (GameObject)null || Svc.GameGui.GetAddonByName("_DTR") == IntPtr.Zero)
            return false;
        var addonByName = (AtkUnitBase*) Svc.GameGui.GetAddonByName("_DTR");
        if (addonByName->UldManager.NodeListCount < (ushort)9)
            return false;
        try
        {
            return addonByName->GetNodeById(10U)->IsVisible;
        }
        catch (Exception ex)
        {
            ex.Log();
            return false;
        }
    }

    internal static unsafe int GetInventoryFreeSlotCount()
    {
        var inventoryTypeArray = new InventoryType[4]
  {
    InventoryType.Inventory1,
    InventoryType.Inventory2,
    InventoryType.Inventory3,
    InventoryType.Inventory4
  };
        var inventoryManagerPtr = InventoryManager.Instance();
        var inventoryFreeSlotCount = 0;
        foreach (var inventoryType in inventoryTypeArray)
        {
            var inventoryContainer = inventoryManagerPtr->GetInventoryContainer(inventoryType);
            for (var index = 0; (long)index < (long)inventoryContainer->Size; ++index)
            {
                if (inventoryContainer->Items[index].ItemID == 0U)
                    ++inventoryFreeSlotCount;
            }
        }
        return inventoryFreeSlotCount;
    }

    internal static unsafe bool IsTargetLocked
    {
        get => *(byte*)((IntPtr)TargetSystem.Instance() + new IntPtr(309)) == (byte)1;
    }

    internal static bool IsInventoryFree() => BaseFeature.GetInventoryFreeSlotCount() >= 1;

    public unsafe bool IsMoving() => AgentMap.Instance()->IsPlayerMoving == (byte)1;

    public void PrintModuleMessage(string msg)
    {
        Svc.Chat.Print(new XivChatEntry()
        {
            Message = new SeStringBuilder().AddUiForeground("[" + AetherBox.Name + "] ", (ushort)45).AddUiForeground("[" + this.Name + "] ", (ushort)62).AddText(msg).Build()
        });
    }

    public void PrintModuleMessage(SeString msg)
    {
        Svc.Chat.Print(new XivChatEntry()
        {
            Message = new SeStringBuilder().AddUiForeground("[" + AetherBox.Name + "] ", (ushort)45).AddUiForeground("[" + this.Name + "] ", (ushort)62).Append(msg).Build()
        });
    }

    internal static unsafe AtkUnitBase* GetSpecificYesno(Predicate<string> compare)
    {
        for (var index = 1; index < 100; ++index)
        {
            try
            {
                var addonByName = (AtkUnitBase*) Svc.GameGui.GetAddonByName("SelectYesno", index);
                if ((IntPtr)addonByName == IntPtr.Zero)
                    return (AtkUnitBase*)null;
                if (GenericHelpers.IsAddonReady(addonByName))
                {
                    var text = MemoryHelper.ReadSeString(&addonByName->UldManager.NodeList[15]->GetAsAtkTextNode()->NodeText).ExtractText();
                    if (compare(text))
                    {
                        var interpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
                        interpolatedStringHandler.AppendLiteral("SelectYesno ");
                        interpolatedStringHandler.AppendFormatted(text);
                        interpolatedStringHandler.AppendLiteral(" addon ");
                        interpolatedStringHandler.AppendFormatted(index);
                        interpolatedStringHandler.AppendLiteral(" by predicate");
                        Svc.Log.Debug(interpolatedStringHandler.ToStringAndClear());
                        return addonByName;
                    }
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error("", (object)ex);
                return (AtkUnitBase*)null;
            }
        }
        return (AtkUnitBase*)null;
    }

    internal static unsafe AtkUnitBase* GetSpecificYesno(params string[] s)
    {
        for (var index = 1; index < 100; ++index)
        {
            try
            {
                var addonByName = (AtkUnitBase*) Svc.GameGui.GetAddonByName("SelectYesno", index);
                if ((IntPtr)addonByName == IntPtr.Zero)
                    return (AtkUnitBase*)null;
                if (GenericHelpers.IsAddonReady(addonByName))
                {
                    if (MemoryHelper.ReadSeString(&addonByName->UldManager.NodeList[15]->GetAsAtkTextNode()->NodeText).ExtractText().Replace(" ", "").EqualsAny(((IEnumerable<string>)s).Select((Func<string, string>)(x => x.Replace(" ", "")))))
                    {
                        var interpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
                        interpolatedStringHandler.AppendLiteral("SelectYesno ");
                        interpolatedStringHandler.AppendFormatted(((IEnumerable<string>)s).Print());
                        interpolatedStringHandler.AppendLiteral(" addon ");
                        interpolatedStringHandler.AppendFormatted(index);
                        Svc.Log.Debug(interpolatedStringHandler.ToStringAndClear());
                        return addonByName;
                    }
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error("", (object)ex);
                return (AtkUnitBase*)null;
            }
        }
        return (AtkUnitBase*)null;
    }

    internal static bool TrySelectSpecificEntry(string text, Func<bool> Throttler = null)
    {
        return BaseFeature.TrySelectSpecificEntry((IEnumerable<string>)new string[1]
        {
    text
        }, Throttler);
    }

    internal static unsafe bool TrySelectSpecificEntry(
      IEnumerable<string> text,
      Func<bool> Throttler = null)
    {
        AddonSelectString* AddonPtr;
        if (GenericHelpers.TryGetAddonByName("SelectString", out AddonPtr) && GenericHelpers.IsAddonReady(&AddonPtr->AtkUnitBase))
        {
            var str = BaseFeature.GetEntries(AddonPtr).FirstOrDefault((Func<string, bool>) (x => x.ContainsAny(text)));
            if (str != null)
            {
                var index = BaseFeature.GetEntries(AddonPtr).IndexOf(str);
                if (index >= 0 && BaseFeature.IsSelectItemEnabled(AddonPtr, index) && (Throttler != null ? (Throttler() ? 1 : 0) : (BaseFeature.GenericThrottle ? 1 : 0)) != 0)
                {
                    ClickSelectString.Using((IntPtr)AddonPtr).SelectItem((ushort)index);
                    var interpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 3);
                    interpolatedStringHandler.AppendLiteral("TrySelectSpecificEntry: selecting ");
                    interpolatedStringHandler.AppendFormatted(str);
                    interpolatedStringHandler.AppendLiteral("/");
                    interpolatedStringHandler.AppendFormatted(index);
                    interpolatedStringHandler.AppendLiteral(" as requested by ");
                    interpolatedStringHandler.AppendFormatted(text.Print());
                    Svc.Log.Debug(interpolatedStringHandler.ToStringAndClear());
                    return true;
                }
            }
        }
        else
            BaseFeature.RethrottleGeneric();
        return false;
    }

    internal static unsafe bool IsSelectItemEnabled(AddonSelectString* addon, int index)
    {
        return GenericHelpers.IsSelectItemEnabled((AtkTextNode*)addon->AtkUnitBase.UldManager.NodeList[2]->GetComponent()->UldManager.NodeList[index + 1]->GetComponent()->UldManager.NodeList[3]);
    }

    internal static unsafe List<string> GetEntries(AddonSelectString* addon)
    {
        var entries = new List<string>();
        for (var index = 0; index < addon->PopupMenu.PopupMenu.EntryCount; ++index)
            entries.Add(MemoryHelper.ReadSeStringNullTerminated((IntPtr)addon->PopupMenu.PopupMenu.EntryNames[index]).ExtractText());
        return entries;
    }

    internal static bool GenericThrottle => EzThrottler.Throttle("AetherBoxGenericThrottle", 200);

    internal static void RethrottleGeneric(int num)
    {
        EzThrottler.Throttle("AetherBoxGenericThrottle", num, true);
    }

    internal static void RethrottleGeneric()
    {
        EzThrottler.Throttle("AetherBoxGenericThrottle", 200, true);
    }

    internal static unsafe bool IsLoading()
    {
        AtkUnitBase* AddonPtr1;
        if (GenericHelpers.TryGetAddonByName("FadeBack", out AddonPtr1) && AddonPtr1->IsVisible)
            return true;
        AtkUnitBase* AddonPtr2;
        return GenericHelpers.TryGetAddonByName("FadeMiddle", out AddonPtr2) && AddonPtr2->IsVisible;
    }

    public bool IsInDuty()
    {
        return Svc.Condition[ConditionFlag.BoundByDuty] || Svc.Condition[ConditionFlag.BoundByDuty56] || Svc.Condition[ConditionFlag.BoundByDuty95] || Svc.Condition[ConditionFlag.BoundToDuty97];
    }

    static BaseFeature()
    {
        var seString = new SeString(new Payload[1]
  {
    (Payload) new UIForegroundPayload((ushort) 32)
  });
        var interpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 7);
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterP.ToIconString());
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterA.ToIconString());
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterN.ToIconString());
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterD.ToIconString());
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterO.ToIconString());
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterR.ToIconString());
        interpolatedStringHandler.AppendFormatted(SeIconChar.BoxedLetterA.ToIconString());
        interpolatedStringHandler.AppendLiteral(" ");
        var stringAndClear = (SeString) interpolatedStringHandler.ToStringAndClear();
        BaseFeature.PandoraPayload = seString.Append(stringAndClear).Append((Payload)new UIForegroundPayload((ushort)0));
    }

    public delegate void OnJobChangeDelegate(uint? jobId);

    protected delegate void DrawConfigDelegate(ref bool hasChanged);
}
