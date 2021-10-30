using System;
using System.Linq;
using BepInEx.Configuration;
using CodeIsNotAmongUs.Patches.RemovePlayerLimit;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class HotPotato : BaseGameMode<HotPotato.Component>
    {
        public override string Id => "hot_potato";
        internal static bool Enabled => GameModeManager.CurrentGameMode is HotPotato && AmongUsClient.Instance.IsGameStarted;

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            public void FixedUpdate()
            {
                if (!Enabled || !Text)
                    return;

                if (!PlayerControl.LocalPlayer.Data.IsImpostor)
                {
                    Timer = MaxTimer.Value;
                    Text.Text = string.Empty;
                    return;
                }

                if (Timer > 0 && !PlayerControl.LocalPlayer.Data.IsDead && !RemovePlayerLimit.IsInCutscene && GameData.Instance)
                {
                    Timer = Mathf.Clamp(Timer - Time.fixedDeltaTime, 0, MaxTimer.Value);

                    var color = Palette.ImpostorRed;
                    Text.Text = $"You are going to explode in [{color.ToHtmlStringRGBA()}]{Timer:F1}[FFFFFFFF]s";

                    if (Timer <= 0)
                    {
                        Rpc<RpcSetDead>.Instance.Send(true);
                        RpcSetImpostor.Send(GameData.Instance.AllPlayers.ToArray().Where(x => !x.IsDead && !x.Disconnected).Random().Object, true);
                    }
                }
            }

            public static float Timer;
            internal static ConfigEntry<float> MaxTimer;
        }

        public override void BindConfig(ConfigFile config)
        {
            Component.MaxTimer = config.Bind("HotPotato", "Max timer", 30f);
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SelectInfected))]
        public static class SelectInfectedPatch
        {
            public static bool Prefix()
            {
                if (!Enabled)
                    return true;

                PlayerControl.LocalPlayer.RpcSetInfected(new Il2CppReferenceArray<GameData.PlayerInfo>(new[]
                {
                    GameData.Instance.AllPlayers.ToArray().Random()
                }));
                return false;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
        public static class CheckEndCriteriaPatch
        {
            public static bool Prefix(ShipStatus __instance)
            {
                if (!Enabled)
                    return true;

                var allPlayers = GameData.Instance.AllPlayers.ToArray();
                var dead = allPlayers.Count(x => x.IsDead || x.Disconnected);

                if (dead + 1 >= allPlayers.Count)
                {
                    var alive = allPlayers.SingleOrDefault(x => !x.IsDead && !x.Disconnected);

                    __instance.enabled = false;
                    ShipStatus.RpcEndGame(alive == null ? GameOverReason.ImpostorDisconnect : GameOverReason.ImpostorByKill, false);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
        public static class SetHudActivePatch
        {
            public static void Postfix(HudManager __instance)
            {
                if (!Enabled)
                    return;

                __instance.TaskStuff.SetActiveRecursively(false);
                __instance.TaskText.gameObject.SetActiveRecursively(false);
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckTaskCompletion))]
        public static class CheckTaskCompletionPatch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!Enabled)
                    return true;

                return __result = false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class RedNamePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled)
                    return;

                __instance.nameText.color = __instance.Data.IsImpostor ? Palette.Orange : Palette.White;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                Component.Timer = Component.MaxTimer.Value;

                var pingTracker = Object.FindObjectOfType<PingTracker>();
                if (!pingTracker)
                    return;

                var gameObject = Object.Instantiate(pingTracker.gameObject, pingTracker.transform.parent);

                Object.DestroyImmediate(gameObject.GetComponent<PingTracker>());

                var aspectPosition = gameObject.GetComponent<AspectPosition>();

                var vector3 = aspectPosition.DistanceFromEdge;
                vector3.x = 0.3f;
                vector3.y = 0.4f;
                aspectPosition.DistanceFromEdge = vector3;

                aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftBottom;

                aspectPosition.AdjustPosition();

                Text = gameObject.GetComponent<TextRenderer>();
                Text.Text = string.Empty;
                Text.scale = 0.8f;
            }
        }

        private static TextRenderer Text { get; set; }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                if (!Enabled)
                    return true;

                RpcSetImpostor.Handle(__instance, false);
                RpcSetImpostor.Handle(target, true);

                return false;
            }
        }
    }
}
