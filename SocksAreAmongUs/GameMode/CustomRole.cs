using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using SocksAreAmongUs.GameMode.Roles;
using SocksAreAmongUs.Patches;
using UnhollowerBaseLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SocksAreAmongUs.GameMode
{
    public enum RoleSide
    {
        Crewmate,
        Impostor,
        Solo
    }

    public abstract class CustomRole
    {
        public abstract string Name { get; }
        public virtual string Description => null;
        public abstract Color? Color { get; }
        public abstract RoleSide Side { get; }
        public abstract bool ShouldColorName(PlayerControl player);

        public virtual void OnSet(GameData.PlayerInfo playerInfo)
        {
        }

        public bool Test(GameData.PlayerInfo playerInfo)
        {
            return CustomRoles.Players.TryGetValue(playerInfo.PlayerId, out var customRole) && customRole == this;
        }

        public int Count { get; set; }
        public ForceRole.Role Force { get; set; }

        public virtual void BindConfig(ConfigFile config)
        {
            CustomOptions.Options.Add(new CustomNumberOption(CustomStringName.Register(Name + " count"), option =>
            {
                Count = option.Cast<NumberOption>().GetInt();
            }, () => (float) Count));
        }

        public virtual void LoadAssets(AssetBundle assetBundle)
        {
        }
    }

    public static class CustomRoles
    {
        public static Dictionary<byte, CustomRole> Players { get; } = new Dictionary<byte, CustomRole>();
        public static List<CustomRole> Roles { get; } = new List<CustomRole>();

        private static T Register<T>(T role) where T : CustomRole
        {
            Roles.Add(role);
            role.Force = (ForceRole.Role) (2 + Roles.Count);

            return role;
        }

        public static DetectiveRole Detective { get; } = Register(new DetectiveRole());
        public static JesterRole Jester { get; } = Register(new JesterRole());
        public static SheriffRole Sheriff { get; } = Register(new SheriffRole());
        public static SpectatorRole Spectator { get; } = Register(new SpectatorRole());
        public static DoctorRole Doctor { get; } = Register(new DoctorRole());
        public static MayorRole Mayor { get; } = Register(new MayorRole());
        public static TrollRole Troll { get; } = Register(new TrollRole());
        public static ThanosRole Thanos { get; } = Register(new ThanosRole());
        public static JanitorRole Janitor { get; } = Register(new JanitorRole());
        public static DictatorRole Dictator { get; } = Register(new DictatorRole());

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SelectInfected))]
        public static class SelectInfectedPatch
        {
            private static IEnumerable<GameData.PlayerInfo> SelectCustomRoles(List<GameData.PlayerInfo> list, RoleSide side)
            {
                foreach (var customRole in Roles.Where(role => role.Side == side))
                {
                    foreach (var playerInfo in GetSelected(list.Where(x => !x.IsImpostor && !Players.ContainsKey(x.PlayerId)), customRole.Count, customRole.Force))
                    {
                        Players.Add(playerInfo.PlayerId, customRole);
                        yield return playerInfo;
                    }
                }
            }

            private static List<GameData.PlayerInfo> GetSelected(IEnumerable<GameData.PlayerInfo> list, int count, ForceRole.Role force)
            {
                var selected = list.Where(force);

                var left = count - selected.Count;

                if (left > 0)
                {
                    selected.AddRange(list.OrderBy(_ => Random.value).Take(left));
                }

                return selected;
            }

            [HarmonyBefore("NewWizardMod")]
            public static bool Prefix(bool __runOriginal)
            {
                if (!__runOriginal)
                {
                    return false;
                }

                var list = GameData.Instance.AllPlayers.ToArray()
                    .Where(info => !info.Disconnected && !info.IsDead)
                    .Where(info => !ForceRole.Force.TryGetValue(info.PlayerName, out var role) || role != ForceRole.Role.Crewmate)
                    .ToList();

                Players.Clear();

                var impostorRoles = SelectCustomRoles(list, RoleSide.Impostor).ToList();
                impostorRoles.Do(info => list.Remove(info));

                PlayerControl.LocalPlayer.RpcSetInfected(new Il2CppReferenceArray<GameData.PlayerInfo>(
                    impostorRoles
                        .Concat(GetSelected(list.Where(x => !ForceRole.Force.TryGetValue(x.PlayerName, out var role) || role == ForceRole.Role.Impostor || role == ForceRole.Role.Random), PlayerControl.GameOptions.NumImpostors - impostorRoles.Count, ForceRole.Role.Impostor))
                        .ToArray())
                );

                SelectCustomRoles(list, RoleSide.Solo).Do(info => list.Remove(info));
                SelectCustomRoles(list, RoleSide.Crewmate).Do(info => list.Remove(info));

                return false;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
        public static class SetCustomRolePatch
        {
            public static void Postfix()
            {
                Rpc<SetCustomRoleRpc>.Instance.Send(new SetCustomRoleRpc.Data(Players.ToDictionary(k => k.Key, v => (byte) Roles.IndexOf(v.Value))));
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        public static class NamePatch
        {
            public static void Postfix(PlayerControl __instance)
            {
                if (Players.TryGetValue(__instance.PlayerId, out var customRole) && customRole.Color.HasValue && customRole.ShouldColorName(__instance))
                {
                    __instance.nameText.color = customRole.Color!.Value;
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public static class CreateButtonPatch
        {
            public static void Postfix(MeetingHud __instance)
            {
                foreach (var playerVoteArea in __instance.playerStates)
                {
                    var playerInfo = GameData.Instance.GetPlayerById((byte) playerVoteArea.TargetPlayerId);

                    if (playerInfo != null && Players.TryGetValue(playerInfo.PlayerId, out var customRole) && customRole.Color.HasValue && customRole.ShouldColorName(playerInfo.Object))
                    {
                        playerVoteArea.NameText.color = customRole.Color!.Value;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
        public static class ExileControllerPatch
        {
            public static void Postfix(ExileController __instance, [HarmonyArgument(0)] GameData.PlayerInfo exiled)
            {
                if (exiled != null && Players.TryGetValue(exiled.PlayerId, out var customRole))
                {
                    __instance.completeString = exiled.PlayerName + " was The " + customRole.Name;
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene.Nested_0), nameof(IntroCutscene.Nested_0.MoveNext))]
        public static class CutscenePatch
        {
            private static readonly int _color = Shader.PropertyToID("_Color");

            public static void Postfix(IntroCutscene.Nested_0 __instance)
            {
                var player = PlayerControl.LocalPlayer;

                if (Players.TryGetValue(player.PlayerId, out var customRole))
                {
                    var introCutscene = __instance.__this;

                    if (customRole.Color.HasValue)
                    {
                        introCutscene.BackgroundBar.material.SetColor(_color, customRole.Color.Value);
                        introCutscene.Title.text = $"[{customRole.Color.Value.ToHtmlStringRGBA()}]{customRole.Name}";
                        introCutscene.Title.color = customRole.Color.Value;
                    }
                    else
                    {
                        introCutscene.Title.text = customRole.Name;
                    }

                    if (customRole.Description != null)
                    {
                        introCutscene.ImpostorText.gameObject.SetActive(true);
                        introCutscene.ImpostorText.text = customRole.Description;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
        public static class MurderPlayerPatch
        {
            public static void Postfix([HarmonyArgument(0)] PlayerControl target)
            {
                if (target && target.AmOwner && target.Data.IsDead)
                {
                    HudManager.Instance.SetHudActive(false);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetInfected))]
        public static class RpcSetInfectedPatch
        {
            public static void Postfix()
            {
                if (TutorialManager.InstanceExists)
                {
                    HudManager.Instance.SetHudActive(true);
                }
            }
        }

        [RegisterCustomRpc((uint) CustomRpcCalls.SetCustomRole)]
        public class SetCustomRoleRpc : PlayerCustomRpc<SocksAreAmongUsPlugin, SetCustomRoleRpc.Data>
        {
            public SetCustomRoleRpc(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
            {
            }

            public readonly struct Data
            {
                public Dictionary<byte, byte> Roles { get; }

                public Data(Dictionary<byte, byte> roles)
                {
                    Roles = roles;
                }
            }

            public override RpcLocalHandling LocalHandling => RpcLocalHandling.After;

            public override void Write(MessageWriter writer, Data data)
            {
                writer.WritePacked(data.Roles.Count);

                foreach (var (playerId, roleId) in data.Roles)
                {
                    writer.Write(playerId);
                    writer.Write(roleId);
                }
            }

            public override Data Read(MessageReader reader)
            {
                var length = reader.ReadPackedInt32();
                var roles = new Dictionary<byte, byte>(length);

                for (var i = 0; i < length; i++)
                {
                    var playerId = reader.ReadByte();
                    var customRoleId = reader.ReadByte();

                    roles[playerId] = customRoleId;
                }

                return new Data(roles);
            }

            public override void Handle(PlayerControl innerNetObject, Data data)
            {
                Players.Clear();

                foreach (var (playerId, roleId) in data.Roles)
                {
                    var playerInfo = GameData.Instance.GetPlayerById(playerId);
                    var customRole = Roles.ElementAt(roleId);

                    Players[playerInfo.PlayerId] = customRole;
                    customRole.OnSet(playerInfo);
                }
            }
        }
    }
}
