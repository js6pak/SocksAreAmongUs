using System;
using System.Linq;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class Rotating : BaseGameMode<Rotating.Component>
    {
        public override string Id => "rotating";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Rotating;

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            private float _time;

            public void Update()
            {
                if (Enabled && AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.AmHost && GameData.Instance && MeetingHud.Instance == null)
                {
                    if (_time > 60)
                    {
                        var players = PlayerControl.AllPlayerControls.ToArray().Where(x => !x.Data.IsImpostor && !x.Data.Disconnected && !x.Data.IsDead).ToArray();
                        var impostors = PlayerControl.AllPlayerControls.ToArray().Where(x => x.Data.IsImpostor).ToArray();
                        var newImpostor = players.ElementAtOrDefault(UnityEngine.Random.Range(0, players.Length));

                        if (newImpostor != null)
                        {
                            RpcSetImpostor.Send(newImpostor, true);
                        }

                        foreach (var player in impostors)
                        {
                            if (player.inVent)
                            {
                                player.MyPhysics.RpcExitVent(0);
                            }

                            RpcSetImpostor.Send(player, false);
                        }

                        _time = 0;
                    }

                    _time += Time.deltaTime;
                }
            }
        }
    }
}
