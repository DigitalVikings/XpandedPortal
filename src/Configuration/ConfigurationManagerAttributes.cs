namespace DigitalVikings.XpandedPortal.Configuration
{
    /// <summary>
    /// Custom attributes recognized via reflection by BepInEx ConfigurationManager and other in-game config editors.
    /// Used to specify display order, categories, and UI visibility.
    /// </summary>
    public class ConfigurationManagerAttributes
    {
        /// <summary>
        /// Show this setting in the settings list at all? If false, only appears in the "Advanced" view.
        /// </summary>
        public bool? Browsable;

        /// <summary>
        /// Category the setting is under. Null means use the section name from ConfigFile.Bind.
        /// </summary>
        public string? Category;

        /// <summary>
        /// If set, a button will be displayed next to the setting in the editor to reset the setting to its default value.
        /// </summary>
        public bool? DefaultValue;

        /// <summary>
        /// Force the "Reset" button to not be displayed, even if a valid default value exists.
        /// </summary>
        public bool? HideDefaultButton;

        /// <summary>
        /// Force the setting name to not be displayed, only its value.
        /// </summary>
        public bool? HideSettingName;

        /// <summary>
        /// Optional description for the setting. If null, the description from ConfigDescription is used.
        /// </summary>
        public string? Description;

        /// <summary>
        /// Order of the setting in the list. Higher numbers are displayed first.
        /// </summary>
        public int? Order;

        /// <summary>
        /// Only show the value, don't allow editing it.
        /// </summary>
        public bool? ReadOnly;

        /// <summary>
        /// If true, don't show the setting by default. User has to turn on "Advanced settings" to see it.
        /// </summary>
        public bool? IsAdvanced;

        /// <summary>
        /// If true, this setting is synchronized from server to clients in multiplayer,
        /// and can only be modified by server admins. Recognized by Jotunn and ConfigurationManager.
        /// </summary>
        public bool? IsAdminOnly;
    }
}
