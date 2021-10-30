using System.Linq;
using Hazel;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode
{
    [RegisterCustomRpc((uint) CustomRpcCalls.SetDead)]
    public class RpcSetDead : PlayerCustomRpc<SocksAreAmongUsPlugin, bool>
    {
        public RpcSetDead(SocksAreAmongUsPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.Before;

        public override void Write(MessageWriter writer, bool data)
        {
            writer.Write(data);
        }

        public override bool Read(MessageReader reader)
        {
            return reader.ReadBoolean();
        }

        public override void Handle(PlayerControl target, bool value)
        {
            if (value)
            {
                var data = target.Data;
                if (data != null && !data.IsDead)
                {
                    target.gameObject.layer = LayerMask.NameToLayer("Ghost");
                    if (target.AmOwner)
                    {
                        if (Minigame.Instance)
                        {
                            Minigame.Instance.Close();
                            Minigame.Instance.Close();
                        }

                        DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
                        target.nameText.GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
                        target.RpcSetScanner(false);
                        var importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                        importantTextTask.transform.SetParent(target.transform, false);
                        if (!PlayerControl.GameOptions.GhostsDoTasks)
                        {
                            foreach (var playerTask in target.myTasks)
                            {
                                playerTask.OnRemove();
                                Object.Destroy(playerTask.gameObject);
                            }

                            target.myTasks.Clear();

                            importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostIgnoreTasks, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                        }
                        else
                        {
                            importantTextTask.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostDoTasks, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                        }

                        target.myTasks.Insert(0, importantTextTask);
                    }

                    target.Die(DeathReason.Kill);

                    var killAnimation = target.KillAnimations.Random();
                    var deadBody = Object.Instantiate(killAnimation.bodyPrefab);
                    var vector = target.transform.position + killAnimation.BodyOffset;
                    vector.z = vector.y / 1000f;
                    deadBody.transform.position = vector;
                    deadBody.ParentId = target.PlayerId;
                    target.SetPlayerMaterialColors(deadBody.GetComponent<Renderer>());
                }
            }
            else
            {
                target.Revive();

                if (target.AmOwner)
                {
                    HudManager.Instance.SetHudActive(true);

                    var task = target.myTasks.ToArray().Select(x => x.TryCast<ImportantTextTask>()).FirstOrDefault(x => x != null);

                    if (task)
                    {
                        task.OnRemove();
                        Object.Destroy(task.gameObject);
                        target.myTasks.Remove(task);
                    }

                    if (target.Data.IsImpostor)
                    {
                        var importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                        importantTextTask.transform.SetParent(PlayerControl.LocalPlayer.transform, false);
                        importantTextTask.Text =
                            TranslationController.Instance.GetString(StringNames.ImpostorTask, new Il2CppReferenceArray<Il2CppSystem.Object>(0))
                            + "\r\n[FFFFFFFF]" + TranslationController.Instance.GetString(StringNames.FakeTasks, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                        target.myTasks.Insert(0, importantTextTask);
                    }
                }
            }
        }
    }
}
