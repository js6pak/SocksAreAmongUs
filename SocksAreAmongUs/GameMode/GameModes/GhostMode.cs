using System;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class GhostMode : BaseGameMode
    {
        public override string Id => "ghost_mode";
        internal static bool Enabled => GameModeManager.CurrentGameMode is GhostMode || Terminator.Enabled;

        public static Dictionary<int, bool> GhostModes { get; } = new Dictionary<int, bool>();

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/GhostMode.png").DontUnload();
        }

        public override void Cleanup()
        {
            Rpc<RpcSetGhostMode>.Instance.Send(false);
            _buttonManager.IsActive = false;
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

                Destroy(GetComponentInChildren<TextRenderer>());

                var passiveButton = GetComponent<PassiveButton>();

                passiveButton.OnClick.RemoveAllListeners();
                passiveButton.OnClick.AddListener((Action) OnClick);

                IsActive = false;
            }

            public void OnClick()
            {
                bool value;
                value = !(GhostModes.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out value) && value);
                Rpc<RpcSetGhostMode>.Instance.Send(value);
                IsActive = value;
            }

            private bool _isActive;

            [HideFromIl2Cpp]
            public bool IsActive
            {
                get => _isActive;
                set
                {
                    _isActive = value;

                    renderer.color = value ? Palette.EnabledColor : Palette.DisabledClear;
                    renderer.material.SetFloat("_Desat", value ? 0f : 1f);
                }
            }

            public SpriteRenderer renderer;
        }

        private static ButtonManager _buttonManager;

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class ButtonPatch
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

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class FixedUpdatePatch
        {
            private static readonly int _mask = Shader.PropertyToID("_Mask");
            private static readonly int _ghost = LayerMask.NameToLayer("Ghost");
            private static readonly int _players = LayerMask.NameToLayer("Players");

            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled)
                    return;

                if (!__instance.nameText)
                    return;

                var data = __instance.Data;
                var isGhost = GhostModes.TryGetValue(data.PlayerId, out var value) && value && data.IsImpostor;

                __instance.gameObject.layer = data.IsDead || isGhost ? _ghost : _players;

                if (isGhost)
                {
                    __instance.nameText.renderer.material.SetInt(_mask, 0);

                    if (__instance.AmOwner)
                    {
                        DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
                    }
                }
                else if (!data.IsDead)
                {
                    __instance.nameText.renderer.material.SetInt(_mask, 4);

                    if (__instance.AmOwner)
                    {
                        DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(true);
                    }
                }
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetGhostMode)]
        public class RpcSetGhostMode : PlayerCustomRpc<SocksAreAmongUsPlugin, bool>
        {
            public RpcSetGhostMode(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
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
                GhostModes[target.PlayerId] = value;
            }
        }
    }
}
