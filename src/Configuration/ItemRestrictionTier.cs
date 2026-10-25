namespace PortalTweaks.Configuration
{
    /// <summary>
    /// Progressive filtering tiers dictating what items players can carry through portals.
    /// </summary>
    public enum ItemRestrictionTier
    {
        /// <summary>
        /// Portals cannot be used at all.
        /// </summary>
        NoTeleport,

        /// <summary>
        /// Players can only teleport if their inventory is completely empty (0 items).
        /// </summary>
        Naked,

        /// <summary>
        /// Players can only carry items actively equipped on their person (worn armor, wielded weapons/tools, active utility).
        /// </summary>
        EquippedOnly,

        /// <summary>
        /// Players can carry any equipment items (weapons, armor, tools, shields, utility), equipped or stored in bag,
        /// but no resources, materials, or consumables.
        /// </summary>
        EquipmentOnly,

        /// <summary>
        /// Players can carry all equipment, consumables (food, potions, mead), and ammo (arrows/bolts),
        /// but no raw materials, trophies, or loose loot.
        /// </summary>
        GearAndProvisions,

        /// <summary>
        /// Standard Valheim rules: materials and gear allowed, but ores, ingots, and dragon eggs are blocked.
        /// </summary>
        Vanilla,

        /// <summary>
        /// All items are permitted through portals, including ores and metals.
        /// </summary>
        Unrestricted
    }
}
