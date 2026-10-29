using System.Collections.Generic;
using UnityEngine;

namespace DigitalVikings.XpandedPortal.Data
{
    /// <summary>
    /// Data structure representing a portal in the world.
    /// </summary>
    public class PortalInfo
    {
        public ZDOID Id { get; set; }
        public string Tag { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public ZDOID OutboundTarget { get; set; } = ZDOID.None;
        public string OutboundPassword { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrEmpty(Tag) ? $"[Unnamed Portal] ({Id})" : Tag;
    }

    /// <summary>
    /// Helper utilities for querying and updating portal ZDO data.
    /// </summary>
    public static class PortalDataManager
    {
        public const string KeyPassword = "PortalTweaks_Password";
        public const string KeyOutboundTarget = "PortalTweaks_OutboundTarget";
        public const string KeyOutboundPassword = "PortalTweaks_OutboundPassword";
        public const string KeyEnergy = "PortalTweaks_Energy";
        public const string KeyEnergyTime = "PortalTweaks_EnergyTime";
        public const string KeyConduitPlayer = "PortalTweaks_ConduitPlayer";
        public const string KeyConduitTime = "PortalTweaks_ConduitTime";
        public const string KeyConduitTarget = "PortalTweaks_ConduitTarget";

        /// <summary>
        /// Reads PortalInfo from a portal ZDO.
        /// </summary>
        public static PortalInfo? FromZDO(ZDO? zdo)
        {
            if (zdo == null || zdo.m_uid == ZDOID.None)
                return null;

            return new PortalInfo
            {
                Id = zdo.m_uid,
                Tag = zdo.GetString(ZDOVars.s_tag, string.Empty),
                Password = zdo.GetString(KeyPassword, string.Empty),
                Position = zdo.GetPosition(),
                OutboundTarget = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal),
                OutboundPassword = zdo.GetString(KeyOutboundPassword, string.Empty)
            };
        }

        /// <summary>
        /// Retrieves all known portals in the world from ZDOMan.
        /// </summary>
        public static List<PortalInfo> GetAllPortals(ZDOID excludeId = default)
        {
            var list = new List<PortalInfo>();
            if (ZDOMan.instance == null)
                return list;

            List<ZDO> portalZdos = ZDOMan.instance.GetPortalList();
            if (portalZdos == null)
                return list;

            foreach (var zdo in portalZdos)
            {
                if (zdo == null || zdo.m_uid == ZDOID.None || zdo.m_uid == excludeId)
                    continue;

                var info = FromZDO(zdo);
                if (info != null)
                {
                    list.Add(info);
                }
            }

            list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
            return list;
        }

        /// <summary>
        /// Writes updated portal configuration into the portal's ZDO.
        /// </summary>
        public static void SavePortal(TeleportWorld portal, string tag, string password, ZDOID outboundTarget, string outboundPassword, bool updateTarget)
        {
            if (portal == null)
                return;

            var nview = portal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return;

            ZDO zdo = nview.GetZDO();
            if (zdo == null)
                return;

            // 1. Update tag via standard TeleportWorld method (ensures network RPC sync)
            if (tag != portal.GetText())
            {
                portal.SetText(tag);
            }

            // 2. Save portal's own password
            zdo.Set(KeyPassword, password ?? string.Empty);

            // 3. Save target routing and outbound password if target selection is active
            if (updateTarget)
            {
                zdo.Set(KeyOutboundPassword, outboundPassword ?? string.Empty);
                zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, outboundTarget);
            }
        }

        /// <summary>
        /// Validates whether a connection from source portal to destination portal is permitted under password rules.
        /// </summary>
        public static bool ValidatePasswordMatch(string targetPortalPassword, string suppliedOutboundPassword, out string error)
        {
            error = string.Empty;

            // If target portal has no password, access is open
            if (string.IsNullOrEmpty(targetPortalPassword))
                return true;

            // Target has a password: supplied password must match
            if (targetPortalPassword == suppliedOutboundPassword)
                return true;

            error = "Password mismatch: incorrect password for destination.";
            return false;
        }
    }
}
