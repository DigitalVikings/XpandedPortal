using HarmonyLib;
using DigitalVikings.XpandedPortal.Configuration;
using DigitalVikings.XpandedPortal.UI;

namespace DigitalVikings.XpandedPortal.Patches
{
    /// <summary>
    /// Harmony hooks for custom portal UI interception and routing management.
    /// </summary>
    [HarmonyPatch]
    public static class UIPatches
    {
        /// <summary>
        /// Initializes the UI panel on game startup.
        /// </summary>
        [HarmonyPatch(typeof(Game), "Awake")]
        [HarmonyPostfix]
        private static void OnGameAwake()
        {
            PortalConfigPanel.Initialize();
        }

        /// <summary>
        /// Intercepts portal interaction to open PortalConfigPanel when custom UI is enabled.
        /// Falls back completely to vanilla TextInput when both features are disabled.
        /// </summary>
        [HarmonyPatch(typeof(TextInput), nameof(TextInput.RequestText))]
        [HarmonyPrefix]
        private static bool PrefixRequestText(TextReceiver sign)
        {
            if (!ModConfig.ModEnabled.Value)
                return true;

            // If either Target Selection or Password System is enabled, show custom UI
            if ((ModConfig.EnableTargetSelectionUI.Value || ModConfig.EnablePasswordSystem.Value) &&
                sign is TeleportWorld portal)
            {
                if (PortalConfigPanel.Instance == null)
                {
                    PortalConfigPanel.Initialize();
                }

                PortalConfigPanel.Instance?.Show(portal);
                return false; // Suppress vanilla text dialog
            }

            return true; // State 1: Fall back 100% to vanilla UI
        }

        /// <summary>
        /// When target selection UI is active, prevents vanilla Game.ConnectPortals from
        /// overwriting player-dialed destinations based on matching tags.
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.ConnectPortals))]
        [HarmonyPrefix]
        private static bool PrefixConnectPortals()
        {
            if (ModConfig.ModEnabled.Value && ModConfig.EnableTargetSelectionUI.Value)
            {
                // Bypass automatic tag pairing; preserve manual dial routes
                return false;
            }

            return true;
        }
    }
}
