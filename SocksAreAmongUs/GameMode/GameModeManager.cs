using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeIsNotAmongUs;
using HarmonyLib;
using Il2CppSystem.IO;
using Reactor;
using Reactor.Extensions;

namespace SocksAreAmongUs.GameMode
{
    public static class GameModeManager
    {
        private static readonly List<BaseGameMode> _gameModes = new List<BaseGameMode>();

        public static IReadOnlyList<BaseGameMode> GameModes => _gameModes.AsReadOnly();

        public static void Register(BaseGameMode gameMode)
        {
            if (_gameModes.Any(x => x.Id == gameMode.Id))
            {
                throw new ArgumentException("A game mode with the same id has already been added.");
            }

            _gameModes.Add(gameMode);
        }

        private static BaseGameMode _currentGameMode;

        public static BaseGameMode CurrentGameMode
        {
            get => _currentGameMode;
            set
            {
                _currentGameMode = value;

                if (PlayerControl.LocalPlayer && PlayerControl.GameOptions != null && AmongUsClient.Instance && AmongUsClient.Instance.AmHost)
                {
                    PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);
                }
            }
        }

        // [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Serialize))]
        [HarmonyPatch]
        public static class SerializePatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(GameOptionsData).GetMethods(AccessTools.all).Where(x => x.ReturnType == typeof(void) && x.GetParameters().Any() && x.GetParameters().ElementAt(0).ParameterType == typeof(BinaryWriter));
            }

            public static void Postfix([HarmonyArgument(0)] BinaryWriter writer, [HarmonyArgument(1)] byte version)
            {
                if (version == 4)
                {
                    writer.Write(CurrentGameMode?.Id ?? string.Empty);
                    CurrentGameMode?.Serialize(writer);
                }
            }
        }

        // [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.Deserialize), typeof(BinaryReader))]
        [HarmonyPatch]
        public static class DeserializePatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(GameOptionsData).GetMethods(AccessTools.all).Where(x => x.ReturnType == typeof(GameOptionsData) && x.GetParameters().Any() && x.GetParameters().ElementAt(0).ParameterType == typeof(BinaryReader));
            }

            public static void Postfix([HarmonyArgument(0)] BinaryReader reader, ref GameOptionsData __result)
            {
                // var fromNetwork = new Il2CppSystem.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name == nameof(PlayerControl.HandleRpc);

                if (__result != null)
                {
                    if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    {
                        return;
                    }

                    var id = reader.ReadString();
                    CurrentGameMode = GameModes.SingleOrDefault(x => x.Id == id);
                    CurrentGameMode?.Deserialize(reader);
                    PluginSingleton<CodeIsNotAmongUsPlugin>.Instance.Log.LogInfo($"Set current game mode to {CurrentGameMode}");

                    var menu = UnityEngine.Object.FindObjectOfType<GameOptionsMenu>();

                    if (menu)
                    {
                        menu.cachedData = null;
                        menu.Update();
                    }
                }
            }
        }
    }
}
