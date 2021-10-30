using System;
using System.Collections;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using SocksAreAmongUs.GameMode.GameModes;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class ThanosRole : CustomRole
    {
        public override string Name => "Thanos";
        public override Color? Color => Palette.Purple;
        public override string Description => null;
        public override RoleSide Side => RoleSide.Impostor;
        public override bool ShouldColorName(PlayerControl player) => PlayerControl.LocalPlayer.Data.IsImpostor;

        private static Sprite _mindAsset;
        private static Sprite _realityAsset;
        private static Sprite _soulBackgroundAsset;
        private static Sprite _soulBodyAsset;
        private static Sprite _powerAsset;
        private static Sprite _spaceAsset;
        private static Sprite _timeAsset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _mindAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/Mind.png").DontUnload();
            _realityAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/Reality.png").DontUnload();
            _soulBackgroundAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/SoulBackground.png").DontUnload();
            _soulBodyAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/SoulBody.png").DontUnload();
            _powerAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/Power.png").DontUnload();
            _spaceAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/Space.png").DontUnload();
            _timeAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Thanos/Time.png").DontUnload();
        }

        private static SpaceButtonManager _spaceButtonManager;

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class ButtonsPatch
        {
            public static void Postfix()
            {
                static bool IsActive()
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    return CustomRoles.Thanos.Test(data) && data.IsImpostor && !data.IsDead;
                }

                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<MindButtonManager>(), IsActive));
                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<RealityButtonManager>(), IsActive));
                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<SoulButtonManager>(), IsActive));
                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<PowerButtonManager>(), IsActive));
                CustomButton.Buttons.Add(new CustomButton(_spaceButtonManager = Extensions.CreateButton<SpaceButtonManager>(), IsActive));
                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<TimeButtonManager>(), IsActive));
            }
        }

        private static IEnumerator DelayAction(Action action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        [RegisterInIl2Cpp]
        public class MindButtonManager : CustomButtonBehaviour
        {
            public MindButtonManager(IntPtr ptr) : base(ptr)
            {
                Menu = new MindMenu(this);
            }

            [HideFromIl2Cpp]
            public MindMenu Menu { get; }

            private void OnDestroy()
            {
                Menu?.Hide();
            }

            public override bool OnClick()
            {
                if (timer > 0 || !IsActive)
                    return false;

                Menu.Toggle();

                return true;
            }

            public override Sprite Sprite { get; } = _mindAsset;
            public override float MaxTimer => 60;

            public class MindMenu : PlayerSelectMenu
            {
                private readonly MindButtonManager _buttonManager;

                public MindMenu(MindButtonManager buttonManager)
                {
                    _buttonManager = buttonManager;
                }

                private LightSource _light;

                protected override void UpdateButton(PlayerControl player, Button button, Text text, Image image)
                {
                    if (player.Data.IsDead)
                    {
                        button.gameObject.Destroy();
                        return;
                    }

                    base.UpdateButton(player, button, text, image);
                }

                public override void OnClick(PlayerControl playerControl)
                {
                    if (_buttonManager.timer > 0 || !_buttonManager.IsActive)
                        return;

                    _buttonManager.timer = _buttonManager.MaxTimer;
                    Control(playerControl);
                    Coroutines.Start(DelayAction(
                        () => Control(PlayerControl.LocalPlayer),
                        10
                    ));
                }

                private void Control(PlayerControl playerControl)
                {
                    if (!_light)
                    {
                        _light = PlayerControl.LocalPlayer.myLight;
                    }

                    PlayerControl.LocalPlayer.MyPhysics.body.velocity = Vector2.zero;
                    Rpc<MindControl.RpcMindControl>.Instance.Send(playerControl);
                    Camera.main!.GetComponent<FollowerCamera>().SetTarget(playerControl);
                    var lightTransform = _light.transform;
                    lightTransform.SetParent(playerControl.transform);
                    lightTransform.localPosition = playerControl.Collider.offset;
                }

                protected override bool ArePlayersEqual(PlayerControl a, PlayerControl b)
                {
                    return MindControl.Controlled.Forward.TryGetValue(a, out var controlled) ? controlled.Equals(b) : base.ArePlayersEqual(a, b);
                }
            }
        }

        [RegisterInIl2Cpp]
        public class RealityButtonManager : CustomButtonBehaviour
        {
            public RealityButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            [HideFromIl2Cpp]
            public override bool OnClick()
            {
                if (base.OnClick())
                {
                    RandomMap.ChangeMapToRandom();
                }

                return false;
            }

            public override Sprite Sprite { get; } = _realityAsset;
            public override float MaxTimer => 120;
        }

        [RegisterInIl2Cpp]
        public class SoulButtonManager : CustomButtonBehaviour
        {
            public SoulButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            protected override void SetSprite()
            {
                renderer = GetComponent<SpriteRenderer>();
                var material = renderer.material;
                renderer.Destroy();

                renderer = AddLayer(_soulBodyAsset, new Material(PlayerSelectMenu.PlayerShaderAsset));
                AddLayer(_soulBackgroundAsset, material);
            }

            public override bool OnClick()
            {
                if (base.OnClick())
                {
                    RpcSetImpostor.Send(_lastTarget, true);
                    _used = true;
                    IsActive = false;
                    timer = MaxTimer;
                    UpdateCoolDown();
                    timerText.gameObject.Destroy();
                }

                return false;
            }

            private PlayerControl _lastTarget;
            private bool? _lastIsCoolingDown;
            public override float MaxTimer => 60;
            private bool _used;

            public override void FixedUpdate()
            {
                if (_used)
                {
                    return;
                }

                UpdateTimer();

                var target = HudManager.Instance.KillButton.CurrentTarget;

                if (!PlayerControl.Equals(target, _lastTarget) || _lastIsCoolingDown != isCoolingDown)
                {
                    IsActive = target && !isCoolingDown;

                    if (target)
                    {
                        PlayerControl.SetPlayerMaterialColors(target.Data.ColorId, renderer);
                    }

                    _lastTarget = target;
                }

                _lastIsCoolingDown = isCoolingDown;
            }
        }

        [RegisterInIl2Cpp]
        public class PowerButtonManager : CustomButtonBehaviour
        {
            public PowerButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            [HideFromIl2Cpp]
            public override bool OnClick()
            {
                if (base.OnClick())
                {
                    var oldKillCooldown = PlayerControl.GameOptions.KillCooldown;

                    PlayerControl.LocalPlayer.myRend.SetOutline(Palette.Purple);
                    PlayerControl.GameOptions.KillCooldown = 0;
                    PlayerControl.LocalPlayer.SetKillTimer(0);

                    Coroutines.Start(DelayAction(
                        () =>
                        {
                            PlayerControl.LocalPlayer.myRend.SetOutline(null);
                            PlayerControl.GameOptions.KillCooldown = oldKillCooldown;
                            PlayerControl.LocalPlayer.SetKillTimer(oldKillCooldown);
                        },
                        5
                    ));
                }

                return false;
            }

            public override Sprite Sprite { get; } = _powerAsset;
            public override float MaxTimer => 60;
        }

        [RegisterInIl2Cpp]
        public class SpaceButtonManager : CustomButtonBehaviour
        {
            public SpaceButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            public static bool IsOpen { get; set; }

            [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
            public static class MapPatch
            {
                public static void Postfix(MapBehaviour __instance)
                {
                    if (IsOpen && Input.GetMouseButtonDown(0))
                    {
                        if (Teleport.TeleportToMouse(__instance))
                        {
                            _spaceButtonManager.timer = _spaceButtonManager.MaxTimer;
                        }

                        IsOpen = false;
                        __instance.Close();
                    }
                }
            }

            [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close))]
            private class ClosePatch
            {
                public static bool Prefix()
                {
                    return !IsOpen;
                }
            }

            [HideFromIl2Cpp]
            public override bool OnClick()
            {
                if (timer > 0 || !IsActive)
                    return false;

                IsOpen = true;

                HudManager.Instance.ShowMap((Action<MapBehaviour>) (mapBehaviour =>
                {
                    if (!PlayerControl.LocalPlayer.CanMove)
                    {
                        return;
                    }

                    mapBehaviour.transform.FindChild("CloseButton").gameObject.SetActive(false);

                    PlayerControl.LocalPlayer.SetPlayerMaterialColors(mapBehaviour.HerePoint);
                    mapBehaviour.GenericShow();
                    mapBehaviour.ColorControl.SetColor(Palette.Purple);
                    HudManager.Instance.SetHudActive(false);
                }));


                return true;
            }

            public override Sprite Sprite { get; } = _spaceAsset;
            public override float MaxTimer => 30;
        }

        [RegisterInIl2Cpp]
        public class TimeButtonManager : CustomButtonBehaviour
        {
            public TimeButtonManager(IntPtr ptr) : base(ptr)
            {
            }

            [HideFromIl2Cpp]
            public override bool OnClick()
            {
                if (base.OnClick())
                {
                    Rpc<Freezer.RpcSetFreezer>.Instance.Send(true);
                    Coroutines.Start(DelayAction(
                        () => Rpc<Freezer.RpcSetFreezer>.Instance.Send(false),
                        10
                    ));
                }

                return false;
            }

            public override Sprite Sprite { get; } = _timeAsset;
            public override float MaxTimer => 30;
        }
    }
}
