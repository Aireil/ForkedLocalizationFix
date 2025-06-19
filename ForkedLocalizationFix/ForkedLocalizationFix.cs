using System;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace ForkedLocalizationFix;

public sealed unsafe class ForkedLocalizationFix : IDalamudPlugin
{
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    private readonly nint goodRowPtr;
    private readonly nint badRowPtr;

    public ForkedLocalizationFix()
    {
        var gameVersion = Framework.Instance()->GameVersionString;
        var luminaGameVersion = DataManager.GameData.Repositories["ffxiv"].Version;
        const string targetGameVersion = "2025.06.10.0000.0000";

        if (gameVersion != targetGameVersion || luminaGameVersion != targetGameVersion)
        {
            return;
        }

        GameInteropProvider.InitializeFromAttributes(this);

        this.goodRowPtr = getActionIdRowPtr(41549);
        this.badRowPtr = getActionIdRowPtr(41547);

        this.resolveStringHook?.Enable();
    }

    public void Dispose()
    {
        this.resolveStringHook?.Dispose();
    }

    [Signature("E8 ?? ?? ?? ?? 80 FB 12")]
    private static delegate* unmanaged<uint, nint> getActionIdRowPtr;

    [Signature("E8 ?? ?? ?? ?? 8D 2C 5B", DetourName = nameof(ResolveStringDetour))]
    private Hook<ResolveStringDelegate> resolveStringHook;
    private delegate long ResolveStringDelegate(nint a1);
    private long ResolveStringDetour(nint a1)
    {
        var ret = 0L;

        try
        {
            ret = this.resolveStringHook.Original(a1 == badRowPtr ? goodRowPtr : a1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in ResolveStringDetour");
        }

        return ret;
    }
}
