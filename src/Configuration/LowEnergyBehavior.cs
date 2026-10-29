namespace DigitalVikings.XpandedPortal.Configuration
{
    /// <summary>
    /// Determines how portals respond when a player attempts to teleport with insufficient energy.
    /// </summary>
    public enum LowEnergyBehavior
    {
        /// <summary>
        /// Portal refuses to teleport the player until sufficient energy has recharged (no overdraft).
        /// </summary>
        RefuseTeleport,

        /// <summary>
        /// Overdraft is allowed: Portal teleports the player, draining remaining energy to 0, and extends
        /// the Teleportation Sickness duration proportionally to the energy deficit.
        /// </summary>
        ExtendSickness
    }
}
