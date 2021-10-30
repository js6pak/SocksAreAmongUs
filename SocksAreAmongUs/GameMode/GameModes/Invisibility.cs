using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using TMPro;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using BinaryReader = Il2CppSystem.IO.BinaryReader;
using BinaryWriter = Il2CppSystem.IO.BinaryWriter;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Invisibility : BaseGameMode
    {
        public override string Id => "invisibility";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Invisibility || Terminator.Enabled;

        public static Dictionary<byte, bool> Invisible { get; } = new Dictionary<byte, bool>();

        public override void BindConfig(ConfigFile config)
        {
            ButtonManager.MaxTimer = config.Bind("Invisibility", "Max timer", 20f);
            ButtonManager.MaxRemainingTimer = config.Bind("Invisibility", "Max remaining timer", 10f);
        }

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Invisibility.png").DontUnload();
        }

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

        public static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
        {
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        public static void SetAlpha(TextRenderer textRenderer, float alpha)
        {
            var color = textRenderer.Color;
            color.a = alpha;
            textRenderer.Color = color;

            color = textRenderer.OutlineColor;
            color.a = alpha;
            textRenderer.OutlineColor = color;
        }

        public static void SetAlpha(TextMeshPro text, float alpha)
        {
            var color = text.color;
            color.a = alpha;
            text.color = color;
        }

        public static void SetAlpha(PlayerControl playerControl, float alpha)
        {
            SetAlpha(playerControl.myRend, alpha);
            SetAlpha(playerControl.MyPhysics.Skin.layer, alpha);
            playerControl.SetHatAlpha(alpha);
            if (playerControl.CurrentPet)
            {
                SetAlpha(playerControl.CurrentPet.rend, alpha);
            }

            SetAlpha(playerControl.nameText, alpha);
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

            public void OnClick()
            {
                if (timer > 0 || !IsActive)
                    return;

                timer = MaxTimer.Value;
                remainingTimer = MaxRemainingTimer.Value;
                Rpc<RpcSetVisible>.Instance.Send(true);
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
                        Rpc<RpcSetVisible>.Instance.Send(false);
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
                if (!Enabled)
                    return;

                Invisible.Clear();
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Visible), MethodType.Setter)]
        public static class VisiblePatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] bool value)
            {
                if (!Enabled || !value)
                    return true;

                return !Invisible.GetValueSafe(__instance.PlayerId);
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        public static class MeetingHudPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                Invisible.Clear();
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class InvisibilityPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled || PlayerControl.LocalPlayer == null)
                    return;

                var data = PlayerControl.LocalPlayer.Data;

                if (Invisible.GetValueSafe(__instance.PlayerId))
                {
                    if (data.IsImpostor && __instance.Data.IsImpostor || data.IsDead)
                    {
                        SetAlpha(__instance, 0.25f);
                    }
                    else
                    {
                        __instance.Visible = false;
                    }
                }
                else
                {
                    SetAlpha(__instance, 1f);
                    __instance.Visible = !__instance.inVent && (!__instance.Data.IsDead || data.IsDead);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
        public static class HatInvisibilityPatch
        {
            public static void Postfix(PlayerPhysics __instance)
            {
                if (!Enabled || PlayerControl.LocalPlayer == null)
                    return;

                var data = PlayerControl.LocalPlayer.Data;

                var playerControl = __instance.myPlayer;

                if (data.IsImpostor && playerControl.Data.IsImpostor || data.IsDead)
                {
                    if (Invisible.GetValueSafe(playerControl.PlayerId))
                    {
                        if (data.IsImpostor && playerControl.Data.IsImpostor || data.IsDead)
                        {
                            playerControl.SetHatAlpha(0.25f);
                        }
                    }
                    else
                    {
                        playerControl.SetHatAlpha(1f);
                    }
                }
            }
        }
    }

    [RegisterCustomRpc((uint) CustomRpcCalls.SetVisible)]
    public class RpcSetVisible : PlayerCustomRpc<SocksAreAmongUsPlugin, bool>
    {
        public RpcSetVisible(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
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

        public override void Handle(PlayerControl innerNetObject, bool data)
        {
            Invisibility.Invisible[innerNetObject.PlayerId] = data;
        }
    }
}
