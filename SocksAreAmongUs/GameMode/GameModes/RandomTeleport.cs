using System;
using System.Collections.Generic;
using System.Linq;
using CodeIsNotAmongUs;
using Reactor;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class RandomTeleport : BaseGameMode<RandomTeleport.Component>
    {
        public override string Id => "random_teleport";
        internal static bool Enabled => GameModeManager.CurrentGameMode is RandomTeleport;

        public class Component : MonoBehaviour
        {
            public Component(IntPtr ptr) : base(ptr)
            {
            }

            private float _time;

            private static readonly List<Vector2> _positions = new List<Vector2>()
            {
                new Vector2(-2.5f, 1.1f), // cafeteria, left
                new Vector2(-9f, -4f), // medbay
                new Vector2(-16.5f, -1f), // upper engine
                new Vector2(-21f, -5.5f), // reactor
                new Vector2(-13.5f, -5f), // security
                new Vector2(-17f, -13f), // lower engine
                new Vector2(-7.5f, -8.5f), // electrical
                new Vector2(-2f, -15.5f), // storage bottom
                new Vector2(0f, -10f), // storage top
                new Vector2(6.2f, -8.5f), // admin
                new Vector2(4.5f, -15.5f), // comms
                new Vector2(9.2f, -12f), // shields
                new Vector2(17f, -4.5f), // navigation
                new Vector2(6.5f, -3.4f), // o2
                new Vector2(10f, 2.69f), // weapons
            };

            public void Update()
            {
                if (Enabled && AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.AmHost && GameData.Instance && MeetingHud.Instance == null)
                {
                    if (_time > 60)
                    {
                        var positions = _positions.ToList();

                        foreach (var playerControl in PlayerControl.AllPlayerControls)
                        {
                            if (positions.Count <= 0)
                            {
                                positions = _positions.ToList();
                            }

                            var position = positions[UnityEngine.Random.Range(0, positions.Count)];
                            positions.Remove(position);

                            if (playerControl.inVent)
                            {
                                playerControl.MyPhysics.RpcExitVent(0);
                            }

                            PluginSingleton<CodeIsNotAmongUsPlugin>.Instance.Log.LogDebug($"{playerControl.Data.PlayerName} - {position.ToString()}");
                            playerControl.NetTransform.RpcSnapTo(position);
                        }

                        _time = 0;
                    }

                    _time += Time.deltaTime;
                }
            }
        }
    }
}
