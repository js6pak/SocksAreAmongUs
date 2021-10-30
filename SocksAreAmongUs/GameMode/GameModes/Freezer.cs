using System;
using BepInEx.Configuration;
using CodeIsNotAmongUs;
using HarmonyLib;
using Hazel;
using Il2CppSystem.IO;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using SocksAreAmongUs.Effects;
using TMPro;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Freezer : BaseGameMode
    {
        public override string Id => "freezer";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Freezer || Terminator.Enabled;

        public static bool Active { get; set; }
        public static bool IsActive => Active && !PlayerControl.LocalPlayer.Data.IsImpostor;

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(ButtonManager.MaxTimer.Value);
            writer.Write(ButtonManager.MaxRemainingTimer.Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ButtonManager.MaxTimer.Value = reader.ReadSingle();
            ButtonManager.MaxRemainingTimer.Value = reader.ReadSingle();
        }

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Freeze.png").DontUnload();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
        public static class PlayerControl_ReportDeadBodyPatch
        {
            public static bool Prefix()
            {
                return !IsActive;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        public static class CmdReportDeadBodyPatch
        {
            public static bool Prefix()
            {
                return !IsActive;
            }
        }

        [HarmonyPatch(typeof(Minigame), nameof(Minigame.Begin))]
        public static class MinigamePatch
        {
            public static bool Prefix()
            {
                return !IsActive;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CanMove), MethodType.Getter)]
        public static class CanMovePatch
        {
            public static bool Prefix(PlayerControl __instance, ref bool __result)
            {
                if (Active && !__instance.Data.IsImpostor && !__instance.Data.IsDead)
                {
                    return __result = false;
                }

                return true;
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

                gameObject.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                timerText = GetComponentInChildren<TextMeshPro>();

                var passiveButton = GetComponent<PassiveButton>();

                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((Action) OnClick);

                timer = MaxTimer.Value;
                CooldownHelpers.SetCooldownNormalizedUvs(renderer);
                IsActive = false;
                UpdateCoolDown();
            }

            public void Reset()
            {
                timer = MaxTimer.Value;
                remainingTimer = MaxRemainingTimer.Value;
            }

            public void OnClick()
            {
                if (timer > 0 || !IsActive)
                    return;

                Rpc<RpcSetFreezer>.Instance.Send(true);
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

                if (remainingTimer > 0)
                {
                    remainingTimer = Mathf.Clamp(remainingTimer - Time.fixedDeltaTime, 0, MaxRemainingTimer.Value);

                    if (remainingTimer == 0)
                    {
                        Rpc<RpcSetFreezer>.Instance.Send(false);
                    }
                }
            }

            public SpriteRenderer renderer;
            public TextMeshPro timerText;

            public bool isCoolingDown = true;
            private bool _isActive;

            public float timer;
            public float remainingTimer;

            internal static ConfigEntry<float> MaxTimer;
            internal static ConfigEntry<float> MaxRemainingTimer;
        }

        public override void BindConfig(ConfigFile config)
        {
            ButtonManager.MaxTimer = config.Bind("Freezer", "Max timer", 20f);
            ButtonManager.MaxRemainingTimer = config.Bind("Freezer", "Max remaining timer", 10f);
        }

        private static ButtonManager _buttonManager;

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class InvisibilityButtonPatch
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
                Active = false;
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetFreezer)]
        public class RpcSetFreezer : PlayerCustomRpc<SocksAreAmongUsPlugin, bool>
        {
            public RpcSetFreezer(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

            public override void Write(MessageWriter writer, bool data)
            {
                writer.Write(data);
            }

            public override bool Read(MessageReader reader)
            {
                return reader.ReadBoolean();
            }

            public override void Handle(PlayerControl target, bool value)
            {
                Active = value;

                var camera = Camera.main;
                if (camera == null)
                {
                    throw new NullReferenceException("Camera.main");
                }

                if (Enabled)
                {
                    var effect = camera.GetComponent<FrostEffect>();

                    if (!effect)
                    {
                        effect = camera.gameObject.AddComponent<FrostEffect>();
                    }

                    Coroutines.Start(effect.SetActive(value));
                }
                else
                {
                    if (value)
                    {
                        camera.gameObject.AddComponent<GrayScaleEffect>();
                    }
                    else
                    {
                        camera.gameObject.GetComponent<GrayScaleEffect>().Destroy();
                    }
                }

                if (value)
                {
                    _buttonManager.Reset();
                }

                if (Minigame.Instance)
                {
                    Minigame.Instance.Close();
                }

                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player.Data.IsImpostor)
                    {
                        continue;
                    }

                    player.MyPhysics.body.velocity = Vector2.zero;
                }
            }
        }
    }
}
