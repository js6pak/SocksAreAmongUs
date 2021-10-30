using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class RandomMap : BaseGameMode
    {
        public override string Id => "randon_map";

        internal static bool Enabled => GameModeManager.CurrentGameMode is RandomMap;

        public static void ChangeMap(ShipStatus.MapType mapType)
        {
            throw new NotImplementedException();
            // DetectiveRole.ClearFootsteps();
            // ShipStatus.Instance.Despawn();
            // Object.Destroy(MapBehaviour.Instance);
            //
            // PlayerControl.LocalPlayer.hitBuffer = new Il2CppReferenceArray<Collider2D>(200);
            // PlayerControl.LocalPlayer.cache.Clear();
            // PlayerControl.LocalPlayer.itemsInRange.Clear();
            // PlayerControl.LocalPlayer.newItemsInRange.Clear();
            //
            // GameData.Instance.TotalTasks = GameData.Instance.CompletedTasks = 0;
            // ShipStatus.Instance = Object.Instantiate(AmongUsClient.Instance.ShipPrefabs[(byte) mapType]);
            //
            // var tasks = new Dictionary<byte, int>();
            //
            // foreach (var playerControl in PlayerControl.AllPlayerControls)
            // {
            //     tasks[playerControl.PlayerId] = playerControl.myTasks.ToArray().Count(x => x.IsComplete);
            // }
            //
            // HudManager.Instance.ShadowQuad.material.SetInt("_Mask", 3);
            // ShipStatus.Instance.Begin();
            //
            // foreach (var playerControl in PlayerControl.AllPlayerControls)
            // {
            //     playerControl.MyPhysics.ExitAllVents();
            //     playerControl.NetTransform.SnapTo(ShipStatus.Instance.GetSpawnLocation(playerControl.PlayerId, GameData.Instance.PlayerCount, false));
            // }
            //
            // Coroutines.Start(SetTasks(tasks));
        }

        private static IEnumerator SetTasks(Dictionary<byte, int> tasks)
        {
            while (!ShipStatus.Instance || GameData.Instance.TotalTasks <= 0)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1f); // bruh

            foreach (var playerControl in PlayerControl.AllPlayerControls)
            {
                var completed = tasks[playerControl.PlayerId];

                foreach (var task in playerControl.myTasks)
                {
                    if (!task.TryCast<NormalPlayerTask>())
                        continue;

                    if (completed > 0)
                    {
                        GameData.Instance.CompleteTask(playerControl, task.Id);
                        task.Complete();
                    }

                    completed--;
                }
            }

            GameData.Instance.RecomputeTaskCounts();
        }

        public static void ChangeMapToRandom()
        {
            var mapTypes = (ShipStatus.MapType[]) Enum.GetValues(typeof(ShipStatus.MapType));
            var currentMapType = ShipStatus.Instance.Type;
            var mapType = mapTypes.Where(x => x != currentMapType).Random();

            Rpc<RpcSetMap>.Instance.Send(mapType, true);
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
        public static class MeetingHudPatch
        {
            public static void Postfix()
            {
                if (!Enabled || !AmongUsClient.Instance.AmHost)
                    return;

                ChangeMapToRandom();
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetMap)]
        public class RpcSetMap : PlayerCustomRpc<SocksAreAmongUsPlugin, ShipStatus.MapType>
        {
            public RpcSetMap(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

            public override void Write(MessageWriter writer, ShipStatus.MapType data)
            {
                writer.Write((byte) data);
            }

            public override ShipStatus.MapType Read(MessageReader reader)
            {
                return (ShipStatus.MapType) reader.ReadByte();
            }

            public override void Handle(PlayerControl innerNetObject, ShipStatus.MapType data)
            {
                ChangeMap(data);
            }
        }
    }
}
