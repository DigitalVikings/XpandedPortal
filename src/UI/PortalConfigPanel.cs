using System;
using System.Collections.Generic;
using Jotunn.Managers;
using DigitalVikings.XpandedPortal.Configuration;
using DigitalVikings.XpandedPortal.Data;
using DigitalVikings.XpandedPortal.Energy;
using DigitalVikings.XpandedPortal.Patches;
using DigitalVikings.XpandedPortal.StatusEffects;
using UnityEngine;
using UnityEngine.UI;

namespace DigitalVikings.XpandedPortal.UI
{
    /// <summary>
    /// Interactive in-game configuration panel for portals supporting destination selection and passwords.
    /// Implemented as a clean 2x2 grid (Tag & Destination on left, Passwords on right).
    /// </summary>
    public class PortalConfigPanel : MonoBehaviour
    {
        public static PortalConfigPanel? Instance { get; private set; }

        private GameObject? _rootPanel;
        private RectTransform? _rootRect;
        private TeleportWorld? _currentPortal;

        // Title
        private Text? _titleText;

        // Row 1 (This Portal)
        private Text? _tagLabel;
        private InputField? _tagInput;
        private Text? _passLabel;
        private InputField? _passwordInput;

        // Row 2 (Destination)
        private Text? _targetLabel;
        private Dropdown? _targetDropdown;
        private Text? _outPassLabel;
        private InputField? _outboundPasswordInput;

        // Info & Feedback
        private Text? _distanceText;
        private Text? _energyText;
        private Text? _weightText;
        private Text? _statusText;
        private float _uiUpdateTimer;

        // Buttons
        private Button? _saveButton;
        private Button? _cancelButton;

        private List<PortalInfo> _availablePortals = new();

        public static void Initialize()
        {
            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
                return;

            if (Instance != null)
                return;

            var go = new GameObject("PortalTweaks_UIHandler");
            go.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
            Instance = go.AddComponent<PortalConfigPanel>();
        }

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void Update()
        {
            if (_rootPanel != null && _rootPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Hide();
                    return;
                }

                _uiUpdateTimer += Time.deltaTime;
                if (_uiUpdateTimer >= 0.25f)
                {
                    _uiUpdateTimer = 0f;
                    UpdateStatus();
                }
            }
        }

        private void BuildUI()
        {
            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
                return;

            // Base woodpanel
            _rootPanel = GUIManager.Instance.CreateWoodpanel(
                parent: transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: Vector2.zero,
                width: 580f,
                height: 360f,
                draggable: true
            );
            _rootPanel.name = "PortalTweaks_ConfigPanel";
            _rootRect = _rootPanel.GetComponent<RectTransform>();

            // Title
            var titleGo = GUIManager.Instance.CreateText(
                text: "PORTAL CONFIGURATION",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -25f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 19,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 400f,
                height: 28f,
                addContentSizeFitter: false
            );
            _titleText = titleGo.GetComponent<Text>();
            _titleText.alignment = TextAnchor.MiddleCenter;

            // Row 1 - Left: Portal Tag
            _tagLabel = CreateTextElement("Portal Tag", new Vector2(-140f, -55f), 250f);
            var tagInputGo = GUIManager.Instance.CreateInputField(
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(-140f, -85f),
                contentType: InputField.ContentType.Standard,
                placeholderText: "Enter portal tag...",
                fontSize: 15,
                width: 250f,
                height: 32f
            );
            _tagInput = tagInputGo.GetComponent<InputField>();

            // Row 1 - Right: Portal Password
            _passLabel = CreateTextElement("Portal Password", new Vector2(140f, -55f), 250f);
            var passInputGo = GUIManager.Instance.CreateInputField(
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(140f, -85f),
                contentType: InputField.ContentType.Standard,
                placeholderText: "None (Unprotected)",
                fontSize: 15,
                width: 250f,
                height: 32f
            );
            _passwordInput = passInputGo.GetComponent<InputField>();

            // Row 2 - Left: Destination Dropdown
            _targetLabel = CreateTextElement("Outbound Destination", new Vector2(-140f, -125f), 250f);
            var dropdownGo = GUIManager.Instance.CreateDropDown(
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(-140f, -155f),
                fontSize: 14,
                width: 250f,
                height: 32f
            );
            _targetDropdown = dropdownGo.GetComponent<Dropdown>();
            _targetDropdown.onValueChanged.AddListener(_ => UpdateStatus());

            // Row 2 - Right: Destination Password
            _outPassLabel = CreateTextElement("Destination Password", new Vector2(140f, -125f), 250f);
            var outPassGo = GUIManager.Instance.CreateInputField(
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(140f, -155f),
                contentType: InputField.ContentType.Standard,
                placeholderText: "Password for target...",
                fontSize: 15,
                width: 250f,
                height: 32f
            );
            _outboundPasswordInput = outPassGo.GetComponent<InputField>();
            _outboundPasswordInput.onValueChanged.AddListener(_ => UpdateStatus());

            // Info Lines
            var distGo = GUIManager.Instance.CreateText(
                text: "",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -198f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 14,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 520f,
                height: 22f,
                addContentSizeFitter: false
            );
            _distanceText = distGo.GetComponent<Text>();
            _distanceText.alignment = TextAnchor.MiddleCenter;

            var energyGo = GUIManager.Instance.CreateText(
                text: "",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -220f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 14,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 520f,
                height: 22f,
                addContentSizeFitter: false
            );
            _energyText = energyGo.GetComponent<Text>();
            _energyText.alignment = TextAnchor.MiddleCenter;

            var weightGo = GUIManager.Instance.CreateText(
                text: "",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -220f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 14,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 520f,
                height: 22f,
                addContentSizeFitter: false
            );
            _weightText = weightGo.GetComponent<Text>();
            _weightText.alignment = TextAnchor.MiddleCenter;

            var statusGo = GUIManager.Instance.CreateText(
                text: "",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -244f),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 14,
                color: Color.yellow,
                outline: true,
                outlineColor: Color.black,
                width: 520f,
                height: 22f,
                addContentSizeFitter: false
            );
            _statusText = statusGo.GetComponent<Text>();
            _statusText.alignment = TextAnchor.MiddleCenter;

            // Buttons
            var saveBtnGo = GUIManager.Instance.CreateButton(
                text: "Save & Connect",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(-80f, -288f),
                width: 140f,
                height: 36f
            );
            _saveButton = saveBtnGo.GetComponent<Button>();
            _saveButton.onClick.AddListener(OnSaveClicked);

            var cancelBtnGo = GUIManager.Instance.CreateButton(
                text: "Cancel",
                parent: _rootPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(80f, -288f),
                width: 140f,
                height: 36f
            );
            _cancelButton = cancelBtnGo.GetComponent<Button>();
            _cancelButton.onClick.AddListener(Hide);

            _rootPanel.SetActive(false);
        }

        private Text CreateTextElement(string text, Vector2 pos, float width)
        {
            var go = GUIManager.Instance.CreateText(
                text: text,
                parent: _rootPanel!.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: pos,
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 14,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: width,
                height: 24f,
                addContentSizeFitter: false
            );
            var t = go.GetComponent<Text>();
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        /// <summary>
        /// Displays the configuration panel for the given portal, dynamically arranging the 2x2 grid.
        /// </summary>
        public void Show(TeleportWorld portal)
        {
            if (_rootPanel == null)
            {
                BuildUI();
            }

            _currentPortal = portal;
            if (_currentPortal == null || _rootPanel == null || _rootRect == null)
                return;

            var nview = _currentPortal.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
                return;

            var zdo = nview.GetZDO();
            var info = PortalDataManager.FromZDO(zdo);
            if (info == null)
                return;

            if (_titleText != null)
            {
                _titleText.text = ModConfig.IsStonePortal(_currentPortal) ? "STONE PORTAL CONFIGURATION" : "PORTAL CONFIGURATION";
            }

            bool hasPasswords = ModConfig.EnablePasswordSystem.Value;
            bool hasTargetSelect = ModConfig.EnableTargetSelectionUI.Value;

            // 1. Calculate Grid Dimensions and Positions
            float panelWidth;
            float col1X;
            float colWidth;

            if (hasPasswords)
            {
                panelWidth = 580f;
                col1X = -140f;
                colWidth = 250f;
            }
            else
            {
                panelWidth = 380f;
                col1X = 0f;
                colWidth = 290f;
            }

            // Reposition Row 1 - Left (Tag)
            SetPosAndSize(_tagLabel?.rectTransform, new Vector2(col1X, -55f), colWidth, 24f);
            SetPosAndSize(_tagInput?.GetComponent<RectTransform>(), new Vector2(col1X, -85f), colWidth, 32f);

            // Row 1 - Right (Portal Password)
            _passLabel?.gameObject.SetActive(hasPasswords);
            _passwordInput?.gameObject.SetActive(hasPasswords);
            if (hasPasswords)
            {
                SetPosAndSize(_passLabel?.rectTransform, new Vector2(140f, -55f), 250f, 24f);
                SetPosAndSize(_passwordInput?.GetComponent<RectTransform>(), new Vector2(140f, -85f), 250f, 32f);
            }

            // Row 2 - Left (Destination Dropdown)
            _targetLabel?.gameObject.SetActive(hasTargetSelect);
            _targetDropdown?.gameObject.SetActive(hasTargetSelect);
            if (hasTargetSelect)
            {
                SetPosAndSize(_targetLabel?.rectTransform, new Vector2(col1X, -125f), colWidth, 24f);
                SetPosAndSize(_targetDropdown?.GetComponent<RectTransform>(), new Vector2(col1X, -155f), colWidth, 32f);
            }

            // Row 2 - Right (Destination Password)
            bool showOutPass = hasPasswords && hasTargetSelect;
            _outPassLabel?.gameObject.SetActive(showOutPass);
            _outboundPasswordInput?.gameObject.SetActive(showOutPass);
            if (showOutPass)
            {
                SetPosAndSize(_outPassLabel?.rectTransform, new Vector2(140f, -125f), 250f, 24f);
                SetPosAndSize(_outboundPasswordInput?.GetComponent<RectTransform>(), new Vector2(140f, -155f), 250f, 32f);
            }

            // 2. Vertical Info Section Placement
            float infoY = hasTargetSelect ? -198f : -128f;

            _distanceText?.gameObject.SetActive(hasTargetSelect);
            if (hasTargetSelect && _distanceText != null)
            {
                SetPosAndSize(_distanceText.rectTransform, new Vector2(0f, infoY), panelWidth - 40f, 22f);
                infoY -= 22f;
            }

            bool showEnergy = ModConfig.EnableEnergySystem.Value;
            _energyText?.gameObject.SetActive(showEnergy);
            if (showEnergy && _energyText != null)
            {
                SetPosAndSize(_energyText.rectTransform, new Vector2(0f, infoY), panelWidth - 40f, 22f);
                infoY -= 22f;
            }

            if (_weightText != null)
            {
                SetPosAndSize(_weightText.rectTransform, new Vector2(0f, infoY), panelWidth - 40f, 22f);
                infoY -= 22f;
            }

            if (_statusText != null)
            {
                SetPosAndSize(_statusText.rectTransform, new Vector2(0f, infoY), panelWidth - 40f, 22f);
                infoY -= 32f;
            }

            // Buttons placement
            SetPosAndSize(_saveButton?.GetComponent<RectTransform>(), new Vector2(-80f, infoY), 140f, 36f);
            SetPosAndSize(_cancelButton?.GetComponent<RectTransform>(), new Vector2(80f, infoY), 140f, 36f);

            // Resize outer woodpanel
            float totalHeight = Mathf.Abs(infoY) + 55f;
            _rootRect.sizeDelta = new Vector2(panelWidth, totalHeight);

            // Populate current values
            if (_tagInput != null)
                _tagInput.text = info.Tag;

            if (_passwordInput != null)
                _passwordInput.text = info.Password;

            if (_outboundPasswordInput != null)
                _outboundPasswordInput.text = info.OutboundPassword;

            // Populate Dropdown
            if (hasTargetSelect && _targetDropdown != null)
            {
                _availablePortals = PortalDataManager.GetAllPortals(excludeId: info.Id);
                Vector3 myPos = _currentPortal.transform.position;
                _availablePortals.Sort((a, b) => Utils.DistanceXZ(myPos, a.Position).CompareTo(Utils.DistanceXZ(myPos, b.Position)));

                _targetDropdown.ClearOptions();

                var options = new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData("[ None / Unconnected ]")
                };

                int selectedIndex = 0;
                for (int i = 0; i < _availablePortals.Count; i++)
                {
                    var p = _availablePortals[i];
                    float dist = Utils.DistanceXZ(myPos, p.Position);
                    string lockTag = !string.IsNullOrEmpty(p.Password) ? " [Locked]" : "";
                    options.Add(new Dropdown.OptionData($"{p.DisplayName}{lockTag} ({dist:F0}m)"));

                    if (info.OutboundTarget != ZDOID.None && info.OutboundTarget == p.Id)
                    {
                        selectedIndex = i + 1; // offset by [None]
                    }
                }

                _targetDropdown.AddOptions(options);
                _targetDropdown.value = selectedIndex;
            }

            UpdateStatus();

            _rootPanel.SetActive(true);
            GUIManager.BlockInput(true);
        }

        private static void SetPosAndSize(RectTransform? rt, Vector2 pos, float width, float height)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, height);
        }

        public void Hide()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(false);
            }
            GUIManager.BlockInput(false);
            _currentPortal = null;
        }

        private void UpdateStatus()
        {
            if (_currentPortal == null)
                return;

            bool hasPasswords = ModConfig.EnablePasswordSystem.Value;
            bool hasTargetSelect = ModConfig.EnableTargetSelectionUI.Value;

            // 1. Live Carry Weight Status
            if (_weightText != null)
            {
                Player player = Player.m_localPlayer;
                if (player != null && player.GetInventory() != null)
                {
                    float currentWeight = player.GetInventory().GetTotalWeight();
                    float maxWeight = ModConfig.GetMaxWeight(_currentPortal);
                    if (ModConfig.EnableMaxWeightLimit.Value && currentWeight > maxWeight)
                    {
                        _weightText.text = $"<color=red>Carry Weight: {currentWeight:F0} / {maxWeight:F0} lbs (Overweight)</color>";
                    }
                    else
                    {
                        _weightText.text = $"<color=#cccccc>Carry Weight: {currentWeight:F0} / {maxWeight:F0} lbs</color>";
                    }
                }
            }

            // 2. Live Destination Distance & Password Mismatch Checks
            bool allowSave = true;
            string statusMsg = "";
            float? selectedTargetDist = null;

            if (hasTargetSelect && _targetDropdown != null)
            {
                int index = _targetDropdown.value;
                if (index > 0 && index - 1 < _availablePortals.Count)
                {
                    var targetPortal = _availablePortals[index - 1];
                    float dist = Utils.DistanceXZ(_currentPortal.transform.position, targetPortal.Position);
                    selectedTargetDist = dist;

                    // Distance status
                    if (_distanceText != null)
                    {
                        float maxDist = ModConfig.GetMaxDistance(_currentPortal);
                        float minDist = ModConfig.GetMinDistance(_currentPortal);

                        if (ModConfig.EnableDistanceLimit.Value && dist > maxDist)
                        {
                            _distanceText.text = $"<color=red>Distance: {dist:F0}m (Exceeds {maxDist:F0}m limit)</color>";
                            statusMsg = "Destination is too far away!";
                            allowSave = false;
                        }
                        else if (ModConfig.EnableMinDistanceLimit.Value && dist < minDist)
                        {
                            _distanceText.text = $"<color=red>Distance: {dist:F0}m (Below {minDist:F0}m limit)</color>";
                            statusMsg = "Destination is too close!";
                            allowSave = false;
                        }
                        else
                        {
                            _distanceText.text = $"<color=#80c0ff>Distance to target: {dist:F0}m</color>";
                        }
                    }

                    // Password check in State 4
                    if (hasPasswords)
                    {
                        string suppliedOutboundPass = _outboundPasswordInput != null ? _outboundPasswordInput.text.Trim() : "";
                        if (!PortalDataManager.ValidatePasswordMatch(targetPortal.Password, suppliedOutboundPass, out string passErr))
                        {
                            statusMsg = "<color=red>Destination is password-protected! Incorrect password.</color>";
                            allowSave = false;
                        }
                        else if (!string.IsNullOrEmpty(targetPortal.Password))
                        {
                            statusMsg = "<color=#80ff80>Destination password matched.</color>";
                        }
                    }
                }
                else
                {
                    if (_distanceText != null)
                        _distanceText.text = "<color=#aaaaaa>No destination selected (Portal will be unlinked).</color>";
                }
            }
            else
            {
                if (_distanceText != null)
                    _distanceText.text = "";
            }

            // 1b. Live Portal Energy Status
            if (ModConfig.EnableEnergySystem.Value && _energyText != null)
            {
                if (EnergyManager.GetCurrentEnergy(_currentPortal, out float currentEnergy, out float maxEnergy, out float rechargeRate))
                {
                    Player player = Player.m_localPlayer;
                    float cost = EnergyManager.CalculateTeleportCost(player, _currentPortal, out _, out string breakdown, selectedTargetDist);
                    if (currentEnergy >= cost)
                    {
                        _energyText.text = $"<color=#ffd700>Portal Energy: {currentEnergy:F0} / {maxEnergy:F0} (+{rechargeRate:F1}/s) | Jump Cost: {cost:F0} ({breakdown})</color>";
                    }
                    else if (ModConfig.LowEnergyAction.Value == LowEnergyBehavior.ExtendSickness)
                    {
                        float extraSec = EnergyManager.CalculateOverdraftExtraSickness(cost - currentEnergy);
                        if (cost > maxEnergy)
                        {
                            string capacityReason = EnergyManager.GetExceedsCapacityReason(player, _currentPortal, maxEnergy, selectedTargetDist);
                            _energyText.text = $"<color=#ffaa55>Portal Overdraft: {currentEnergy:F0} / {maxEnergy:F0} (Cost: {cost:F0} - {capacityReason}) (+{extraSec:F0}s sickness)</color>";
                        }
                        else
                        {
                            float sec = EnergyManager.GetRechargeTimeRemaining(currentEnergy, cost, rechargeRate);
                            _energyText.text = $"<color=#ffaa55>Portal Overdraft: {currentEnergy:F0} / {maxEnergy:F0} (+{extraSec:F0}s sickness! Ready in ~{sec:F0}s)</color>";
                        }
                    }
                    else if (cost > maxEnergy)
                    {
                        string capacityReason = EnergyManager.GetExceedsCapacityReason(player, _currentPortal, maxEnergy, selectedTargetDist);
                        _energyText.text = $"<color=#ff8080>Portal Energy: {currentEnergy:F0} / {maxEnergy:F0} (Cost: {cost:F0} - {capacityReason})</color>";
                    }
                    else
                    {
                        float sec = EnergyManager.GetRechargeTimeRemaining(currentEnergy, cost, rechargeRate);
                        _energyText.text = $"<color=#ff8080>Portal Energy: {currentEnergy:F0} / {maxEnergy:F0} (Cost: {cost:F0} - Recharging: ~{sec:F0}s)</color>";
                    }
                }
            }

            if (TeleportPatch.HasProximityConflict(_currentPortal, out float proxDist))
            {
                float minAllowed = ModConfig.MinDistanceBetweenPortals.Value;
                string proxWarning = $"<color=red>Interference: Portal is {proxDist:F0}m from another portal (min {minAllowed:F0}m)!</color>";
                statusMsg = string.IsNullOrEmpty(statusMsg) ? proxWarning : $"{statusMsg}\n{proxWarning}";
            }

            if (ModConfig.EnableTeleportSickness.Value && ModConfig.SicknessBlockTeleport.Value &&
                TeleportSicknessManager.HasSickness(Player.m_localPlayer, out float sicknessRemaining))
            {
                string sickWarning = $"<color=red>Teleportation sickness active ({sicknessRemaining:F0}s remaining)!</color>";
                statusMsg = string.IsNullOrEmpty(statusMsg) ? sickWarning : $"{statusMsg}\n{sickWarning}";
            }

            if (_statusText != null)
            {
                _statusText.text = statusMsg;
            }

            if (_saveButton != null)
            {
                _saveButton.interactable = allowSave;
            }
        }

        private void OnSaveClicked()
        {
            if (_currentPortal == null)
            {
                Hide();
                return;
            }

            string newTag = _tagInput != null ? _tagInput.text.Trim() : "";
            string newPassword = _passwordInput != null ? _passwordInput.text.Trim() : "";
            ZDOID targetId = ZDOID.None;
            string outboundPassword = _outboundPasswordInput != null ? _outboundPasswordInput.text.Trim() : "";

            bool hasTargetSelect = ModConfig.EnableTargetSelectionUI.Value;
            if (hasTargetSelect && _targetDropdown != null)
            {
                int index = _targetDropdown.value;
                if (index > 0 && index - 1 < _availablePortals.Count)
                {
                    targetId = _availablePortals[index - 1].Id;
                }
            }

            PortalDataManager.SavePortal(
                _currentPortal,
                newTag,
                newPassword,
                targetId,
                outboundPassword,
                updateTarget: hasTargetSelect
            );

            Player.m_localPlayer?.Message(
                MessageHud.MessageType.Center,
                hasTargetSelect && targetId != ZDOID.None ? "Portal destination routed!" : "Portal configuration saved."
            );

            Hide();
        }
    }
}
