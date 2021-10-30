using Hazel;
using InnerNet;
using Reactor;
using Reactor.Networking;
using UnhollowerBaseLib;
using UnityEngine;
using Object = Il2CppSystem.Object;

namespace SocksAreAmongUs.GameMode
{
    [RegisterCustomRpc((uint) CustomRpcCalls.SetImpostor)]
    public class RpcSetImpostor : PlayerCustomRpc<SocksAreAmongUsPlugin, RpcSetImpostor.Data>
    {
        public RpcSetImpostor(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public class Data
        {
            public PlayerControl Target { get; }
            public bool IsImpostor { get; }

            public Data(PlayerControl target, bool isImpostor)
            {
                Target = target;
                IsImpostor = isImpostor;
            }
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

        public override void Write(MessageWriter writer, Data data)
        {
            MessageExtensions.WriteNetObject(writer, data.Target);
            writer.Write(data.IsImpostor);
        }

        public override Data Read(MessageReader reader)
        {
            var player = MessageExtensions.ReadNetObject<PlayerControl>(reader);
            var isImpostor = reader.ReadBoolean();

            return new Data(player, isImpostor);
        }

        public override void Handle(PlayerControl innerNetObject, Data data)
        {
            if (data.Target.AmOwner)
            {
                if (data.IsImpostor)
                {
                    var importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                    importantTextTask.transform.SetParent(PlayerControl.LocalPlayer.transform, false);
                    importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.ImpostorTask, new Il2CppReferenceArray<Object>(0))
                                             + "\r\n[FFFFFFFF]"
                                             + DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FakeTasks, new Il2CppReferenceArray<Object>(0));
                    data.Target.myTasks.Insert(0, importantTextTask);
                }
                else
                {
                    data.Target.RemoveInfected();
                }
            }

            data.Target.Data.IsImpostor = data.IsImpostor;

            if (data.Target.AmOwner)
            {
                HudManager.Instance.SetHudActive(!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled);
            }

            foreach (var otherPlayer in PlayerControl.AllPlayerControls)
            {
                otherPlayer.nameText.color = otherPlayer.Data.IsImpostor && PlayerControl.LocalPlayer.Data.IsImpostor ? Palette.ImpostorRed : Color.white;
            }
        }

        public static void Handle(PlayerControl target, bool isImpostor)
        {
            Rpc<RpcSetImpostor>.Instance.Handle(PlayerControl.LocalPlayer, new Data(target, isImpostor));
        }

        public static void Send(PlayerControl target, bool isImpostor)
        {
            Rpc<RpcSetImpostor>.Instance.Send(new Data(target, isImpostor));
        }
    }
}
