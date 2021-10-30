using System.Collections.Generic;
using System.Linq;
using Reactor.Extensions;
using SocksAreAmongUs.GameMode;
using UnityEngine;

namespace SocksAreAmongUs
{
    public static class Extensions
    {
        public static List<GameData.PlayerInfo> Where(this IEnumerable<GameData.PlayerInfo> players, ForceRole.Role role)
        {
            return players
                .Where(info => info?.PlayerName != null)
                .Where(info => ForceRole.Force.TryGetValue(info.PlayerName, out var x) && x == role)
                .ToList();
        }

        public static T CreateButton<T>() where T : CustomButtonBehaviour
        {
            var hudManager = HudManager.Instance;
            var gameObject = Object.Instantiate(hudManager.KillButton.gameObject, hudManager.transform.parent);
            gameObject.GetComponent<KillButtonManager>().DestroyImmediate();

            return gameObject.AddComponent<T>();
        }

        public static DeadBody FindClosestBody(this PlayerControl playerControl, float? radius = null)
        {
            var position = PlayerControl.LocalPlayer.GetTruePosition();
            var max = radius ?? playerControl.MaxReportDistance;
            DeadBody result = null;

            foreach (var collider2D in Physics2D.OverlapCircleAll(position, max, Constants.NotShipMask))
            {
                if (collider2D.CompareTag("DeadBody"))
                {
                    var distance = Vector3.Distance(position, collider2D.transform.position);
                    if (distance > max)
                        continue;

                    var component = collider2D.GetComponent<DeadBody>();
                    if (component)
                    {
                        max = distance;
                        result = component;
                    }
                }
            }

            return result;
        }

        private static readonly int _backColor = Shader.PropertyToID("_BackColor");
        private static readonly int _bodyColor = Shader.PropertyToID("_BodyColor");
        private static readonly int _visorColor = Shader.PropertyToID("_VisorColor");

        public static void SetPlayerMaterialColors(Material source, Material destination)
        {
            destination.SetColor(_backColor, source.GetColor(_backColor));
            destination.SetColor(_bodyColor, source.GetColor(_bodyColor));
            destination.SetColor(_visorColor, source.GetColor(_visorColor));
        }
    }
}
