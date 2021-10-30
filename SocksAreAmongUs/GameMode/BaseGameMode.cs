using BepInEx.Configuration;
using HarmonyLib;
using Il2CppSystem.IO;
using Reactor.Unstrip;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode
{
    public abstract class BaseGameMode
    {
        public abstract string Id { get; }
        public virtual Il2CppSystem.Type ComponentType => null;
        public string Name => GetType().Name;

        public virtual void BindConfig(ConfigFile config)
        {
        }

        public virtual void Initialize()
        {
        }

        public virtual void Serialize(BinaryWriter writer)
        {
        }

        public virtual void Deserialize(BinaryReader reader)
        {
        }

        public virtual void LoadAssets(AssetBundle assetBundle)
        {
        }

        public virtual void OnGameStart()
        {
        }

        public virtual void Cleanup()
        {
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetInfected))]
        private static class OnGameStartPatch
        {
            public static void Postfix()
            {
                GameModeManager.GameModes.Do(x => x.OnGameStart());
            }
        }
    }

    public abstract class BaseGameMode<TComponent> : BaseGameMode where TComponent : MonoBehaviour
    {
        public override void Initialize()
        {
            base.Initialize();

            ClassInjector.RegisterTypeInIl2Cpp<TComponent>();
        }

        public override Il2CppSystem.Type ComponentType => Il2CppType.Of<TComponent>();
    }
}
