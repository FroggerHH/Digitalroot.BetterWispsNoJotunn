using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using ServerSync;
using UnityEngine;

namespace BetterWispsNoJotunn;

[BepInPlugin(ModGUID, ModName, ModVersion)]
internal class Plugin : BaseUnityPlugin
{
    #region values

    internal const string ModName = "Digitalroot.Frogger.BetterWispsNoJotunn", ModVersion = "1.1.0", ModGUID = "com." + ModName;
    internal static Plugin _self;

    #endregion

    private void Awake()
    {
        _self = this;

        Config.SaveOnConfigSet = false;
        
        configSync.AddLockingConfigEntry(config("Main", "Lock Configuration", true,
            "If on, the configuration is locked and can be changed by server admins only."));
        
        baseRangeConfig = config("General", "Base Range", 15f,
            new ConfigDescription("Base clear range of the Wisp Light.", new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = 11 }));
        increasedRangePerLevelConfig = config("General", "Increased Range Per Level", 5f,
            new ConfigDescription("How much the clear range is Increased per level of the Wisp Light.",
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = 10 }));
        maxLevelConfig = config("Advanced", "Max Level", 5,
            new ConfigDescription("Max level of the Wisp Light.", new AcceptableValueRange<int>(1, 25),
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4 }));
        wispsPerLevelConfig = config("Advanced", "Wisps Per Level", 5,
            new ConfigDescription("Amount of Wisps needed per level.", new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
        silverPerLevelConfig = config("Advanced", "Silver Per Level", 10,
            new ConfigDescription("Amount of Silver needed per level.", new AcceptableValueRange<int>(1, 50),
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

        SetupWatcher();
        Config.ConfigReloaded += (_, _) => UpdateConfiguration();
        Config.SaveOnConfigSet = true;
        Config.Save();

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);
    }

    #region tools

    public static void Debug(object msg) => _self.Logger.LogInfo(msg);

    public static void DebugError(object msg, bool showWriteToDev = true)
    {
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";

        _self.Logger.LogError(msg);
    }

    public static void DebugWarning(object msg, bool showWriteToDev = false)
    {
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";

        _self.Logger.LogWarning(msg);
    }

    #endregion

    #region ConfigSettings

    #region tools

    private static readonly string ConfigFileName = $"{ModGUID}.cfg";
    private DateTime LastConfigChange;

    public static readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        var configEntry = _self.Config.Bind(group, name, value, description);

        var syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private void SetupWatcher()
    {
        FileSystemWatcher fileSystemWatcher = new(Paths.ConfigPath, ConfigFileName);
        fileSystemWatcher.Changed += ConfigChanged;
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 2) return;

        LastConfigChange = DateTime.Now;
        try
        {
            Config.Reload();
        }
        catch
        {
            DebugError("Can't reload Config");
        }
    }

    internal void ConfigChanged()
    {
        ConfigChanged(null, null);
    }

    #endregion

    #region configs

    public static ConfigEntry<float> baseRangeConfig;
    public static ConfigEntry<float> increasedRangePerLevelConfig;
    public static ConfigEntry<int> maxLevelConfig;
    public static ConfigEntry<int> wispsPerLevelConfig;
    public static ConfigEntry<int> silverPerLevelConfig;

    public static float baseRange;
    public static float increasedRangePerLevel;
    public static int maxLevel;
    public static int wispsPerLevel;
    public static int silverPerLevel;

    #endregion

    internal void UpdateConfiguration()
    {
        try
        {
            baseRange = baseRangeConfig.Value;
            increasedRangePerLevel = increasedRangePerLevelConfig.Value;
            maxLevel = maxLevelConfig.Value;
            wispsPerLevel = wispsPerLevelConfig.Value;
            silverPerLevel = silverPerLevelConfig.Value;
            UpdateWisp();
            Debug("Configuration received");
        }
        catch (Exception e)
        {
            DebugError(e.Message, false);
        }
    }

    #endregion

    private static void UpdateWisp()
    {
        try
        {
            foreach (var recipe in ObjectDB.instance.m_recipes.Where(r => r.m_item?.name == "Demister"))
            {
                var wispRequirement =
                    recipe.m_resources.FirstOrDefault(r => r.m_resItem.name == "Wisp");
                if (wispRequirement != null) wispRequirement.m_amountPerLevel = wispsPerLevel;
                Debug($"Updated {recipe.m_item.name} of {recipe.name}, " +
                      $"set {wispRequirement?.m_resItem.name} m_amountPerLevel " +
                      $"to {wispRequirement?.m_amountPerLevel}");

                var silverRequirement = recipe.m_resources.FirstOrDefault(r =>
                    r.m_resItem = ObjectDB.instance.GetItemPrefab("Silver").GetComponent<ItemDrop>());
                if (silverRequirement != null) silverRequirement.m_amountPerLevel = silverPerLevel;
                Debug($"Updated {recipe.m_item.name} of {recipe.name}, " +
                      $"set {silverRequirement?.m_resItem.name} m_amountPerLevel " +
                      $"to {silverRequirement?.m_amountPerLevel}");
            }

            var wispLight = ObjectDB.instance.GetItemPrefab("Demister");
            var itemDrop = wispLight?.GetComponent<ItemDrop>();

            if (!itemDrop) return;
            itemDrop.m_itemData.m_shared.m_maxQuality = maxLevel;
            Debug($"Updated {wispLight.name} set m_maxQuality to {itemDrop.m_itemData.m_shared.m_maxQuality}");
        }
        catch (Exception e)
        {
            DebugError(e, false);
        }
    }
}