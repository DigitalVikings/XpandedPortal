# XpandedPortal

A comprehensive portal overhaul for Valheim introducing targeted destination selection, an energy & recharge system, customizable travel restrictions, and dedicated server synchronization.

---

## 🌟 Features

- **🎯 Targeted Destination UI**: Interacting with any portal opens a clean dropdown list showing all available destinations with distances in meters, sorted nearest-to-furthest.
- **⚡ Portal Energy System**: Portals can accumulate energy reserves over time and spend them to power teleports, with optional carry-weight and distance cost scaling.
- **⏱️ Configurable Transition Time**: Adjust the portal loading screen / transition duration from instant to cinematic (0 to 30 seconds, vanilla default: 8.0s).
- **🤢 Teleportation Sickness**: Optional debuff scaling with jump distance, temporarily reducing carry weight, movement speed, and regeneration rates.
- **⚖️ Travel Restrictions**: Optional limits for maximum inventory weight, travel distances (minimum and maximum), and progressive item restriction tiers.
- **🪨 Ashlands Stone Portal Support**: Independent limits and recharge tuning for vanilla Wood Portals vs. Ashlands Stone Portals.
- **🔒 Password Protection**: Optional password authentication to secure private or faction portals.
- **🌐 Dedicated Server Synchronization**: In multiplayer, server hosts enforce gameplay rules across all connected clients, while players retain control over their personal HUD options.
- **⚙️ Safe Defaults**: All restriction systems and energy costs are **disabled by default**. Only the targeted destination UI and helpful HUD hints are active out of the box, preserving vanilla gameplay until you choose to configure them.

---

## 📦 Installation

### Automatic (Recommended)
Install using **r2modman** or the **Thunderstore Mod Manager** with one click.

### Manual
1. Ensure both **BepInExPack Valheim** and **Jötunn, the Valheim Library** are installed.
2. Download and extract `XpandedPortal.dll` into your `Valheim/BepInEx/plugins/` folder.

---

## ⚙️ How It Works & Configuration

- Upon launching the game, a configuration file is generated in `BepInEx/config/`.
- Settings can be tweaked directly in the configuration file or in real-time in-game using the BepInEx Configuration Manager (**F1**).
- **Singleplayer**: Customize every aspect of portal mechanics, UI, and restrictions to match your preferred playstyle.
- **Multiplayer**: When connecting to a dedicated server or co-op host, gameplay rules and restrictions automatically synchronize from the server. Connected players cannot bypass server rules.

---

## 🧩 Compatibility

XpandedPortal is designed to work smoothly alongside most Valheim mods. However, other mods that modify the portal system may cause unintended consequences or conflicts. If using other portal mods, ensure overlapping features are disabled in their respective configs.

---

## 👏 Credits & Acknowledgments

- Special thanks and credit to **SpikeHimself** for the original **XPortal** mod, which served as the foundation and inspiration for our targeted destination selection UI.
- Built with **BepInEx**, **HarmonyX**, and **Jötunn**.
