using System;
using System.Linq;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class RandomGameMode : BaseGameMode
    {
        public override string Id => "randon_game_mode";

        private static bool _enabled;

        internal static bool Enabled => GameModeManager.CurrentGameMode is RandomGameMode || _enabled;

        public override void Initialize()
        {
            base.Initialize();

            GameModes = GameModeManager.GameModes.Where(x => GameModeTypes.Contains(x.GetType())).ToArray();
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        public static class SetInfectedPatch
        {
            public static void Postfix()
            {
                _enabled = GameModeManager.CurrentGameMode is RandomGameMode;

                if (_enabled)
                {
                    SetGameMode(GameModes.Random());
                }
            }
        }

        private static Type[] GameModeTypes { get; } =
        {
            typeof(BodyDragging),
            typeof(Creeper),
            typeof(CrewmateFightsBack),
            typeof(FreeVent),
            typeof(Freezer),
            typeof(GhostMode),
            typeof(Invisibility),
            typeof(Morphing),
            typeof(MurderOrDie),
            typeof(RandomTeleport),
            typeof(Teleport),
            typeof(Witch)
        };

        private static BaseGameMode[] GameModes { get; set; }

        private static void SetGameMode(BaseGameMode gameMode)
        {
            Rpc<SetGameModeRpc>.Instance.Send(new SetGameModeRpc.Data(gameMode));
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        public static class MeetingHudPatch
        {
            public static void Postfix()
            {
                if (!Enabled)
                    return;

                if (AmongUsClient.Instance.AmHost)
                {
                    SetGameMode(GameModes.Random());
                }
            }
        }

        // private static int _i;
        //
        // [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        // public static class TestPatch
        // {
        //     public static void Postfix()
        //     {
        //         if (!Enabled)
        //             return;
        //
        //         if (Input.GetKeyDown(KeyCode.M))
        //         {
        //             SetGameMode(GameModes[_i++ % GameModes.Length]);
        //         }
        //     }
        // }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetGameMode)]
        public class SetGameModeRpc : PlayerCustomRpc<SocksAreAmongUsPlugin, SetGameModeRpc.Data>
        {
            public SetGameModeRpc(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public class Data
            {
                public BaseGameMode GameMode { get; set; }

                public Data(BaseGameMode gameMode)
                {
                    GameMode = gameMode;
                }
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

            public override void Write(MessageWriter writer, Data data)
            {
                writer.Write(data.GameMode?.Id ?? string.Empty);
                // data.GameMode?.Serialize(writer);
            }

            public override Data Read(MessageReader reader)
            {
                var id = reader.ReadString();
                var gameMode = GameModes.SingleOrDefault(x => x.Id == id);
                // gameMode?.Deserialize(reader);

                return new Data(gameMode);
            }

            public override void Handle(PlayerControl innerNetObject, Data data)
            {
                GameModeManager.CurrentGameMode?.Cleanup();
                GameModeManager.CurrentGameMode = data.GameMode;
                GameModeManager.CurrentGameMode.OnGameStart();
                PluginSingleton<SocksAreAmongUsPlugin>.Instance.Log.LogDebug(GameModeManager.CurrentGameMode);
                HudManager.Instance.SetHudActive(true);
            }
        }
    }
}
