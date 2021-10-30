using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using Reactor.Unstrip;
using UnityEngine;

namespace SocksAreAmongUs
{
    [RegisterInIl2Cpp]
    public class CustomHats : MonoBehaviour
    {
        public static List<HatBehaviour> Hats { get; private set; }

        public static void Load(AssetBundle assetBundle)
        {
            Hats = new List<HatBehaviour>();

            foreach (var asset in assetBundle.LoadAllAssets().Select(x => x.TryCast<Sprite>()).Where(x => x).OrderBy(x => x.name))
            {
                if (asset.name.EndsWith(".hat"))
                {
                    asset.DontUnload();
                    Hats.Add(CreateHat(asset.name, asset));
                }
            }
        }

        private static HatBehaviour CreateHat(string id, Sprite sprite)
        {
            var newHat = ScriptableObject.CreateInstance<HatBehaviour>().DontDestroy();

            newHat.MainImage = sprite;
            newHat.ProductId = id;
            newHat.InFront = true;
            newHat.NoBounce = true;
            newHat.ChipOffset = new Vector2(0, 0.4f);

            return newHat;
        }

        public CustomHats(IntPtr ptr) : base(ptr)
        {
        }

        [HarmonyPatch(typeof(HatManager), nameof(HatManager.GetHatById))]
        public static class HatManagerPatch
        {
            public static void Prefix(HatManager __instance)
            {
                if (!__instance.GetComponent<CustomHats>())
                {
                    __instance.gameObject.AddComponent<CustomHats>();

                    Hats.Do(__instance.AllHats.Add);
                }
            }
        }

        // [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetTargetPlayerId))]
        // public static class PlayerVoteAreaPatch
        // {
        //     public static void Postfix(PlayerVoteArea __instance)
        //     {
        //         if (__instance.PlayerIcon)
        //         {
        //             var maskArea = __instance.transform.FindChild("votePlayerBase").FindChild("MaskArea");
        //             maskArea.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        //
        //             const float offset = 1.5f;
        //             maskArea.localScale += new Vector3(0, 0.2f * offset, 0);
        //             maskArea.localPosition += new Vector3(0, 0.1f * offset, 0);
        //
        //             __instance.PlayerIcon.HatSlot.BackLayer.material = __instance.PlayerIcon.HatSlot.FrontLayer.material = __instance.PlayerIcon.Body.material;
        //         }
        //     }
        // }
    }
}
