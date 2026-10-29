using System;
using System.Collections.Generic;
using DigitalVikings.XpandedPortal.Configuration;
using DigitalVikings.XpandedPortal.Data;
using DigitalVikings.XpandedPortal.Patches;
using UnityEngine;

namespace DigitalVikings.XpandedPortal.Energy
{
    /// <summary>
    /// Manages portal energy accumulation, cost calculations, deductions, and deficit backlash.
    /// </summary>
    public static class EnergyManager
    {
        /// <summary>
        /// Retrieves the current accumulated energy of a portal along with maximum capacity and recharge rate.
        /// Uses deterministic timestamp delta calculation (O(1), zero tick loops, persistent across sessions).
        /// </summary>
        public static bool GetCurrentEnergy(TeleportWorld? portal, out float currentEnergy, out float maxEnergy, out float rechargeRate)
        {
            maxEnergy = ModConfig.GetMaxEnergy(portal);
            rechargeRate = ModConfig.GetRechargeRate(portal);
            currentEnergy = maxEnergy;

            if (portal == null)
                return false;

            var nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return false;

            var zdo = nview.GetZDO();
            if (zdo == null)
                return false;

            long timeBits = zdo.GetLong(PortalDataManager.KeyEnergyTime, 0L);
            double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : (double)Time.time;

            if (timeBits == 0L)
            {
                // First time portal energy is accessed: initialize at configured starting percentage
                float startingPct = Mathf.Clamp(ModConfig.StartingEnergyPercentage.Value, 0f, 100f) / 100f;
                currentEnergy = maxEnergy * startingPct;

                if (!nview.IsOwner())
                {
                    nview.ClaimOwnership();
                }
                zdo.Set(PortalDataManager.KeyEnergy, currentEnergy);
                zdo.Set(PortalDataManager.KeyEnergyTime, BitConverter.DoubleToInt64Bits(now));
                return true;
            }

            double savedTime = BitConverter.Int64BitsToDouble(timeBits);
            float savedEnergy = zdo.GetFloat(PortalDataManager.KeyEnergy, 0f);

            double elapsed = now - savedTime;
            if (elapsed < 0.0)
                elapsed = 0.0;

            currentEnergy = Mathf.Min(maxEnergy, savedEnergy + (float)(elapsed * rechargeRate));
            currentEnergy = Mathf.Max(0f, currentEnergy);

            return true;
        }

        /// <summary>
        /// Determines if an item is ammunition (arrows, bolts, bait).
        /// </summary>
        public static bool IsAmmoItem(ItemDrop.ItemData? item)
        {
            if (item?.m_shared == null)
                return false;

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo ||
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
        }

        /// <summary>
        /// Determines if an item is food or consumable.
        /// </summary>
        public static bool IsFoodItem(ItemDrop.ItemData? item)
        {
            if (item?.m_shared == null)
                return false;

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable ||
                   item.m_shared.m_food > 0f ||
                   item.m_shared.m_foodStamina > 0f ||
                   item.m_shared.m_foodEitr > 0f;
        }

        /// <summary>
        /// Determines if an item is equipment (weapons, tools, armor, shields, utilities, trinkets).
        /// Excludes ammunition and consumables.
        /// </summary>
        public static bool IsEquipmentItem(ItemDrop.ItemData? item)
        {
            if (item?.m_shared == null)
                return false;

            if (IsAmmoItem(item) || IsFoodItem(item))
                return false;

            return item.m_equipped || item.IsEquipable();
        }

        /// <summary>
        /// Calculates the portion of inventory weight that incurs energy cost according to configuration.
        /// Equipment, food, and ammo costs are governed by their respective configuration toggles.
        /// General items (materials, ores, trophies, misc, etc.) are always counted.
        /// </summary>
        public static float CalculateChargeableWeight(Inventory? inventory)
        {
            if (inventory == null)
                return 0f;

            float chargeableWeight = 0f;
            foreach (var item in inventory.GetAllItems())
            {
                if (item == null)
                    continue;

                if (IsAmmoItem(item))
                {
                    if (ModConfig.AmmoCostsEnergy.Value)
                    {
                        chargeableWeight += item.GetWeight();
                    }
                }
                else if (IsFoodItem(item))
                {
                    if (ModConfig.FoodCostsEnergy.Value)
                    {
                        chargeableWeight += item.GetWeight();
                    }
                }
                else if (IsEquipmentItem(item))
                {
                    if (ModConfig.EquipmentCostsEnergy.Value)
                    {
                        chargeableWeight += item.GetWeight();
                    }
                }
                else
                {
                    // General items (materials, ores, trophies, etc.) always incur carry weight cost
                    chargeableWeight += item.GetWeight();
                }
            }

            return chargeableWeight;
        }

        /// <summary>
        /// Checks if a portal has an active group conduit discount for the given player.
        /// Returns true if another player traveled to the same destination within the configured conduit window.
        /// </summary>
        public static bool IsConduitDiscountActive(TeleportWorld? portal, Player? player, out float secondsRemaining)
        {
            secondsRemaining = 0f;
            if (!ModConfig.ModEnabled.Value || !ModConfig.EnableEnergySystem.Value || !ModConfig.EnableGroupConduitDiscount.Value)
                return false;

            if (portal == null || player == null)
                return false;

            var nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return false;

            var zdo = nview.GetZDO();
            if (zdo == null)
                return false;

            long timeBits = zdo.GetLong(PortalDataManager.KeyConduitTime, 0L);
            if (timeBits == 0L)
                return false;

            double lastTravelTime = BitConverter.Int64BitsToDouble(timeBits);
            double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : (double)Time.time;
            double elapsed = now - lastTravelTime;
            float window = ModConfig.GroupConduitWindowSeconds.Value;

            if (elapsed < 0.0 || elapsed > window)
                return false;

            long lastPlayerId = zdo.GetLong(PortalDataManager.KeyConduitPlayer, 0L);
            long currentPlayerId = player.GetPlayerID();
            if (lastPlayerId == 0L || lastPlayerId == currentPlayerId)
                return false;

            ZDOID currentTargetID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (currentTargetID == ZDOID.None)
                return false;

            string lastTarget = zdo.GetString(PortalDataManager.KeyConduitTarget, string.Empty);
            if (string.IsNullOrEmpty(lastTarget) || lastTarget != currentTargetID.ToString())
                return false;

            secondsRemaining = Mathf.Max(0f, window - (float)elapsed);
            return true;
        }

        /// <summary>
        /// Records a player's teleportation on the portal ZDO to initiate or refresh the group conduit window.
        /// </summary>
        public static void RecordConduitTravel(TeleportWorld? portal, Player? player)
        {
            if (portal == null || player == null)
                return;

            var nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return;

            var zdo = nview.GetZDO();
            if (zdo == null)
                return;

            ZDOID targetID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (targetID == ZDOID.None)
                return;

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : (double)Time.time;
            zdo.Set(PortalDataManager.KeyConduitPlayer, player.GetPlayerID());
            zdo.Set(PortalDataManager.KeyConduitTime, BitConverter.DoubleToInt64Bits(now));
            zdo.Set(PortalDataManager.KeyConduitTarget, targetID.ToString());
        }

        /// <summary>
        /// Calculates the total teleport energy cost for a player, factoring in fixed base cost, carried inventory,
        /// and distance multiplier: TotalCost = (Base + Weight + Ore) * (dist / base)^exponent.
        /// </summary>
        public static float CalculateTeleportCost(Player? player, TeleportWorld? portal, out float totalCost, out string costBreakdown, float? overrideDistance = null)
        {
            float baseCost = ModConfig.GetBaseCost(portal);
            if (IsConduitDiscountActive(portal, player, out _))
            {
                baseCost = 0f;
            }

            float rawCost = baseCost;

            var parts = new List<string>
            {
                $"Base: {baseCost:F0}"
            };

            if (player != null && player.GetInventory() != null)
            {
                // Carry weight cost scaling
                if (ModConfig.EnableWeightCostScaling.Value)
                {
                    float weight = CalculateChargeableWeight(player.GetInventory());
                    float weightCost = weight * ModConfig.GetWeightCostFactor(portal);
                    rawCost += weightCost;
                    if (weightCost > 0.01f)
                    {
                        parts.Add($"Weight: +{weightCost:F0}");
                    }
                }

                // Non-teleportable metal/ore cost scaling
                if (ModConfig.EnableOreCostScaling.Value)
                {
                    float oreWeight = 0f;
                    foreach (var item in player.GetInventory().GetAllItems())
                    {
                        if (item.m_shared != null && !item.m_shared.m_teleportable)
                        {
                            oreWeight += item.GetWeight();
                        }
                    }

                    if (oreWeight > 0f)
                    {
                        float oreCost = oreWeight * ModConfig.GetOreCostFactor(portal);
                        rawCost += oreCost;
                        parts.Add($"Ore: +{oreCost:F0}");
                    }
                }
            }

            rawCost = Mathf.Max(0f, rawCost);
            totalCost = rawCost;

            // Distance cost scaling multiplier: mult(dist) = (dist / base)^exponent
            if (ModConfig.EnableDistanceCostScaling.Value)
            {
                float distance = 0f;
                bool hasDistance = false;

                if (overrideDistance.HasValue)
                {
                    distance = overrideDistance.Value;
                    hasDistance = true;
                }
                else if (portal != null && TeleportPatch.TryGetTargetPortalDistance(portal, out float targetDist))
                {
                    distance = targetDist;
                    hasDistance = true;
                }

                if (hasDistance)
                {
                    float baseDist = ModConfig.GetDistanceCostBase(portal);
                    float exponent = ModConfig.GetDistanceCostExponent(portal);
                    float distanceMult = 1.0f;

                    if (baseDist > 0.001f)
                    {
                        float ratio = Mathf.Max(0f, distance / baseDist);
                        distanceMult = Mathf.Pow(ratio, exponent);
                        distanceMult = Mathf.Max(ModConfig.MinDistanceCostMultiplier.Value, distanceMult);
                    }

                    totalCost = rawCost * distanceMult;
                    parts.Add($"Dist ({distance:F0}m): x{distanceMult:F2}");
                }
            }

            totalCost = Mathf.Max(0f, totalCost);
            costBreakdown = string.Join(", ", parts);
            return totalCost;
        }

        /// <summary>
        /// Deducts energy from the portal and updates the timestamp on the portal's ZDO.
        /// </summary>
        public static void ConsumeEnergy(TeleportWorld? portal, float amount, out float remainingEnergy)
        {
            remainingEnergy = 0f;
            if (portal == null)
                return;

            var nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return;

            var zdo = nview.GetZDO();
            if (zdo == null)
                return;

            GetCurrentEnergy(portal, out float current, out _, out _);
            remainingEnergy = Mathf.Max(0f, current - amount);

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : (double)Time.time;
            zdo.Set(PortalDataManager.KeyEnergy, remainingEnergy);
            zdo.Set(PortalDataManager.KeyEnergyTime, BitConverter.DoubleToInt64Bits(now));
        }

        /// <summary>
        /// Computes remaining seconds until sufficient energy has recharged for the given cost.
        /// </summary>
        public static float GetRechargeTimeRemaining(float currentEnergy, float targetCost, float rechargeRate)
        {
            if (currentEnergy >= targetCost || rechargeRate <= 0.0001f)
                return 0f;

            return Mathf.Max(0f, (targetCost - currentEnergy) / rechargeRate);
        }

        /// <summary>
        /// Returns a concise, non-verbose explanation of why the teleport cost exceeds maximum portal capacity.
        /// Differentiates between carrying too much vs portal being too far away.
        /// </summary>
        public static string GetExceedsCapacityReason(Player? player, TeleportWorld? portal, float maxEnergy, float? overrideDistance = null)
        {
            float baseCost = ModConfig.GetBaseCost(portal);
            if (IsConduitDiscountActive(portal, player, out _))
            {
                baseCost = 0f;
            }

            float weightCost = 0f;
            float oreCost = 0f;

            if (player != null && player.GetInventory() != null)
            {
                if (ModConfig.EnableWeightCostScaling.Value)
                {
                    weightCost = CalculateChargeableWeight(player.GetInventory()) * ModConfig.GetWeightCostFactor(portal);
                }

                if (ModConfig.EnableOreCostScaling.Value)
                {
                    float oreWeight = 0f;
                    foreach (var item in player.GetInventory().GetAllItems())
                    {
                        if (item.m_shared != null && !item.m_shared.m_teleportable)
                        {
                            oreWeight += item.GetWeight();
                        }
                    }
                    oreCost = oreWeight * ModConfig.GetOreCostFactor(portal);
                }
            }

            float rawInventoryCost = baseCost + weightCost + oreCost;

            float distanceMult = 1.0f;
            if (ModConfig.EnableDistanceCostScaling.Value)
            {
                float distance = 0f;
                bool hasDistance = false;

                if (overrideDistance.HasValue)
                {
                    distance = overrideDistance.Value;
                    hasDistance = true;
                }
                else if (portal != null && TeleportPatch.TryGetTargetPortalDistance(portal, out float targetDist))
                {
                    distance = targetDist;
                    hasDistance = true;
                }

                float baseDist = ModConfig.GetDistanceCostBase(portal);
                float exponent = ModConfig.GetDistanceCostExponent(portal);
                if (hasDistance && baseDist > 0.001f)
                {
                    float ratio = Mathf.Max(0f, distance / baseDist);
                    distanceMult = Mathf.Pow(ratio, exponent);
                    distanceMult = Mathf.Max(ModConfig.MinDistanceCostMultiplier.Value, distanceMult);
                }
            }

            bool inventoryExceeds = rawInventoryCost > maxEnergy;
            bool distanceExceeds = (baseCost * distanceMult) > maxEnergy;

            if (inventoryExceeds && !distanceExceeds)
                return "Carrying too much";
            if (distanceExceeds && !inventoryExceeds)
                return "Portal is too far away";

            if (distanceMult > 1.8f && (weightCost + oreCost) <= baseCost)
                return "Portal is too far away";
            if ((weightCost + oreCost) > baseCost * 2f && distanceMult <= 1.2f)
                return "Carrying too much";

            return "Carrying too much or portal too far";
        }

        /// <summary>
        /// Calculates additional Teleportation Sickness duration added from energy deficit during an overdraft jump.
        /// </summary>
        public static float CalculateOverdraftExtraSickness(float deficit)
        {
            if (deficit <= 0f)
                return 0f;

            return Mathf.Max(0f, deficit * ModConfig.OverdraftSicknessSecondsPerEnergy.Value);
        }
    }
}
