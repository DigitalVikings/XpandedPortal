using BepInEx.Configuration;

namespace DigitalVikings.XpandedPortal.Configuration
{
    public static class ModConfig
    {
        // General
        public static ConfigEntry<bool> ModEnabled { get; private set; } = null!;
        public static ConfigEntry<float> PortalLoadTime { get; private set; } = null!;

        // Weight Limit
        public static ConfigEntry<bool> EnableMaxWeightLimit { get; private set; } = null!;
        public static ConfigEntry<float> MaxPortalWeight { get; private set; } = null!;
        public static ConfigEntry<float> StoneMaxPortalWeight { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverWeightWarning { get; private set; } = null!;

        // Item Restrictions
        public static ConfigEntry<ItemRestrictionTier> ItemRestrictionTierConfig { get; private set; } = null!;
        public static ConfigEntry<ItemRestrictionTier> StoneRestrictionTier { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverItemWarning { get; private set; } = null!;

        // Distance Limit
        public static ConfigEntry<bool> EnableDistanceLimit { get; private set; } = null!;
        public static ConfigEntry<float> MaxPortalDistance { get; private set; } = null!;
        public static ConfigEntry<float> StoneMaxPortalDistance { get; private set; } = null!;
        public static ConfigEntry<bool> EnableMinDistanceLimit { get; private set; } = null!;
        public static ConfigEntry<float> MinPortalDistance { get; private set; } = null!;
        public static ConfigEntry<float> StoneMinPortalDistance { get; private set; } = null!;
        public static ConfigEntry<bool> ShowDistanceInHoverText { get; private set; } = null!;

        // UI & Connection Systems
        public static ConfigEntry<bool> EnableTargetSelectionUI { get; private set; } = null!;
        public static ConfigEntry<bool> EnablePasswordSystem { get; private set; } = null!;

        // Portal Proximity Limits
        public static ConfigEntry<bool> EnablePortalProximityLimit { get; private set; } = null!;
        public static ConfigEntry<float> MinDistanceBetweenPortals { get; private set; } = null!;

        // Energy System
        public static ConfigEntry<bool> EnableEnergySystem { get; private set; } = null!;
        public static ConfigEntry<float> MaxPortalEnergy { get; private set; } = null!;
        public static ConfigEntry<float> StoneMaxPortalEnergy { get; private set; } = null!;
        public static ConfigEntry<float> EnergyRechargeRate { get; private set; } = null!;
        public static ConfigEntry<float> StoneEnergyRechargeRate { get; private set; } = null!;
        public static ConfigEntry<float> StartingEnergyPercentage { get; private set; } = null!;
        public static ConfigEntry<float> BaseTeleportCost { get; private set; } = null!;
        public static ConfigEntry<float> StoneBaseTeleportCost { get; private set; } = null!;
        public static ConfigEntry<bool> EnableWeightCostScaling { get; private set; } = null!;
        public static ConfigEntry<bool> EquipmentCostsEnergy { get; private set; } = null!;
        public static ConfigEntry<bool> FoodCostsEnergy { get; private set; } = null!;
        public static ConfigEntry<bool> AmmoCostsEnergy { get; private set; } = null!;
        public static ConfigEntry<bool> EnableGroupConduitDiscount { get; private set; } = null!;
        public static ConfigEntry<float> GroupConduitWindowSeconds { get; private set; } = null!;
        public static ConfigEntry<float> WeightCostFactor { get; private set; } = null!;
        public static ConfigEntry<float> StoneWeightCostFactor { get; private set; } = null!;
        public static ConfigEntry<bool> EnableOreCostScaling { get; private set; } = null!;
        public static ConfigEntry<float> OreCostFactor { get; private set; } = null!;
        public static ConfigEntry<float> StoneOreCostFactor { get; private set; } = null!;
        public static ConfigEntry<bool> EnableDistanceCostScaling { get; private set; } = null!;
        public static ConfigEntry<float> DistanceCostBase { get; private set; } = null!;
        public static ConfigEntry<float> StoneDistanceCostBase { get; private set; } = null!;
        public static ConfigEntry<float> DistanceCostExponent { get; private set; } = null!;
        public static ConfigEntry<float> StoneDistanceCostExponent { get; private set; } = null!;
        public static ConfigEntry<float> MinDistanceCostMultiplier { get; private set; } = null!;
        public static ConfigEntry<LowEnergyBehavior> LowEnergyAction { get; private set; } = null!;
        public static ConfigEntry<float> OverdraftSicknessSecondsPerEnergy { get; private set; } = null!;
        public static bool DeactivateVortexWhenInsufficientEnergy => LowEnergyAction.Value == LowEnergyBehavior.RefuseTeleport;
        public static ConfigEntry<bool> ScalePortalVfxWithEnergy { get; private set; } = null!;
        public static ConfigEntry<bool> ShowHoverEnergyInfo { get; private set; } = null!;

        // Teleportation Sickness
        public static ConfigEntry<bool> EnableTeleportSickness { get; private set; } = null!;
        public static ConfigEntry<float> SicknessBaseDuration { get; private set; } = null!;
        public static ConfigEntry<float> StoneSicknessBaseDuration { get; private set; } = null!;
        public static ConfigEntry<float> SicknessBaseDistance { get; private set; } = null!;
        public static ConfigEntry<float> SicknessDurationExponent { get; private set; } = null!;
        public static ConfigEntry<bool> SicknessBlockTeleport { get; private set; } = null!;
        public static ConfigEntry<float> SicknessDurabilityLossPercent { get; private set; } = null!;
        public static ConfigEntry<float> StoneSicknessDurabilityLossPercent { get; private set; } = null!;
        public static ConfigEntry<float> SicknessCarryWeightReduction { get; private set; } = null!;
        public static ConfigEntry<float> SicknessMovementSpeed { get; private set; } = null!;
        public static ConfigEntry<float> SicknessHealthRegen { get; private set; } = null!;
        public static ConfigEntry<float> SicknessStaminaRegen { get; private set; } = null!;
        public static ConfigEntry<float> SicknessEitrRegen { get; private set; } = null!;

        // Debug
        public static ConfigEntry<bool> LogPortalTeleports { get; private set; } = null!;

        public static void Init(ConfigFile config)
        {
            // ==========================================
            // 1 - General
            // ==========================================
            ModEnabled = Bind(
                config,
                "1 - General", "General",
                "Enabled",
                true,
                "Master switch to enable or disable all features of PortalTweaks.",
                order: 100
            );

            PortalLoadTime = Bind(
                config,
                "1 - General", "General",
                "PortalLoadTime",
                8.0f,
                "Portal teleport load / transition screen duration in seconds. Vanilla default is 8.0 seconds.",
                order: 90,
                acceptableValues: new AcceptableValueRange<float>(0f, 30.0f)
            );

            // ==========================================
            // 2 - Portal UI
            // ==========================================
            EnableTargetSelectionUI = Bind(
                config,
                "2 - Portal UI", "Portal UI",
                "EnableTargetSelectionUI",
                true,
                "If true, interacting with a portal displays a destination selection dropdown to pick outbound portals.",
                order: 100
            );

            EnablePasswordSystem = Bind(
                config,
                "2 - Portal UI", "Portal UI",
                "EnablePasswordSystem",
                false,
                "If true, enables portal passwords. Portals only connect if passwords match.",
                order: 90
            );

            // ==========================================
            // 3 - Portal Proximity
            // ==========================================
            EnablePortalProximityLimit = Bind(
                config,
                "3 - Portal Proximity", "Portal Proximity",
                "EnablePortalProximityLimit",
                false,
                "If true, enforces a minimum distance between any two portals to prevent portal stacking.",
                order: 100
            );

            MinDistanceBetweenPortals = Bind(
                config,
                "3 - Portal Proximity", "Portal Proximity",
                "MinDistanceBetweenPortals",
                400.0f,
                "Minimum distance (in meters) required between any two portals in the world.",
                order: 90,
                acceptableValues: new AcceptableValueRange<float>(0f, 2000f)
            );

            // ==========================================
            // 4 - Item Restrictions
            // ==========================================
            ItemRestrictionTierConfig = Bind(
                config,
                "4 - Item Restrictions", "Item Restrictions",
                "RestrictionTier",
                ItemRestrictionTier.Vanilla,
                "Progressive item filtering tier for standard wood portals:\n" +
                "- NoTeleport: Portals cannot be used at all.\n" +
                "- Naked: Inventory must be 100% empty.\n" +
                "- EquippedOnly: Only actively worn armor and wielded items allowed.\n" +
                "- EquipmentOnly: Weapons, armor, and tools allowed (in bag or equipped), but no materials or food.\n" +
                "- GearAndProvisions: Equipment + food, potions, and ammo allowed, but no raw materials or loot.\n" +
                "- Vanilla: Standard Valheim rules (materials allowed, but ores/metals blocked).\n" +
                "- Unrestricted: All items allowed, including ores and metals.",
                order: 100
            );

            StoneRestrictionTier = Bind(
                config,
                "4 - Item Restrictions", "Item Restrictions",
                "StoneRestrictionTier",
                ItemRestrictionTier.Unrestricted,
                "Progressive item filtering tier for Ashlands Stone Portals. Defaults to Unrestricted.",
                order: 90
            );

            ShowHoverItemWarning = Bind(
                config,
                "4 - Item Restrictions", "Item Restrictions",
                "ShowHoverItemWarning",
                true,
                "If true, displays a warning in the portal hover text when carrying items disallowed by the active tier.",
                order: 80,
                isAdminOnly: false
            );

            // ==========================================
            // 5 - Weight Limit
            // ==========================================
            EnableMaxWeightLimit = Bind(
                config,
                "5 - Weight Limit", "Weight Limit",
                "EnableMaxWeightLimit",
                false,
                "If true, portals only allow players through if their total inventory weight is below MaxPortalWeight.",
                order: 100
            );

            MaxPortalWeight = Bind(
                config,
                "5 - Weight Limit", "Weight Limit",
                "MaxPortalWeight",
                300.0f,
                "Maximum player inventory weight allowed to use wood portals.",
                order: 90,
                acceptableValues: new AcceptableValueRange<float>(0f, 2000f)
            );

            StoneMaxPortalWeight = Bind(
                config,
                "5 - Weight Limit", "Weight Limit",
                "StoneMaxPortalWeight",
                600.0f,
                "Maximum player inventory weight allowed to use Stone Portals.",
                order: 85,
                acceptableValues: new AcceptableValueRange<float>(0f, 2000f)
            );

            ShowHoverWeightWarning = Bind(
                config,
                "5 - Weight Limit", "Weight Limit",
                "ShowHoverWeightWarning",
                true,
                "If true, displays a warning in the portal hover text when carrying too much weight.",
                order: 80,
                isAdminOnly: false
            );

            // ==========================================
            // 6 - Distance Limit
            // ==========================================
            EnableDistanceLimit = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "EnableDistanceLimit",
                false,
                "If true, portals only connect and allow travel if the distance between them is within configured limits.",
                order: 100
            );

            MaxPortalDistance = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "MaxPortalDistance",
                2000.0f,
                "Maximum distance (in meters) allowed between connected wood portals.",
                order: 90,
                acceptableValues: new AcceptableValueRange<float>(50f, 20000f)
            );

            StoneMaxPortalDistance = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "StoneMaxPortalDistance",
                4000.0f,
                "Maximum distance (in meters) allowed between connected Stone Portals.",
                order: 85,
                acceptableValues: new AcceptableValueRange<float>(50f, 20000f)
            );

            EnableMinDistanceLimit = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "EnableMinDistanceLimit",
                false,
                "If true, portals also require a minimum distance between them to activate.",
                order: 80
            );

            MinPortalDistance = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "MinPortalDistance",
                100.0f,
                "Minimum distance (in meters) required between connected wood portals.",
                order: 75,
                acceptableValues: new AcceptableValueRange<float>(0f, 2000f)
            );

            StoneMinPortalDistance = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "StoneMinPortalDistance",
                100.0f,
                "Minimum distance (in meters) required between connected Stone Portals.",
                order: 70,
                acceptableValues: new AcceptableValueRange<float>(0f, 2000f)
            );

            ShowDistanceInHoverText = Bind(
                config,
                "6 - Distance Limit", "Distance Limit",
                "ShowDistanceInHoverText",
                true,
                "If true, displays the distance to the connected portal on the portal hover HUD text.",
                order: 60,
                isAdminOnly: false
            );

            // ==========================================
            // 7 - Energy System
            // ==========================================
            EnableEnergySystem = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EnableEnergySystem",
                false,
                "If true, portals require and consume energy to teleport players, recharging over time.",
                order: 100
            );

            MaxPortalEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "MaxPortalEnergy",
                360.0f,
                "Maximum energy reserve a wood portal can store.",
                order: 95,
                acceptableValues: new AcceptableValueRange<float>(0f, 3000f)
            );

            StoneMaxPortalEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneMaxPortalEnergy",
                1080.0f,
                "Maximum energy reserve a Stone Portal can store.",
                order: 90,
                acceptableValues: new AcceptableValueRange<float>(0f, 3000f)
            );

            EnergyRechargeRate = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EnergyRechargeRate",
                0.1f,
                "Rate at which wood portal energy recharges per second.",
                order: 85,
                acceptableValues: new AcceptableValueRange<float>(0f, 50f)
            );

            StoneEnergyRechargeRate = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneEnergyRechargeRate",
                0.2f,
                "Rate at which Stone Portal energy recharges per second.",
                order: 80,
                acceptableValues: new AcceptableValueRange<float>(0f, 50f)
            );

            StartingEnergyPercentage = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StartingEnergyPercentage",
                0.0f,
                "Starting energy percentage (0-100%) when a portal is newly placed or first discovered. Defaults to 0%.",
                order: 75,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            BaseTeleportCost = Bind(
                config,
                "7 - Energy System", "Energy System",
                "BaseTeleportCost",
                15.0f,
                "Base fixed energy cost deducted per wood portal teleport.",
                order: 70,
                acceptableValues: new AcceptableValueRange<float>(0f, 500f)
            );

            StoneBaseTeleportCost = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneBaseTeleportCost",
                15.0f,
                "Base fixed energy cost deducted per teleport on Stone Portals.",
                order: 65,
                acceptableValues: new AcceptableValueRange<float>(0f, 500f)
            );

            EnableWeightCostScaling = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EnableWeightCostScaling",
                true,
                "If true, player inventory carry weight adds additional energy cost to the teleport.",
                order: 60
            );

            EquipmentCostsEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EquipmentCostsEnergy",
                false,
                "If true, equipment (weapons, armor, tools, and shields) in inventory contributes to teleport weight energy cost. Defaults to false.",
                order: 58
            );

            FoodCostsEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "FoodCostsEnergy",
                true,
                "If true, food and consumable items in inventory contribute to teleport weight energy cost. Defaults to true.",
                order: 57
            );

            AmmoCostsEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "AmmoCostsEnergy",
                true,
                "If true, arrows, bolts, and ammunition in inventory contribute to teleport weight energy cost. Defaults to true.",
                order: 56
            );

            EnableGroupConduitDiscount = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EnableGroupConduitDiscount",
                true,
                "If true, players jumping through a portal within the conduit window after another player receive a 100% discount on base teleport energy cost.",
                order: 55
            );

            GroupConduitWindowSeconds = Bind(
                config,
                "7 - Energy System", "Energy System",
                "GroupConduitWindowSeconds",
                30.0f,
                "Duration in seconds after a player teleports that the destination conduit remains active for subsequent players. Defaults to 30 seconds.",
                order: 54,
                acceptableValues: new AcceptableValueRange<float>(0f, 300f)
            );

            WeightCostFactor = Bind(
                config,
                "7 - Energy System", "Energy System",
                "WeightCostFactor",
                0.1f,
                "Additional energy cost per unit of inventory weight carried on wood portals.",
                order: 53,
                acceptableValues: new AcceptableValueRange<float>(0f, 2.0f)
            );

            StoneWeightCostFactor = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneWeightCostFactor",
                0.1f,
                "Additional energy cost per unit of inventory weight carried on Stone Portals.",
                order: 50,
                acceptableValues: new AcceptableValueRange<float>(0f, 2.0f)
            );

            EnableOreCostScaling = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EnableOreCostScaling",
                true,
                "If true, carrying non-teleportable items (ores/metals) adds an additional energy surcharge per weight unit.",
                order: 45
            );

            OreCostFactor = Bind(
                config,
                "7 - Energy System", "Energy System",
                "OreCostFactor",
                0.9f,
                "Extra energy cost per unit of non-teleportable ore/metal weight in inventory for wood portals.",
                order: 40,
                acceptableValues: new AcceptableValueRange<float>(0f, 5.0f)
            );

            StoneOreCostFactor = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneOreCostFactor",
                0.0f,
                "Extra energy cost per unit of non-teleportable ore/metal weight on Stone Portals.",
                order: 35,
                acceptableValues: new AcceptableValueRange<float>(0f, 5.0f)
            );

            EnableDistanceCostScaling = Bind(
                config,
                "7 - Energy System", "Energy System",
                "EnableDistanceCostScaling",
                true,
                "If true, scales the teleport energy cost based on the distance between portals.",
                order: 30
            );

            DistanceCostBase = Bind(
                config,
                "7 - Energy System", "Energy System",
                "DistanceCostBase",
                1000.0f,
                "Base distance (in meters) for cost scaling on wood portals: mult(dist) = (dist / base)^exponent.",
                order: 25,
                acceptableValues: new AcceptableValueRange<float>(0f, 20000f)
            );

            StoneDistanceCostBase = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneDistanceCostBase",
                1000.0f,
                "Base distance (in meters) for cost scaling on Stone Portals.",
                order: 22,
                acceptableValues: new AcceptableValueRange<float>(0f, 20000f)
            );

            DistanceCostExponent = Bind(
                config,
                "7 - Energy System", "Energy System",
                "DistanceCostExponent",
                1.5f,
                "Exponent for distance multiplier formula on wood portals: mult(dist) = (dist / base)^exponent.",
                order: 20,
                acceptableValues: new AcceptableValueRange<float>(0f, 4.0f)
            );

            StoneDistanceCostExponent = Bind(
                config,
                "7 - Energy System", "Energy System",
                "StoneDistanceCostExponent",
                1.0f,
                "Exponent for distance multiplier formula on Stone Portals.",
                order: 18,
                acceptableValues: new AcceptableValueRange<float>(0f, 4.0f)
            );

            MinDistanceCostMultiplier = Bind(
                config,
                "7 - Energy System", "Energy System",
                "MinDistanceCostMultiplier",
                0.25f,
                "Minimum floor for the distance cost multiplier. Defaults to 0.25.",
                order: 15,
                acceptableValues: new AcceptableValueRange<float>(0f, 10.0f)
            );

            LowEnergyAction = Bind(
                config,
                "7 - Energy System", "Energy System",
                "LowEnergyAction",
                LowEnergyBehavior.RefuseTeleport,
                "Behavior when attempting to teleport through a portal with insufficient energy:\n" +
                "- RefuseTeleport: Overdraft disabled; portal rejects the jump until recharged.\n" +
                "- ExtendSickness: Overdraft enabled; drains energy to 0 and extends Teleportation Sickness duration.",
                order: 12
            );

            OverdraftSicknessSecondsPerEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "OverdraftSicknessSecondsPerEnergy",
                10.0f,
                "Additional Teleportation Sickness seconds added per point of energy deficit when overdrafting.",
                order: 10,
                acceptableValues: new AcceptableValueRange<float>(0f, 120.0f)
            );

            ScalePortalVfxWithEnergy = Bind(
                config,
                "7 - Energy System", "Energy System",
                "ScalePortalVfxWithEnergy",
                true,
                "If true, scales the emission brightness of the portal arch based on current energy reserve (dim when empty, glowing brighter as it recharges).",
                order: 5,
                isAdminOnly: false
            );

            ShowHoverEnergyInfo = Bind(
                config,
                "7 - Energy System", "Energy System",
                "ShowHoverEnergyInfo",
                true,
                "If true, displays portal energy reserve and recharge status on the portal hover HUD text.",
                order: 2,
                isAdminOnly: false
            );

            // ==========================================
            // 8 - Teleportation Sickness
            // ==========================================
            EnableTeleportSickness = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "EnableTeleportSickness",
                false,
                "If true, imposes a temporary debuff on players upon teleporting.",
                order: 100
            );

            SicknessBlockTeleport = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessBlockTeleport",
                true,
                "If true, prevents players from using portals while affected by teleportation sickness.",
                order: 95
            );

            SicknessBaseDuration = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessBaseDuration",
                180.0f,
                "Base debuff duration (in seconds) at base distance for wood portals: duration(dist) = base_duration * (dist / base_dist)^exponent.",
                order: 90,
                acceptableValues: new AcceptableValueRange<float>(0f, 600f)
            );

            StoneSicknessBaseDuration = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "StoneSicknessBaseDuration",
                120.0f,
                "Base debuff duration (in seconds) at base distance for Stone Portals.",
                order: 85,
                acceptableValues: new AcceptableValueRange<float>(0f, 600f)
            );

            SicknessBaseDistance = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessBaseDistance",
                1000.0f,
                "Base distance (in meters) for sickness duration scaling.",
                order: 80,
                acceptableValues: new AcceptableValueRange<float>(0f, 20000f)
            );

            SicknessDurationExponent = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessDurationExponent",
                0.5f,
                "Exponent for sickness duration scaling: duration(dist) = base_duration * (dist / base_dist)^exponent.",
                order: 75,
                acceptableValues: new AcceptableValueRange<float>(0f, 5.0f)
            );

            SicknessDurabilityLossPercent = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessDurabilityLossPercent",
                10.0f,
                "Instant percentage of max durability damage dealt to all carried and equipped gear upon wood portal jump.",
                order: 70,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            StoneSicknessDurabilityLossPercent = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "StoneSicknessDurabilityLossPercent",
                10.0f,
                "Instant percentage of max durability damage dealt to carried and equipped gear upon teleporting through a Stone Portal.",
                order: 65,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            SicknessCarryWeightReduction = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessCarryWeightReduction",
                100.0f,
                "Absolute reduction to maximum carrying capacity while debuffed.",
                order: 60,
                acceptableValues: new AcceptableValueRange<float>(0f, 300f)
            );

            SicknessMovementSpeed = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessMovementSpeed",
                85.0f,
                "Movement speed percentage while debuffed (0 = immobile, 100 = normal speed, 90 = 90% speed).",
                order: 50,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            SicknessHealthRegen = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessHealthRegen",
                60.0f,
                "Health regeneration percentage while debuffed (0 = no regen, 100 = normal regen, 50 = 50% regen).",
                order: 40,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            SicknessStaminaRegen = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessStaminaRegen",
                60.0f,
                "Stamina regeneration percentage while debuffed (0 = no regen, 100 = normal regen, 50 = 50% regen).",
                order: 30,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            SicknessEitrRegen = Bind(
                config,
                "8 - Teleportation Sickness", "Teleportation Sickness",
                "SicknessEitrRegen",
                60.0f,
                "Eitr regeneration percentage while debuffed (0 = no regen, 100 = normal regen, 50 = 50% regen).",
                order: 20,
                acceptableValues: new AcceptableValueRange<float>(0f, 100f)
            );

            // ==========================================
            // 9 - Debug
            // ==========================================
            LogPortalTeleports = Bind(
                config,
                "9 - Debug", "Debug",
                "LogPortalTeleports",
                false,
                "Log information to BepInEx console whenever a player activates a portal.",
                order: 100,
                isAdminOnly: false
            );
        }

        private static ConfigEntry<T> Bind<T>(
            ConfigFile config,
            string newSection,
            string oldSection,
            string key,
            T defaultValue,
            string description,
            int order,
            AcceptableValueBase? acceptableValues = null,
            bool isAdminOnly = true)
        {
            var oldDef = new ConfigDefinition(oldSection, key);
            T actualDefault = defaultValue;
            if (config.TryGetEntry<T>(oldDef, out var oldEntry))
            {
                actualDefault = oldEntry.Value;
                config.Remove(oldDef);
            }

            var tags = new ConfigurationManagerAttributes
            {
                Order = order,
                Category = newSection,
                IsAdminOnly = isAdminOnly
            };
            var configDesc = new ConfigDescription(description, acceptableValues, tags);
            return config.Bind(newSection, key, actualDefault, configDesc);
        }

        #region Portal Type Helpers

        /// <summary>
        /// Determines whether a TeleportWorld is an Ashlands Stone Portal.
        /// Evaluates vanilla m_allowAllItems (true on portal_stone) and prefab/gameobject naming.
        /// </summary>
        public static bool IsStonePortal(TeleportWorld? portal)
        {
            if (portal == null)
                return false;

            if (portal.m_allowAllItems)
                return true;

            string name = portal.gameObject.name;
            return name.IndexOf("stone", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static float GetMaxWeight(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneMaxPortalWeight.Value : MaxPortalWeight.Value;

        public static float GetMaxWeight(bool isStone) =>
            isStone ? StoneMaxPortalWeight.Value : MaxPortalWeight.Value;

        public static ItemRestrictionTier GetRestrictionTier(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneRestrictionTier.Value : ItemRestrictionTierConfig.Value;

        public static ItemRestrictionTier GetRestrictionTier(bool isStone) =>
            isStone ? StoneRestrictionTier.Value : ItemRestrictionTierConfig.Value;

        public static float GetMaxDistance(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneMaxPortalDistance.Value : MaxPortalDistance.Value;

        public static float GetMinDistance(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneMinPortalDistance.Value : MinPortalDistance.Value;

        public static float GetMaxEnergy(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneMaxPortalEnergy.Value : MaxPortalEnergy.Value;

        public static float GetRechargeRate(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneEnergyRechargeRate.Value : EnergyRechargeRate.Value;

        public static float GetBaseCost(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneBaseTeleportCost.Value : BaseTeleportCost.Value;

        public static float GetWeightCostFactor(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneWeightCostFactor.Value : WeightCostFactor.Value;

        public static float GetOreCostFactor(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneOreCostFactor.Value : OreCostFactor.Value;

        public static float GetDistanceCostBase(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneDistanceCostBase.Value : DistanceCostBase.Value;

        public static float GetDistanceCostExponent(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneDistanceCostExponent.Value : DistanceCostExponent.Value;

        public static float GetSicknessBaseDuration(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneSicknessBaseDuration.Value : SicknessBaseDuration.Value;

        public static float GetSicknessBaseDuration(bool isStone) =>
            isStone ? StoneSicknessBaseDuration.Value : SicknessBaseDuration.Value;

        public static float GetSicknessDurabilityLossPercent(TeleportWorld? portal) =>
            IsStonePortal(portal) ? StoneSicknessDurabilityLossPercent.Value : SicknessDurabilityLossPercent.Value;

        public static float GetSicknessDurabilityLossPercent(bool isStone) =>
            isStone ? StoneSicknessDurabilityLossPercent.Value : SicknessDurabilityLossPercent.Value;

        #endregion
    }
}
