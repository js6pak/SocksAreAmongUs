using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel;
using Il2CppSystem.IO;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using TMPro;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Morphing : BaseGameMode
    {
        public override string Id => "morphing";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Morphing || Terminator.Enabled;

        public static Dictionary<int, int> Morphed { get; } = new Dictionary<int, int>();
        public static Dictionary<int, Character> Characters { get; } = new Dictionary<int, Character>();

        public override void BindConfig(ConfigFile config)
        {
            base.BindConfig(config);
            ButtonManager.MaxTimer = config.Bind("Morphing", "Max timer", 20f);
        }

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Player.png").DontUnload();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(ButtonManager.MaxTimer.Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ButtonManager.MaxTimer.Value = reader.ReadSingle();
        }

        public class Character
        {
            public string PlayerName { get; set; }
            public int ColorId { get; set; }
            public uint HatId { get; set; }
            public uint PetId { get; set; }
            public uint SkinId { get; set; }

            public Character(GameData.PlayerInfo info)
            {
                PlayerName = info.PlayerName;
                ColorId = info.ColorId;
                HatId = info.HatId;
                PetId = info.PetId;
                SkinId = info.SkinId;
            }

            public void Apply(PlayerControl playerControl)
            {
                playerControl.SetName(PlayerName);
                playerControl.SetColor(ColorId);
                playerControl.SetHat(HatId, playerControl.Data.ColorId);
                playerControl.SetPet(PetId);
                playerControl.SetSkin(SkinId);
            }
        }

        [RegisterInIl2Cpp]
        public class ButtonManager : MonoBehaviour
        {
            public ButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            public void Start()
            {
                renderer = GetComponent<SpriteRenderer>();
                renderer.sprite = _asset;

                gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

                timerText = GetComponentInChildren<TextMeshPro>();

                var passiveButton = GetComponent<PassiveButton>();

                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((Action) OnClick);

                timer = MaxTimer.Value;
                CooldownHelpers.SetCooldownNormalizedUvs(renderer);
                IsActive = false;
                UpdateCoolDown();
            }

            [HideFromIl2Cpp]
            public MorphMenu Menu { get; } = new MorphMenu();

            private void OnDestroy()
            {
                Menu?.Hide();
            }

            public void OnClick()
            {
                if (timer > 0 || !IsActive)
                    return;

                Menu.Toggle();
            }

            [HideFromIl2Cpp]
            public bool IsActive
            {
                get => _isActive;
                set
                {
                    _isActive = value;

                    if (value)
                    {
                        renderer.color = Palette.EnabledColor;
                        renderer.material.SetFloat("_Desat", 0f);
                    }
                    else
                    {
                        renderer.color = Palette.DisabledClear;
                        renderer.material.SetFloat("_Desat", 1f);
                    }
                }
            }

            public void UpdateCoolDown()
            {
                var num = Mathf.Clamp(timer / MaxTimer.Value, 0f, 1f);
                if (renderer)
                {
                    renderer.material.SetFloat("_Percent", num);
                }

                isCoolingDown = num > 0f;
                IsActive = !isCoolingDown;

                if (isCoolingDown)
                {
                    timerText.text = Mathf.CeilToInt(timer).ToString();
                    timerText.gameObject.SetActive(true);
                    return;
                }

                timerText.gameObject.SetActive(false);
            }

            public void FixedUpdate()
            {
                var playerControl = PlayerControl.LocalPlayer;
                var data = playerControl.Data;

                if (timer > 0 && data.IsImpostor && playerControl.CanMove && !data.IsDead)
                {
                    timer = Mathf.Clamp(timer - Time.fixedDeltaTime, 0, MaxTimer.Value);
                    UpdateCoolDown();
                }
            }

            public SpriteRenderer renderer;
            public TextMeshPro timerText;

            public bool isCoolingDown = true;
            private bool _isActive;

            public float timer;
            public static ConfigEntry<float> MaxTimer { get; internal set; }

            public class MorphMenu : PlayerSelectMenu
            {
                public override void OnClick(PlayerControl playerControl)
                {
                    if (_buttonManager.timer > 0 || !_buttonManager.IsActive)
                        return;

                    _buttonManager.timer = MaxTimer.Value;
                    Rpc<RpcSetMorphed>.Instance.Send(playerControl.PlayerId);
                }

                protected override bool ArePlayersEqual(PlayerControl a, PlayerControl b)
                {
                    return Morphed.TryGetValue(a.PlayerId, out var current) ? current == b.PlayerId : base.ArePlayersEqual(a, b);
                }
            }
        }

        private static ButtonManager _buttonManager;

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class HudManagerPatch
        {
            public static void Postfix(HudManager __instance)
            {
                var gameObject = Object.Instantiate(__instance.KillButton.gameObject, __instance.transform.parent);
                Object.DestroyImmediate(gameObject.GetComponent<KillButtonManager>());
                _buttonManager = gameObject.AddComponent<ButtonManager>();

                CustomButton.Buttons.Add(new CustomButton(_buttonManager, () =>
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    return Enabled && data.IsImpostor && !data.IsDead;
                }));
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                Morphed.Clear();
                Characters.Clear();

                foreach (var info in GameData.Instance.AllPlayers)
                {
                    Characters[info.PlayerId] = new Character(info);
                }
            }
        }

        private static void Reset()
        {
            foreach (var morphed in Morphed)
            {
                var playerControl = GameData.Instance.GetPlayerById((byte) morphed.Key)?.Object;

                if (playerControl != null)
                {
                    Characters[morphed.Key].Apply(playerControl);
                }
            }

            Morphed.Clear();
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Awake))]
        public static class MeetingHudPatch
        {
            public static void Prefix()
            {
                if (!Enabled)
                    return;

                Reset();
            }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        public static class OnGameEndPatch
        {
            public static void Prefix()
            {
                if (!Enabled)
                    return;

                Reset();
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetMorphed)]
        public class RpcSetMorphed : PlayerCustomRpc<SocksAreAmongUsPlugin, byte>
        {
            public RpcSetMorphed(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

            public override void Write(MessageWriter writer, byte character)
            {
                writer.Write(character);
            }

            public override byte Read(MessageReader reader)
            {
                return reader.ReadByte();
            }

            public override void Handle(PlayerControl target, byte character)
            {
                Morphed[target.PlayerId] = character;
                Characters[character].Apply(target);
            }
        }
    }
}
