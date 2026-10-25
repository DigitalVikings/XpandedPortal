using System.Collections.Generic;
using PortalTweaks.Configuration;
using UnityEngine;

namespace PortalTweaks.StatusEffects
{
    /// <summary>
    /// Custom status effect applying post-teleport sickness debuffs.
    /// Inflicts carry weight reduction, movement speed penalties, health/stamina/eitr regen penalties,
    /// and blocks further teleportation for the debuff's duration.
    /// </summary>
    public class SE_TeleportSickness : SE_Stats
    {
        public const string EffectName = "SE_TeleportSickness";
        public static readonly int EffectHash = EffectName.GetStableHashCode();

        public SE_TeleportSickness()
        {
            name = EffectName;
            m_name = "Teleport Sickness";
            m_tooltip = "Suffering from spatial displacement disorientation.";
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            ApplyConfigModifiers();
        }

        /// <summary>
        /// Updates the numerical stat modifiers according to current configuration settings.
        /// </summary>
        public void ApplyConfigModifiers()
        {
            // Reduced carrying capacity: absolute deduction
            m_addMaxCarryWeight = -Mathf.Max(0f, ModConfig.SicknessCarryWeightReduction.Value);

            // Movement speed: 0 to 100% -> speed delta (e.g. 90% = -0.10f, 0% = -1.0f, 100% = 0.0f)
            float speedVal = Mathf.Clamp(ModConfig.SicknessMovementSpeed.Value, 0f, 100f);
            m_speedModifier = (speedVal / 100f) - 1.0f;

            // Health regen: 0 to 100% -> multiplier (50% = 0.50f)
            m_healthRegenMultiplier = Mathf.Clamp(ModConfig.SicknessHealthRegen.Value, 0f, 100f) / 100f;

            // Stamina regen: 0 to 100% -> multiplier (50% = 0.50f)
            m_staminaRegenMultiplier = Mathf.Clamp(ModConfig.SicknessStaminaRegen.Value, 0f, 100f) / 100f;

            // Eitr regen: 0 to 100% -> multiplier (50% = 0.50f)
            m_eitrRegenMultiplier = Mathf.Clamp(ModConfig.SicknessEitrRegen.Value, 0f, 100f) / 100f;
        }

        public override string GetTooltipString()
        {
            var parts = new List<string>();

            if (ModConfig.SicknessBlockTeleport.Value)
            {
                parts.Add("<color=#ff6060>Unable to use portals</color>");
            }

            if (ModConfig.SicknessCarryWeightReduction.Value > 0f)
            {
                parts.Add($"Carry weight: -{ModConfig.SicknessCarryWeightReduction.Value:F0}");
            }

            if (ModConfig.SicknessMovementSpeed.Value < 100f)
            {
                parts.Add($"Movement speed: {ModConfig.SicknessMovementSpeed.Value:F0}%");
            }

            if (ModConfig.SicknessHealthRegen.Value < 100f)
            {
                parts.Add($"Health regen: {ModConfig.SicknessHealthRegen.Value:F0}%");
            }

            if (ModConfig.SicknessStaminaRegen.Value < 100f)
            {
                parts.Add($"Stamina regen: {ModConfig.SicknessStaminaRegen.Value:F0}%");
            }

            if (ModConfig.SicknessEitrRegen.Value < 100f)
            {
                parts.Add($"Eitr regen: {ModConfig.SicknessEitrRegen.Value:F0}%");
            }

            return string.Join("\n", parts);
        }

        public override string GetIconText()
        {
            float remaining = GetRemaningTime();
            return remaining > 0f ? remaining.ToString("F0") + "s" : "";
        }
    }
}
