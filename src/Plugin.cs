using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using DigitalVikings.XpandedPortal.Configuration;

namespace DigitalVikings.XpandedPortal
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.SoftDependency)]
    public class XpandedPortalPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.digitalvikings.xpandedportal";
        public const string ModName = "XpandedPortal";
        public const string ModVersion = "1.1.1";

        internal static ManualLogSource Log = null!;
        private Harmony _harmony = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{ModName} v{ModVersion} initializing...");

            MigrateLegacyConfig();

            // Initialize configuration
            ModConfig.Init(Config);

            // Apply Harmony patches
            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{ModName} v{ModVersion} loaded successfully.");

            // Listen for server synchronization events if Jotunn is active
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
            {
                HookJotunnSync();
            }
        }

        private void MigrateLegacyConfig()
        {
            try
            {
                string oldCfg = Path.Combine(Paths.ConfigPath, "com.user.xpandedportal.cfg");
                string newCfg = Path.Combine(Paths.ConfigPath, $"{ModGuid}.cfg");
                if (File.Exists(oldCfg) && !File.Exists(newCfg))
                {
                    File.Copy(oldCfg, newCfg);
                    Log.LogInfo($"Migrated legacy configuration from '{oldCfg}' to '{newCfg}'.");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogWarning($"Failed to migrate legacy configuration: {ex.Message}");
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HookJotunnSync()
        {
            Jotunn.Managers.SynchronizationManager.OnConfigurationSynchronized += (sender, args) =>
            {
                Log.LogInfo("Server configuration synchronized successfully.");
            };
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
