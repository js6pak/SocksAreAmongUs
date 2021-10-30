using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class MindControl // : BaseGameMode
    {
        // public override string Id => "mind_control";
        // internal static bool Enabled => GameModeManager.CurrentGameMode is MindControl;

        public static Map<PlayerControl, PlayerControl> Controlled { get; } = new Map<PlayerControl, PlayerControl>(Il2CppEqualityComparer<PlayerControl>.Instance, Il2CppEqualityComparer<PlayerControl>.Instance);

        [RegisterCustomRpc((uint) CustomRpcCalls.MindControl)]
        public class RpcMindControl : PlayerCustomRpc<SocksAreAmongUsPlugin, PlayerControl>
        {
            public RpcMindControl(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

            public override void Write(MessageWriter writer, PlayerControl data)
            {
                MessageExtensions.WriteNetObject(writer, data);
            }

            public override PlayerControl Read(MessageReader reader)
            {
                return MessageExtensions.ReadNetObject<PlayerControl>(reader);
            }

            public override void Handle(PlayerControl innerNetObject, PlayerControl data)
            {
                if (Controlled.Forward.TryGetValue(innerNetObject, out var controlled))
                {
                    controlled.MyPhysics.ExitAllVents();
                    Controlled.Forward.Remove(innerNetObject);
                    Controlled.Reverse.Remove(controlled);
                }

                innerNetObject.MyPhysics.ExitAllVents();

                if (!innerNetObject.Equals(data))
                {
                    data.MyPhysics.ExitAllVents();
                    Controlled[innerNetObject] = data;
                }
            }
        }

        private static bool CanControl(PlayerControl playerControl)
        {
            return Controlled.Reverse.TryGetValue(playerControl, out var controller)
                ? controller.AmOwner
                : playerControl.AmOwner && !Controlled.Forward.Contains(playerControl);
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.GetTruePosition))]
        public static class GetTruePositionPatch
        {
            public static bool Prefix(PlayerControl __instance, ref Vector2 __result)
            {
                if (!__instance.AmOwner)
                    return true;

                if (!Controlled.Forward.TryGetValue(__instance, out var controlled))
                    return true;

                __result = controlled.GetTruePosition();

                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FindClosestTarget))]
        public static class KillPatch
        {
            public static bool Prefix(PlayerControl __instance, out PlayerControl __result)
            {
                __result = null;

                var num = GameOptionsData.KillDistances[PlayerControl.GameOptions.KillDistance];
                if (!ShipStatus.Instance)
                {
                    return false;
                }

                var truePosition = __instance.GetTruePosition();
                var allPlayers = GameData.Instance.AllPlayers;
                foreach (var playerInfo in allPlayers)
                {
                    if (!playerInfo.Disconnected && playerInfo.PlayerId != __instance.PlayerId && !playerInfo.IsDead && !playerInfo.IsImpostor)
                    {
                        var @object = playerInfo.Object;
                        if (@object && !CanControl(@object))
                        {
                            var vector = @object.GetTruePosition() - truePosition;
                            var magnitude = vector.magnitude;
                            if (magnitude <= num && !PhysicsHelpers.AnyNonTriggersBetween(truePosition, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
                            {
                                __result = @object;
                                num = magnitude;
                            }
                        }
                    }
                }

                return false;
            }
        }

        // [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
        // public static class CanUsePatch
        // {
        //     public static bool Prefix([HarmonyArgument(1)] ref bool canUse, [HarmonyArgument(2)] ref bool couldUse)
        //     {
        //         if (Controlled.Forward.Contains(PlayerControl.LocalPlayer))
        //         {
        //             canUse = couldUse = false;
        //             return false;
        //         }
        //
        //         return true;
        //     }
        // }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        public static class RpcMurderPlayerPatch
        {
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                if (Controlled.Forward.TryGetValue(__instance, out var controlled))
                {
                    controlled.RpcMurderPlayer(target);

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch]
        private static class HandleRpcPatch
        {
            private static List<Type> Types { get; } = typeof(IUsable).Assembly.GetTypes().Where(x => x != typeof(IUsable) && x.GetMethods().Any(m => m.Name == nameof(IUsable.CanUse))).ToList();

            public static IEnumerable<MethodBase> TargetMethods()
            {
                return Types.Select(x => x.GetMethod(nameof(IUsable.CanUse), AccessTools.all))
                    .Concat(Types.Select(x => x.GetMethod(nameof(IUsable.Use), AccessTools.all)));
            }

            public static void Prefix(out PlayerControl __state)
            {
                if (Controlled.Forward.TryGetValue(PlayerControl.LocalPlayer, out var controlled))
                {
                    __state = PlayerControl.LocalPlayer;
                    PlayerControl.LocalPlayer = controlled;
                }
                else
                {
                    __state = null;
                }
            }

            public static void Postfix(PlayerControl __state)
            {
                if (__state != null)
                {
                    PlayerControl.LocalPlayer = __state;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
        [HarmonyPriority(Priority.Low)]
        public static class PlayerPhysicsPatch
        {
            public static bool Prefix(PlayerPhysics __instance, bool __runOriginal)
            {
                if (!__runOriginal)
                {
                    return true;
                }

                var playerControl = __instance.myPlayer;
                var canControl = CanControl(playerControl);

                var data = playerControl.Data;
                var isDead = data != null && data.IsDead;
                __instance.HandleAnimation(isDead);
                if (canControl && playerControl.CanMove && GameData.Instance && (__instance.AmOwner || !isDead))
                {
                    __instance.body.velocity = DestroyableSingleton<HudManager>.Instance.joystick.Delta * (isDead ? __instance.TrueGhostSpeed : __instance.TrueSpeed);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
        [HarmonyPriority(Priority.Low)]
        public static class CustomNetworkTransformPatch
        {
            private static bool HasMoved(CustomNetworkTransform __instance)
            {
                float num;
                if (__instance.body != null)
                {
                    num = Vector2.Distance(__instance.body.position, __instance.prevPosSent);
                }
                else
                {
                    num = Vector2.Distance(__instance.transform.position, __instance.prevPosSent);
                }

                if (num > 0.0001f)
                {
                    return true;
                }

                if (__instance.body != null)
                {
                    num = Vector2.Distance(__instance.body.velocity, __instance.prevVelSent);
                }

                return num > 0.0001f;
            }

            public static bool Prefix(CustomNetworkTransform __instance, bool __runOriginal)
            {
                if (!__runOriginal)
                {
                    return true;
                }

                var playerControl = __instance.GetComponent<PlayerControl>();
                var canControl = CanControl(playerControl);

                if (canControl)
                {
                    if (HasMoved(__instance))
                    {
                        __instance.targetSyncPosition = __instance.prevPosSent = __instance.transform.position;
                        __instance.targetSyncVelocity = __instance.prevVelSent = Vector2.zero;
                        __instance.DirtyBits |= 3U;
                        return false;
                    }
                }
                else
                {
                    if (__instance.interpolateMovement != 0f)
                    {
                        var vector = __instance.targetSyncPosition - __instance.body.position;
                        if (vector.sqrMagnitude >= 0.0001f)
                        {
                            var num = __instance.interpolateMovement / __instance.sendInterval;
                            vector.x *= num;
                            vector.y *= num;
                            if (PlayerControl.LocalPlayer)
                            {
                                vector = Vector2.ClampMagnitude(vector, PlayerControl.LocalPlayer.MyPhysics.TrueSpeed);
                            }

                            __instance.body.velocity = vector;
                        }
                        else
                        {
                            __instance.body.velocity = Vector2.zero;
                        }
                    }

                    __instance.targetSyncPosition += __instance.targetSyncVelocity * Time.fixedDeltaTime * 0.1f;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.Deserialize))]
        [HarmonyPriority(Priority.Low)]
        public static class Patch
        {
            private static bool SidGreaterThan(ushort newSid, ushort prevSid)
            {
                var num = (ushort) (prevSid + 32767);
                if (prevSid < num)
                {
                    return newSid > prevSid && newSid <= num;
                }

                return newSid > prevSid || newSid <= num;
            }

            public static bool Prefix(CustomNetworkTransform __instance, [HarmonyArgument(0)] MessageReader reader, [HarmonyArgument(1)] bool initialState, bool __runOriginal)
            {
                if (!__runOriginal)
                {
                    return true;
                }

                var playerControl = __instance.GetComponent<PlayerControl>();
                var canControl = CanControl(playerControl);

                if (initialState)
                {
                    __instance.lastSequenceId = reader.ReadUInt16();
                    __instance.targetSyncPosition = __instance.transform.position = reader.ReadVector2();
                    __instance.targetSyncVelocity = reader.ReadVector2();
                    return false;
                }

                if (canControl)
                {
                    return false;
                }

                var newSid = reader.ReadUInt16();
                if (!SidGreaterThan(newSid, __instance.lastSequenceId))
                {
                    return false;
                }

                __instance.lastSequenceId = newSid;
                if (!__instance.isActiveAndEnabled)
                {
                    return false;
                }

                __instance.targetSyncPosition = reader.ReadVector2();
                __instance.targetSyncVelocity = reader.ReadVector2();
                if (Vector2.Distance(__instance.body.position, __instance.targetSyncPosition) > __instance.snapThreshold)
                {
                    if (__instance.body)
                    {
                        __instance.body.position = __instance.targetSyncPosition;
                        __instance.body.velocity = __instance.targetSyncVelocity;
                    }
                    else
                    {
                        __instance.transform.position = __instance.targetSyncPosition;
                    }
                }

                if (__instance.interpolateMovement == 0f && __instance.body)
                {
                    __instance.body.position = __instance.targetSyncPosition;
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FixedUpdate))]
        public static class InnerNetClientPatch
        {
            public static void Postfix(InnerNetClient __instance)
            {
                var obj = __instance.allObjects;
                lock (obj)
                {
                    foreach (var innerNetObject in __instance.allObjects)
                    {
                        if (!innerNetObject.TryCast<CustomNetworkTransform>())
                            continue;

                        var playerControl = innerNetObject.GetComponent<PlayerControl>();
                        if (!playerControl || playerControl.AmOwner)
                            continue;

                        var canControl = CanControl(playerControl);

                        if (innerNetObject.DirtyBits != 0U && canControl)
                        {
                            var messageWriter = __instance.Streams[(int) innerNetObject.sendMode];
                            messageWriter.StartMessage(1);
                            messageWriter.WritePacked(innerNetObject.NetId);
                            if (innerNetObject.Serialize(messageWriter, false))
                            {
                                messageWriter.EndMessage();
                            }
                            else
                            {
                                messageWriter.CancelMessage();
                            }
                        }
                    }
                }
            }
        }
    }
}
