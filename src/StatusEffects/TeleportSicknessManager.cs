using System;
using System.Collections.Generic;
using PortalTweaks.Configuration;
using UnityEngine;

namespace PortalTweaks.StatusEffects
{
    /// <summary>
    /// Coordinates calculation, durability penalty, and status effect application for Teleportation Sickness.
    /// </summary>
    public static class TeleportSicknessManager
    {
        private static SE_TeleportSickness? _templateEffect;
        private struct PendingSicknessData
        {
            public float JumpDistance;
            public bool IsStonePortal;
            public float OverdraftExtraSeconds;
        }

        private static readonly Dictionary<long, PendingSicknessData> _pendingSickness = new Dictionary<long, PendingSicknessData>();

        /// <summary>
        /// Schedules teleportation sickness and durability damage to be applied once the player's
        /// teleportation sequence completes at the destination.
        /// </summary>
        public static void SchedulePendingSickness(Player? player, float jumpDist, bool isStonePortal = false, float overdraftExtraSeconds = 0f)
        {
            if (player == null)
                return;

            _pendingSickness[player.GetPlayerID()] = new PendingSicknessData
            {
                JumpDistance = jumpDist,
                IsStonePortal = isStonePortal,
                OverdraftExtraSeconds = overdraftExtraSeconds
            };
        }

        /// <summary>
        /// Clears any pending sickness scheduled for this player if teleportation aborted or failed to start.
        /// </summary>
        public static void ClearPendingSickness(Player? player)
        {
            if (player == null)
                return;

            _pendingSickness.Remove(player.GetPlayerID());
        }

        /// <summary>
        /// Called when the player finishes teleporting (arrives at destination) to apply the scheduled
        /// durability loss and Teleportation Sickness debuff.
        /// </summary>
        public static void ApplyPendingSickness(Player? player)
        {
            if (player == null)
                return;

            long id = player.GetPlayerID();
            if (!_pendingSickness.TryGetValue(id, out var data))
                return;

            _pendingSickness.Remove(id);

            // 1. Apply instant durability damage to carried/equipped gear
            if (ModConfig.EnableTeleportSickness.Value)
            {
                ApplyDurabilityLoss(player, data.IsStonePortal);
            }

            // 2. Calculate distance-scaled duration + overdraft duration and attach status effect
            float baseDuration = 0f;
            if (ModConfig.EnableTeleportSickness.Value)
            {
                baseDuration = CalculateDuration(data.JumpDistance, data.IsStonePortal);
            }

            float totalDuration = baseDuration + data.OverdraftExtraSeconds;
            if (totalDuration > 0.001f)
            {
                ApplySickness(player, totalDuration);
                if (data.OverdraftExtraSeconds > 0.001f)
                {
                    player.Message(
                        MessageHud.MessageType.Center,
                        $"Teleported via overdraft! Sickness extended (+{data.OverdraftExtraSeconds:F0}s)"
                    );
                }
            }
        }

        /// <summary>
        /// Calculates the debuff duration based on teleport distance:
        /// duration(dist) = base_duration * (dist / base_dist)^exponent.
        /// </summary>
        public static float CalculateDuration(float distance, bool isStonePortal = false)
        {
            float baseDuration = ModConfig.GetSicknessBaseDuration(isStonePortal);
            if (baseDuration <= 0.001f)
                return 0f;

            float baseDist = ModConfig.SicknessBaseDistance.Value;
            float exponent = ModConfig.SicknessDurationExponent.Value;

            if (distance <= 0f || baseDist <= 0.001f)
                return baseDuration;

            float ratio = Mathf.Max(0f, distance / baseDist);
            float duration = baseDuration * Mathf.Pow(ratio, exponent);
            return Mathf.Max(0f, duration);
        }

        /// <summary>
        /// Applies an instant percentage of max durability damage to all carried and equipped items in player inventory.
        /// </summary>
        public static void ApplyDurabilityLoss(Player? player, bool isStonePortal = false)
        {
            if (player == null || player.GetInventory() == null)
                return;

            float lossPercent = Mathf.Clamp(ModConfig.GetSicknessDurabilityLossPercent(isStonePortal), 0f, 100f);
            if (lossPercent <= 0.001f)
                return;

            float fraction = lossPercent / 100f;
            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (item?.m_shared != null && item.m_shared.m_useDurability)
                {
                    float damage = item.GetMaxDurability() * fraction;
                    item.m_durability = Mathf.Max(0f, item.m_durability - damage);
                }
            }
        }

        /// <summary>
        /// Resolves a suitable HUD icon for Teleport Sickness.
        /// </summary>
        public static Sprite? GetSicknessIcon()
        {
            if (ZNetScene.instance != null)
            {
                var portalPrefab = ZNetScene.instance.GetPrefab("portal_wood");
                if (portalPrefab != null)
                {
                    var piece = portalPrefab.GetComponent<Piece>();
                    if (piece != null && piece.m_icon != null)
                        return piece.m_icon;
                }
            }

            if (ObjectDB.instance != null)
            {
                var frost = ObjectDB.instance.GetStatusEffect("Frost".GetStableHashCode());
                if (frost != null && frost.m_icon != null)
                    return frost.m_icon;
            }

            return null;
        }

        /// <summary>
        /// Retrieves or creates the singleton template instance for SE_TeleportSickness.
        /// </summary>
        public static SE_TeleportSickness GetOrCreateTemplate()
        {
            if (_templateEffect == null)
            {
                _templateEffect = ScriptableObject.CreateInstance<SE_TeleportSickness>();
                _templateEffect.name = SE_TeleportSickness.EffectName;
                _templateEffect.m_name = "Teleport Sickness";
                _templateEffect.m_icon = GetSicknessIcon();
            }

            if (_templateEffect.m_icon == null)
            {
                _templateEffect.m_icon = GetSicknessIcon();
            }

            return _templateEffect;
        }

        /// <summary>
        /// Applies the Teleportation Sickness status effect to the player for the specified duration.
        /// </summary>
        public static void ApplySickness(Player? player, float duration)
        {
            if (player == null || duration <= 0.001f)
                return;

            var template = GetOrCreateTemplate();
            template.ApplyConfigModifiers();
            template.m_ttl = duration;

            player.GetSEMan()?.AddStatusEffect(template, resetTime: true);
        }

        /// <summary>
        /// Checks if a player currently has an active Teleportation Sickness status effect.
        /// </summary>
        public static bool HasSickness(Player? player, out float remainingSeconds)
        {
            remainingSeconds = 0f;
            if (player == null)
                return false;

            var seman = player.GetSEMan();
            if (seman == null || !seman.HaveStatusEffect(SE_TeleportSickness.EffectHash))
                return false;

            var se = seman.GetStatusEffect(SE_TeleportSickness.EffectHash);
            if (se != null)
            {
                remainingSeconds = Mathf.Max(0f, se.GetRemaningTime());
                return remainingSeconds > 0.001f;
            }

            return false;
        }
    }
}
