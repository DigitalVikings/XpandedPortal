using System.Collections.Generic;
using HarmonyLib;
using DigitalVikings.XpandedPortal.Configuration;
using DigitalVikings.XpandedPortal.Data;
using DigitalVikings.XpandedPortal.Energy;
using DigitalVikings.XpandedPortal.StatusEffects;
using UnityEngine;

namespace DigitalVikings.XpandedPortal.Patches
{
    /// <summary>
    /// Harmony patches for portal interaction, teleport restrictions, weight limits, distance limits, and passwords.
    /// </summary>
    [HarmonyPatch]
    public static class TeleportPatch
    {
        private static MaterialPropertyBlock? s_portalPropBlock;
        private static MaterialPropertyBlock PortalPropBlock => s_portalPropBlock ??= new MaterialPropertyBlock();
        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        #region Weight Limit

        /// <summary>
        /// <summary>
        /// Checks if a player exceeds the portal weight limit.
        /// </summary>
        public static bool IsOverWeightLimit(Player? player, TeleportWorld? portal, out float currentWeight, out float maxWeight)
        {
            currentWeight = 0f;
            maxWeight = ModConfig.GetMaxWeight(portal);

            if (player == null || player.GetInventory() == null)
                return false;

            currentWeight = player.GetInventory().GetTotalWeight();
            return ModConfig.EnableMaxWeightLimit.Value && (currentWeight > maxWeight);
        }

        public static bool IsOverWeightLimit(Player? player, bool isStone, out float currentWeight, out float maxWeight)
        {
            currentWeight = 0f;
            maxWeight = ModConfig.GetMaxWeight(isStone);

            if (player == null || player.GetInventory() == null)
                return false;

            currentWeight = player.GetInventory().GetTotalWeight();
            return ModConfig.EnableMaxWeightLimit.Value && (currentWeight > maxWeight);
        }

        public static bool IsOverWeightLimit(Player? player, out float currentWeight, out float maxWeight) =>
            IsOverWeightLimit(player, null, out currentWeight, out maxWeight);

        #endregion

        #region Item Restrictions

        /// <summary>
        /// Checks if a player carries items violating the active ItemRestrictionTier.
        /// </summary>
        public static bool CanTeleportItems(Player? player, TeleportWorld? portal, out string rejectionReason) =>
            CanTeleportItems(player, ModConfig.IsStonePortal(portal), out rejectionReason);

        public static bool CanTeleportItems(Player? player, bool isStone, out string rejectionReason)
        {
            rejectionReason = string.Empty;

            if (!ModConfig.ModEnabled.Value)
                return true;

            var tier = ModConfig.GetRestrictionTier(isStone);

            if (tier == ItemRestrictionTier.NoTeleport)
            {
                rejectionReason = "Portals are disabled";
                return false;
            }

            if (tier == ItemRestrictionTier.Unrestricted)
            {
                return true;
            }

            if (player == null || player.GetInventory() == null)
                return true;

            var items = player.GetInventory().GetAllItems();

            switch (tier)
            {
                case ItemRestrictionTier.Naked:
                    if (items.Count > 0)
                    {
                        rejectionReason = "Inventory must be empty";
                        return false;
                    }
                    break;

                case ItemRestrictionTier.EquippedOnly:
                    foreach (var item in items)
                    {
                        if (!item.m_equipped)
                        {
                            rejectionReason = "Only actively equipped items allowed";
                            return false;
                        }
                    }
                    break;

                case ItemRestrictionTier.EquipmentOnly:
                    foreach (var item in items)
                    {
                        if (!item.IsEquipable())
                        {
                            rejectionReason = "Only equipment is allowed";
                            return false;
                        }
                    }
                    break;

                case ItemRestrictionTier.GearAndProvisions:
                    foreach (var item in items)
                    {
                        bool isGear = item.IsEquipable();
                        bool isConsumable = item.m_shared != null &&
                            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
                        bool isAmmo = item.m_shared != null &&
                            (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo ||
                             item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable);

                        if (!isGear && !isConsumable && !isAmmo)
                        {
                            rejectionReason = "Only equipment, food, and ammo allowed";
                            return false;
                        }
                    }
                    break;

                case ItemRestrictionTier.Vanilla:
                    foreach (var item in items)
                    {
                        if (item.m_shared != null && !item.m_shared.m_teleportable)
                        {
                            rejectionReason = "Carrying non-teleportable items (ores/metals)";
                            return false;
                        }
                    }
                    break;
            }

            return true;
        }

        public static bool CanTeleportItems(Player? player, out string rejectionReason) =>
            CanTeleportItems(player, false, out rejectionReason);

        #endregion

        #region Distance Limit

        /// <summary>
        /// Calculates the horizontal distance between this portal and its connected target.
        /// </summary>
        public static bool TryGetTargetPortalDistance(TeleportWorld portal, out float distance)
        {
            distance = 0f;
            if (portal == null)
                return false;

            ZNetView nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return false;

            ZDO zdo = nview.GetZDO();
            if (zdo == null)
                return false;

            ZDOID targetID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (targetID == ZDOID.None)
                return false;

            if (ZDOMan.instance == null)
                return false;

            ZDO targetZDO = ZDOMan.instance.GetZDO(targetID);
            if (targetZDO == null)
            {
                ZDOMan.instance.RequestZDO(targetID);
                return false;
            }

            Vector3 myPos = portal.transform.position;
            Vector3 targetPos = targetZDO.GetPosition();
            distance = Utils.DistanceXZ(myPos, targetPos);
            return true;
        }

        /// <summary>
        /// Checks if the distance between connected portals violates the configured distance limits.
        /// </summary>
        public static bool IsDistanceViolated(TeleportWorld portal, out float distance, out string rejectionReason)
        {
            distance = 0f;
            rejectionReason = string.Empty;

            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableDistanceLimit.Value)
                return false;

            if (!TryGetTargetPortalDistance(portal, out distance))
                return false;

            float maxDistance = ModConfig.GetMaxDistance(portal);
            if (distance > maxDistance)
            {
                rejectionReason = $"Portals are too far apart ({distance:F0}m / {maxDistance:F0}m)";
                return true;
            }

            if (ModConfig.EnableMinDistanceLimit.Value)
            {
                float minDistance = ModConfig.GetMinDistance(portal);
                if (distance < minDistance)
                {
                    rejectionReason = $"Portals are too close together ({distance:F0}m / {minDistance:F0}m)";
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Password Checks

        /// <summary>
        /// Checks whether the connected destination has a mismatched password under the active password rules.
        /// </summary>
        public static bool IsPasswordMismatched(TeleportWorld portal, out string reason)
        {
            reason = string.Empty;
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnablePasswordSystem.Value)
                return false;

            if (portal == null)
                return false;

            var nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return false;

            var myZdo = nview.GetZDO();
            if (myZdo == null)
                return false;

            ZDOID targetID = myZdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (targetID == ZDOID.None || ZDOMan.instance == null)
                return false;

            ZDO targetZDO = ZDOMan.instance.GetZDO(targetID);
            if (targetZDO == null)
                return false;

            string targetPass = targetZDO.GetString(PortalDataManager.KeyPassword, string.Empty);

            if (ModConfig.EnableTargetSelectionUI.Value)
            {
                // State 4: Outbound password must match target portal's password
                string myOutboundPass = myZdo.GetString(PortalDataManager.KeyOutboundPassword, string.Empty);
                if (!PortalDataManager.ValidatePasswordMatch(targetPass, myOutboundPass, out reason))
                {
                    return true;
                }
            }
            else
            {
                // State 2: Both paired portals must share the same password
                string myPass = myZdo.GetString(PortalDataManager.KeyPassword, string.Empty);
                if (myPass != targetPass)
                {
                    reason = "Password mismatch between paired portals";
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Portal Proximity Limit

        /// <summary>
        /// Checks if placing or operating a portal at the specified position violates the minimum distance
        /// requirement to any other portal in the world.
        /// </summary>
        public static bool HasProximityConflict(Vector3 pos, ZDOID excludeId, out float closestDist, out Vector3 closestPos)
        {
            closestDist = float.MaxValue;
            closestPos = Vector3.zero;

            if (!ModConfig.ModEnabled.Value || !ModConfig.EnablePortalProximityLimit.Value)
                return false;

            float minDistance = ModConfig.MinDistanceBetweenPortals.Value;
            if (minDistance <= 0f)
                return false;

            if (ZDOMan.instance == null)
                return false;

            List<ZDO> portalList = ZDOMan.instance.GetPortalList();
            if (portalList == null || portalList.Count == 0)
                return false;

            foreach (ZDO zdo in portalList)
            {
                if (zdo == null || zdo.m_uid == ZDOID.None || zdo.m_uid == excludeId)
                    continue;

                float dist = Utils.DistanceXZ(pos, zdo.GetPosition());
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPos = zdo.GetPosition();
                }
            }

            return closestDist < minDistance;
        }

        /// <summary>
        /// Checks if this portal has a proximity conflict with another portal in the world.
        /// </summary>
        public static bool HasProximityConflict(TeleportWorld portal, out float closestDist)
        {
            closestDist = float.MaxValue;
            if (portal == null)
                return false;

            ZNetView nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return false;

            ZDO zdo = nview.GetZDO();
            ZDOID myId = zdo != null ? zdo.m_uid : ZDOID.None;
            return HasProximityConflict(portal.transform.position, myId, out closestDist, out _);
        }

        #endregion

        #region Placement Proximity Hooks

        /// <summary>
        /// Updates the placement ghost validity when holding a portal piece.
        /// Turns the ghost red (invalid) if placed too close to an existing portal.
        /// </summary>
        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        [HarmonyPostfix]
        private static void PostfixUpdatePlacementGhost(Player __instance, GameObject ___m_placementGhost, ref Player.PlacementStatus ___m_placementStatus)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnablePortalProximityLimit.Value)
                return;

            if (___m_placementGhost == null)
                return;

            if (___m_placementGhost.GetComponentInChildren<TeleportWorld>() == null)
                return;

            Vector3 ghostPos = ___m_placementGhost.transform.position;
            if (HasProximityConflict(ghostPos, ZDOID.None, out _, out _))
            {
                ___m_placementStatus = Player.PlacementStatus.Invalid;
                Piece ghostPiece = ___m_placementGhost.GetComponent<Piece>();
                if (ghostPiece != null)
                {
                    ghostPiece.SetInvalidPlacementHeightlight(true);
                }
            }
        }

        /// <summary>
        /// Prevents placing a portal piece if too close to an existing portal, displaying an informative center message.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        [HarmonyPrefix]
        private static bool PrefixTryPlacePiece(Player __instance, Piece piece, GameObject ___m_placementGhost)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnablePortalProximityLimit.Value)
                return true;

            if (piece == null)
                return true;

            bool isPortal = piece.GetComponentInChildren<TeleportWorld>() != null ||
                            (___m_placementGhost != null && ___m_placementGhost.GetComponentInChildren<TeleportWorld>() != null);

            if (!isPortal)
                return true;

            Vector3 targetPos = ___m_placementGhost != null
                ? ___m_placementGhost.transform.position
                : __instance.transform.position;

            if (HasProximityConflict(targetPos, ZDOID.None, out float closestDist, out _))
            {
                float minDist = ModConfig.MinDistanceBetweenPortals.Value;
                __instance.Message(MessageHud.MessageType.Center, $"Too close to another portal ({closestDist:F0}m / {minDist:F0}m)");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Additional safeguard on Player.PlacePiece preventing portal placement within proximity limit.
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
        [HarmonyPrefix]
        private static bool PrefixPlacePiece(Player __instance, Piece piece, Vector3 pos)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnablePortalProximityLimit.Value)
                return true;

            if (piece != null && piece.GetComponentInChildren<TeleportWorld>() != null)
            {
                if (HasProximityConflict(pos, ZDOID.None, out float closestDist, out _))
                {
                    float minDist = ModConfig.MinDistanceBetweenPortals.Value;
                    __instance.Message(MessageHud.MessageType.Center, $"Too close to another portal ({closestDist:F0}m / {minDist:F0}m)");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Harmony Hooks

        /// <summary>
        /// Prevents target discovery (and thus portal vortex ignition) if portals exceed distance or have password mismatch.
        /// </summary>
        [HarmonyPatch(typeof(TeleportWorld), "TargetFound")]
        [HarmonyPostfix]
        private static void PostfixTargetFound(TeleportWorld __instance, ref bool __result)
        {
            if (!ModConfig.ModEnabled.Value)
                return;

            if (__result)
            {
                if (HasProximityConflict(__instance, out _))
                {
                    __result = false;
                    return;
                }

                if (IsDistanceViolated(__instance, out _, out _))
                {
                    __result = false;
                    return;
                }

                if (IsPasswordMismatched(__instance, out _))
                {
                    __result = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Extinguishes the portal vortex visually when current energy is below the teleport cost
        /// or when another portal is too close causing interference.
        /// </summary>
        [HarmonyPatch(typeof(TeleportWorld), "UpdatePortal")]
        [HarmonyPostfix]
        private static void PostfixUpdatePortal(TeleportWorld __instance)
        {
            if (!ModConfig.ModEnabled.Value)
                return;

            if (HasProximityConflict(__instance, out _))
            {
                if (__instance.m_target_found != null)
                {
                    __instance.m_target_found.SetActive(false);
                }
                return;
            }

            if (!ModConfig.EnableEnergySystem.Value)
                return;

            // Only extinguish vortex if overdraft is refused! If overdraft is allowed (ExtendSickness), the portal remains usable.
            if (ModConfig.DeactivateVortexWhenInsufficientEnergy)
            {
                if (EnergyManager.GetCurrentEnergy(__instance, out float currentEnergy, out _, out _))
                {
                    Player localPlayer = Player.m_localPlayer;
                    float cost = EnergyManager.CalculateTeleportCost(localPlayer, __instance, out _, out _);
                    if (currentEnergy < cost)
                    {
                        if (__instance.m_target_found != null)
                        {
                            __instance.m_target_found.SetActive(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Distinct ethereal blue/green rune emission color displayed when an active conduit discount is open.
        /// </summary>
        public static readonly Color ConduitRuneColor = new Color(0.15f, 0.95f, 0.8f, 1.0f);

        /// <summary>
        /// Dynamically scales the portal arch rune emission brightness based on the portal's current energy reserve,
        /// and shifts rune illumination to ethereal blue/green when an active group conduit discount is open.
        /// Runes are faint/dim when depleted and glow brilliantly as energy recharges.
        /// </summary>
        [HarmonyPatch(typeof(TeleportWorld), "Update")]
        [HarmonyPostfix]
        private static void PostfixUpdate(TeleportWorld __instance)
        {
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableEnergySystem.Value)
                return;

            if (__instance.m_model == null)
                return;

            bool isConduit = EnergyManager.IsConduitDiscountActive(__instance, Player.m_localPlayer, out _);

            if (ModConfig.ScalePortalVfxWithEnergy.Value)
            {
                if (EnergyManager.GetCurrentEnergy(__instance, out float currentEnergy, out float maxEnergy, out _))
                {
                    var nview = __instance.GetComponent<ZNetView>();
                    bool hasTarget = nview != null && nview.IsValid() && nview.GetZDO() != null &&
                                     nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) != ZDOID.None;

                    float energyFraction = isConduit ? 1.0f : (maxEnergy > 0.001f ? Mathf.Clamp01(currentEnergy / maxEnergy) : 0f);
                    Color baseColor = __instance.m_colorUnconnected;
                    Color activeTargetColor = isConduit ? ConduitRuneColor : __instance.m_colorTargetfound;
                    Color targetColor = hasTarget ? activeTargetColor : baseColor;

                    Color finalColor = Color.Lerp(baseColor, targetColor, energyFraction);
                    __instance.m_model.GetPropertyBlock(PortalPropBlock);
                    PortalPropBlock.SetColor(EmissionColorProp, finalColor);
                    __instance.m_model.SetPropertyBlock(PortalPropBlock);
                }
            }
            else if (isConduit)
            {
                __instance.m_model.GetPropertyBlock(PortalPropBlock);
                PortalPropBlock.SetColor(EmissionColorProp, ConduitRuneColor);
                __instance.m_model.SetPropertyBlock(PortalPropBlock);
            }
        }

        /// <summary>
        /// Hooks into Humanoid.IsTeleportable to prevent portal activation/vortex when overweight
        /// or carrying items prohibited by the active restriction tier.
        /// </summary>
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsTeleportable))]
        [HarmonyPrefix]
        private static void PrefixIsTeleportable(ref bool allowAllItems)
        {
            if (ModConfig.ModEnabled.Value && ModConfig.GetRestrictionTier(allowAllItems) == ItemRestrictionTier.Unrestricted)
            {
                allowAllItems = true;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsTeleportable))]
        [HarmonyPostfix]
        private static void PostfixIsTeleportable(Humanoid __instance, bool allowAllItems, ref bool __result)
        {
            if (!ModConfig.ModEnabled.Value)
                return;

            if (__instance is Player player)
            {
                // Deactivate portal vortex if overweight
                if (IsOverWeightLimit(player, allowAllItems, out _, out _))
                {
                    __result = false;
                    return;
                }

                // Deactivate portal vortex if violating item restrictions
                if (!CanTeleportItems(player, allowAllItems, out _))
                {
                    __result = false;
                    return;
                }

                // Deactivate portal vortex if affected by teleportation sickness
                if (ModConfig.EnableTeleportSickness.Value && ModConfig.SicknessBlockTeleport.Value &&
                    TeleportSicknessManager.HasSickness(player, out _))
                {
                    __result = false;
                }
            }
        }

        /// <summary>
        /// Intercepts the portal teleportation trigger to validate password, distance, weight, and item restrictions.
        /// </summary>
        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        [HarmonyPrefix]
        private static bool OnTeleportPrefix(TeleportWorld __instance, Player player)
        {
            if (!ModConfig.ModEnabled.Value)
                return true;

            // -1. Check teleportation sickness lockout
            if (ModConfig.EnableTeleportSickness.Value && ModConfig.SicknessBlockTeleport.Value &&
                TeleportSicknessManager.HasSickness(player, out float sicknessRemaining))
            {
                player?.Message(
                    MessageHud.MessageType.Center,
                    $"Cannot teleport: Teleportation sickness active ({sicknessRemaining:F0}s remaining)"
                );
                return false;
            }

            // 0. Check portal proximity limit (anti-stacking)
            if (HasProximityConflict(__instance, out float proxDist))
            {
                float minAllowed = ModConfig.MinDistanceBetweenPortals.Value;
                player?.Message(
                    MessageHud.MessageType.Center,
                    $"Cannot teleport: Portal interference (within {proxDist:F0}m of another portal, minimum {minAllowed:F0}m required)"
                );
                return false;
            }

            // 1. Check password match
            if (IsPasswordMismatched(__instance, out string passwordRejection))
            {
                player?.Message(
                    MessageHud.MessageType.Center,
                    $"Cannot teleport: {passwordRejection}"
                );
                return false;
            }

            // 1. Check distance limit
            if (IsDistanceViolated(__instance, out _, out string distanceRejection))
            {
                player?.Message(
                    MessageHud.MessageType.Center,
                    $"Cannot teleport: {distanceRejection}"
                );
                return false;
            }

            // 2. Check weight limit
            if (IsOverWeightLimit(player, __instance, out float currentWeight, out float maxWeight))
            {
                player?.Message(
                    MessageHud.MessageType.Center,
                    $"Too heavy for portal: {currentWeight:F0} / {maxWeight:F0}"
                );
                return false;
            }

            // 3. Check item restriction tier
            if (!CanTeleportItems(player, __instance, out string itemRejection))
            {
                player?.Message(
                    MessageHud.MessageType.Center,
                    $"Cannot teleport: {itemRejection}"
                );
                return false;
            }

            // 4. Check Energy System
            float overdraftExtraSickness = 0f;
            if (ModConfig.EnableEnergySystem.Value)
            {
                EnergyManager.GetCurrentEnergy(__instance, out float currentEnergy, out float maxEnergy, out float rechargeRate);
                float cost = EnergyManager.CalculateTeleportCost(player, __instance, out _, out string breakdown);

                if (currentEnergy < cost)
                {
                    if (ModConfig.LowEnergyAction.Value == LowEnergyBehavior.RefuseTeleport)
                    {
                        if (cost > maxEnergy)
                        {
                            string reason = EnergyManager.GetExceedsCapacityReason(player, __instance, maxEnergy);
                            player?.Message(
                                MessageHud.MessageType.Center,
                                $"Cannot teleport: Exceeds portal capacity ({reason})"
                            );
                            return false;
                        }

                        float secondsRemaining = EnergyManager.GetRechargeTimeRemaining(currentEnergy, cost, rechargeRate);
                        player?.Message(
                            MessageHud.MessageType.Center,
                            $"Cannot teleport: Portal recharging ({currentEnergy:F0} / {cost:F0} energy, ~{secondsRemaining:F0}s)"
                        );
                        return false;
                    }
                    else // LowEnergyBehavior.ExtendSickness
                    {
                        float deficit = cost - currentEnergy;
                        EnergyManager.ConsumeEnergy(__instance, currentEnergy, out _);
                        overdraftExtraSickness = EnergyManager.CalculateOverdraftExtraSickness(deficit);

                        if (ModConfig.LogPortalTeleports.Value)
                        {
                            XpandedPortalPlugin.Log.LogInfo(
                                $"Portal [{__instance.GetText()}] overdrafted by {deficit:F1} energy. Sickness extended by {overdraftExtraSickness:F0}s."
                            );
                        }
                    }
                }
                else
                {
                    // Sufficient energy available: deduct cost
                    EnergyManager.ConsumeEnergy(__instance, cost, out float remainingEnergy);

                    if (ModConfig.LogPortalTeleports.Value)
                    {
                        XpandedPortalPlugin.Log.LogInfo(
                            $"Portal [{__instance.GetText()}] consumed {cost:F1} energy ({breakdown}). Remaining: {remainingEnergy:F1}/{maxEnergy:F1}"
                        );
                    }
                }

                // Record conduit travel state for multiplayer followers
                EnergyManager.RecordConduitTravel(__instance, player);
            }

            if (ModConfig.LogPortalTeleports.Value && player != null)
            {
                string tag = __instance.GetText();
                XpandedPortalPlugin.Log.LogInfo($"Player '{player.GetPlayerName()}' teleporting via portal [Tag: '{tag}']");
            }

            // 5. Schedule Teleportation Sickness & Durability Loss upon arrival
            if ((ModConfig.EnableTeleportSickness.Value || overdraftExtraSickness > 0f) && player != null)
            {
                bool isStone = ModConfig.IsStonePortal(__instance);
                TryGetTargetPortalDistance(__instance, out float jumpDist);
                TeleportSicknessManager.SchedulePendingSickness(player, jumpDist, isStone, overdraftExtraSickness);
            }

            return true;
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        [HarmonyPostfix]
        private static void OnTeleportPostfix(TeleportWorld __instance, Player player)
        {
            if (player != null && !player.IsTeleporting())
            {
                TeleportSicknessManager.ClearPendingSickness(player);
            }
        }

        /// <summary>
        /// Appends warnings and distance/password info to the portal hover text.
        /// </summary>
        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
        [HarmonyPostfix]
        private static void GetHoverTextPostfix(TeleportWorld __instance, ref string __result)
        {
            if (!ModConfig.ModEnabled.Value)
                return;

            Player localPlayer = Player.m_localPlayer;

            // Sickness warning
            if (ModConfig.EnableTeleportSickness.Value && ModConfig.SicknessBlockTeleport.Value &&
                localPlayer != null && TeleportSicknessManager.HasSickness(localPlayer, out float sicknessRemaining))
            {
                __result += $"\n<color=red>Cannot teleport: Teleportation sickness ({sicknessRemaining:F0}s)</color>";
            }

            // Proximity interference warning
            if (HasProximityConflict(__instance, out float proxDist))
            {
                float minAllowed = ModConfig.MinDistanceBetweenPortals.Value;
                __result += $"\n<color=red>Portal interference: Too close to another portal ({proxDist:F0}m / {minAllowed:F0}m)</color>";
            }

            // Password mismatch warning
            if (IsPasswordMismatched(__instance, out string passwordWarning))
            {
                __result += $"\n<color=red>Cannot teleport: {passwordWarning}</color>";
            }

            // Distance info & warnings
            if (TryGetTargetPortalDistance(__instance, out float distance))
            {
                if (IsDistanceViolated(__instance, out _, out string distanceWarning))
                {
                    __result += $"\n<color=red>Cannot teleport: {distanceWarning}</color>";
                }
                else if (ModConfig.ShowDistanceInHoverText.Value)
                {
                    __result += $"\n<color=#80c0ff>Portal distance: {distance:F0}m</color>";
                }
            }

            if (localPlayer == null)
                return;

            // Hover weight warning
            if (ModConfig.ShowHoverWeightWarning.Value && IsOverWeightLimit(localPlayer, __instance, out float currentWeight, out float maxWeight))
            {
                __result += $"\n<color=red>Too heavy to teleport: {currentWeight:F0} / {maxWeight:F0}</color>";
            }

            // Hover item restriction warning
            if (ModConfig.ShowHoverItemWarning.Value && !CanTeleportItems(localPlayer, __instance, out string rejectionReason))
            {
                __result += $"\n<color=red>Cannot teleport: {rejectionReason}</color>";
            }

            // Portal Energy Info
            if (ModConfig.EnableEnergySystem.Value && ModConfig.ShowHoverEnergyInfo.Value)
            {
                if (EnergyManager.GetCurrentEnergy(__instance, out float currentEnergy, out float maxEnergy, out float rechargeRate))
                {
                    float cost = EnergyManager.CalculateTeleportCost(localPlayer, __instance, out _, out _);
                    if (currentEnergy >= cost)
                    {
                        __result += $"\n<color=#ffd700>Portal Energy: {currentEnergy:F0} / {maxEnergy:F0} (Cost: {cost:F0})</color>";
                    }
                    else if (cost > maxEnergy && ModConfig.LowEnergyAction.Value == LowEnergyBehavior.RefuseTeleport)
                    {
                        string capacityReason = EnergyManager.GetExceedsCapacityReason(localPlayer, __instance, maxEnergy);
                        __result += $"\n<color=#ff8080>Portal Energy: {currentEnergy:F0} / {maxEnergy:F0} (Cost: {cost:F0} - {capacityReason})</color>";
                    }
                    else
                    {
                        float secRemaining = EnergyManager.GetRechargeTimeRemaining(currentEnergy, cost, rechargeRate);
                        if (ModConfig.LowEnergyAction.Value == LowEnergyBehavior.RefuseTeleport)
                        {
                            __result += $"\n<color=#ff8080>Portal Recharging: {currentEnergy:F0} / {maxEnergy:F0} (Requires {cost:F0}, ready in ~{secRemaining:F0}s)</color>";
                        }
                        else
                        {
                            float extraSec = EnergyManager.CalculateOverdraftExtraSickness(cost - currentEnergy);
                            __result += $"\n<color=#ffaa00>Portal Overdraft: {currentEnergy:F0} / {cost:F0} (+{extraSec:F0}s sickness! Ready in ~{secRemaining:F0}s)</color>";
                        }
                    }
                }
            }
        }

        #endregion

        #region ObjectDB Hooks

        [HarmonyPatch(typeof(ObjectDB), "Awake")]
        [HarmonyPostfix]
        private static void PostfixObjectDBAwake(ObjectDB __instance)
        {
            RegisterCustomStatusEffects(__instance);
        }

        [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
        [HarmonyPostfix]
        private static void PostfixCopyOtherDB(ObjectDB __instance)
        {
            RegisterCustomStatusEffects(__instance);
        }

        private static void RegisterCustomStatusEffects(ObjectDB db)
        {
            if (db == null || db.m_StatusEffects == null)
                return;

            var template = TeleportSicknessManager.GetOrCreateTemplate();
            if (!db.m_StatusEffects.Contains(template))
            {
                db.m_StatusEffects.Add(template);
            }
        }

        #endregion

        #region Player Teleport Completion Hooks

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPrefix]
        private static void PrefixUpdateTeleport(Player __instance, ref float dt, bool ___m_teleporting, out bool __state)
        {
            __state = ___m_teleporting;

            if (!ModConfig.ModEnabled.Value || !___m_teleporting)
                return;

            float targetDuration = ModConfig.PortalLoadTime.Value;
            if (Mathf.Approximately(targetDuration, 8.0f))
                return;

            if (targetDuration <= 0.05f)
            {
                dt = Mathf.Max(dt, 8.0f);
            }
            else
            {
                dt *= (8.0f / targetDuration);
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        [HarmonyPostfix]
        private static void PostfixUpdateTeleport(Player __instance, bool __state, bool ___m_teleporting)
        {
            if (__state && !___m_teleporting)
            {
                TeleportSicknessManager.ApplyPendingSickness(__instance);
            }
        }

        #endregion
    }
}
