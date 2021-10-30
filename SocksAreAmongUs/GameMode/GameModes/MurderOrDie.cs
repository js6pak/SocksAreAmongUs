using System;
using BepInEx.Configuration;
using CodeIsNotAmongUs.Patches.RemovePlayerLimit;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class MurderOrDie : BaseGameMode<MurderOrDie.Component>
    {
        public override string Id => "murder_or_die";
        internal static bool Enabled => GameModeManager.CurrentGameMode is MurderOrDie;

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            public void FixedUpdate()
            {
                if (!Enabled)
                    return;

                if (Timer > 0 && AmongUsClient.Instance.IsGameStarted && PlayerControl.LocalPlayer.Data.IsImpostor && !PlayerControl.LocalPlayer.Data.IsDead && !RemovePlayerLimit.IsInCutscene && GameData.Instance && MeetingHud.Instance == null)
                {
                    Timer = Mathf.Clamp(Timer - Time.fixedDeltaTime, 0, MaxTimer.Value);

                    var color = Palette.ImpostorRed;
                    Text.Text = $"You have to kill someone in: [{color.ToHtmlStringRGBA()}]{Timer:F1}[FFFFFFFF]s";

                    if (Timer <= 0)
                    {
                        Rpc<RpcSetDead>.Instance.Send(true);
                    }
                }
            }

            public static float Timer;
            internal static ConfigEntry<float> MaxTimer;
        }

        public override void BindConfig(ConfigFile config)
        {
            Component.MaxTimer = config.Bind("MurderOrDie", "Max timer", 60f);
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
        public static class StartPatch
        {
            public static void Postfix()
            {
                Component.Timer = Component.MaxTimer.Value;
            }
        }

        public override void OnGameStart()
        {
            if (!Enabled)
                return;

            Component.Timer = Component.MaxTimer.Value;

            if (!PlayerControl.LocalPlayer.Data.IsImpostor || PlayerControl.LocalPlayer.Data.IsDead)
                return;

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
            Text.Text = "You have to kill someone in: ...";
            Text.scale = 0.8f;
        }

        public override void Cleanup()
        {
            if (Text)
            {
                Object.Destroy(Text.gameObject);
            }
        }

        private static TextRenderer Text { get; set; }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (!Enabled)
                    return;

                if (__instance.AmOwner)
                {
                    Component.Timer = Component.MaxTimer.Value;
                }
            }
        }
    }
}
