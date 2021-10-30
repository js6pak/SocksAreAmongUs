using System;
using System.Linq;
using BepInEx.Configuration;
using Reactor;
using Reactor.Extensions;
using Reactor.Unstrip;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class DetectiveRole : CustomRole
    {
        public override string Name => "Detective";
        public override Color? Color => Palette.Blue;
        public override RoleSide Side => RoleSide.Crewmate;

        public override bool ShouldColorName(PlayerControl player) => player.AmOwner;

        public override void OnSet(GameData.PlayerInfo playerInfo)
        {
            if (playerInfo.Object.AmOwner)
            {
                foreach (var playerControl in PlayerControl.AllPlayerControls)
                {
                    if (playerControl.AmOwner)
                    {
                        continue;
                    }

                    var footstepManager = playerControl.gameObject.AddComponent<FootstepManager>();
                    footstepManager.player = playerControl;
                }
            }
        }

        public override void BindConfig(ConfigFile config)
        {
            base.BindConfig(config);

            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((_, _) =>
            {
                ClearFootsteps();
            }));
        }

        public static void ClearFootsteps()
        {
            foreach (var footstep in Object.FindObjectsOfType<Footstep>().Concat<Component>(Object.FindObjectsOfType<FootstepManager>()))
            {
                Object.Destroy(footstep.gameObject);
            }
        }

        private static Sprite _asset;

        public override void LoadAssets(AssetBundle assetBundle)
        {
            _asset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/Footstep.png").DontUnload();
        }

        [RegisterInIl2Cpp]
        public class Footstep : MonoBehaviour
        {
            public Footstep(IntPtr ptr) : base(ptr)
            {
            }

            public SpriteRenderer spriteRenderer;
            public GameData.PlayerInfo Data { get; set; }

            public void Start()
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.material = new Material(Shader.Find("Sprites/Default"))
                {
                    color = Palette.PlayerColors[Data.ColorId]
                };
                spriteRenderer.transform.localScale = Vector3.one * 0.5f;

                spriteRenderer.sprite = _asset;
            }

            public void FixedUpdate()
            {
                var color = spriteRenderer.color;
                color.a -= 0.05f * Time.fixedDeltaTime;
                spriteRenderer.color = color;

                if (color.a <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }

        [RegisterInIl2Cpp]
        public class FootstepManager : MonoBehaviour
        {
            public FootstepManager(IntPtr ptr) : base(ptr)
            {
            }

            private const float Max = 0.2f;

            public PlayerControl player;
            public float timer;

            private void Start()
            {
                timer = Max;
            }

            private void FixedUpdate()
            {
                if (player.inVent || player.Data.IsDead)
                {
                    return;
                }

                timer -= Time.fixedDeltaTime;

                if (timer > 0)
                {
                    return;
                }

                timer = Max;

                var footstepObject = new GameObject();
                footstepObject.transform.position = player.transform.position;
                var footstep = footstepObject.AddComponent<Footstep>();
                footstep.Data = player.Data;
            }
        }
    }
}
