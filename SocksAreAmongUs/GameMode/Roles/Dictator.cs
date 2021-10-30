using System;
using System.Collections.Generic;
using System.Reflection;
using CodeIsNotAmongUs.Patches.RemovePlayerLimit;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using Reactor.Unstrip;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class DictatorRole : CustomRole
    {
        public override string Name => "Dictator";
        public override Color? Color => new Color32(210, 105, 30, 255);
        public override string Description => "You can rule meetings as you wish";
        public override RoleSide Side => RoleSide.Crewmate;

        public override bool ShouldColorName(PlayerControl player) => player.AmOwner;

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Dictator.png").DontUnload();
        }

        private static GameData.PlayerInfo _exiled;

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        public static class StartPatch
        {
            public static void Postfix()
            {
                _exiled = null;
            }
        }

        [HarmonyPatch(typeof(MeetingPatches), "VotingComplete")]
        public static class VotingCompletePatch
        {
            public static void Prefix(ref GameData.PlayerInfo exiled, ref bool tie)
            {
                if (_exiled != null)
                {
                    exiled = _exiled;
                    tie = false;
                    _exiled = null;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
        public static class SelectPatch
        {
            public static void Postfix(PlayerVoteArea __instance)
            {
                if (__instance.Buttons.active)
                {
                    var exileButton = __instance.Buttons.transform.FindChild("ExileButton");

                    if (exileButton)
                    {
                        exileButton.gameObject.SetActive(_exiled == null);
                    }
                }
            }
        }

        // [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CreateButton))]
        [HarmonyPatch]
        public static class CreateButtonPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(MeetingHud).GetMethods(typeof(PlayerVoteArea), typeof(GameData.PlayerInfo));
            }

            public static void Postfix([HarmonyArgument(0)] GameData.PlayerInfo playerInfo, PlayerVoteArea __result)
            {
                if (!CustomRoles.Dictator.Test(PlayerControl.LocalPlayer.Data))
                    return;

                var exileButton = Object.Instantiate(__result.Buttons.transform.FindChild("ConfirmButton").gameObject, __result.Buttons.transform);
                exileButton.name = "ExileButton";
                exileButton.transform.localPosition -= new Vector3(0.62f, 0, 0);

                exileButton.GetComponent<SpriteRenderer>().sprite = _asset;

                // exileButton.GetComponent<PassiveButton>().DestroyImmediate();
                // var passiveButton = exileButton.AddComponent<PassiveButton>();
                var passiveButton = exileButton.GetComponent<PassiveButton>();

                // passiveButton.Colliders = new Il2CppReferenceArray<Collider2D>(new Collider2D[] { exileButton.GetComponent<BoxCollider2D>() });
                // passiveButton.OnMouseOut = new UnityEvent();
                // passiveButton.OnMouseOver = new UnityEvent();
                passiveButton.OnClick = new Button.ButtonClickedEvent();

                passiveButton.OnClick.AddListener((Action) (() =>
                {
                    __result.ClearButtons();
                    Rpc<ForceExileRpc>.Instance.Send(playerInfo);
                }));
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.ForceExile)]
        public class ForceExileRpc : PlayerCustomRpc<SocksAreAmongUsPlugin, GameData.PlayerInfo>
        {
            public ForceExileRpc(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

            public override void Write(MessageWriter writer, GameData.PlayerInfo data)
            {
                writer.Write(data.PlayerId);
            }

            public override GameData.PlayerInfo Read(MessageReader reader)
            {
                return GameData.Instance.GetPlayerById(reader.ReadByte());
            }

            public override void Handle(PlayerControl innerNetObject, GameData.PlayerInfo data)
            {
                _exiled = data;
            }
        }
    }
}
