using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.IL2CPP;
using CodeIsNotAmongUs;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using Reactor.Patches;
using Reactor.Unstrip;
using SocksAreAmongUs.Effects;
using SocksAreAmongUs.GameMode;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SocksAreAmongUs
{
    [BepInPlugin(Id)]
    [BepInProcess("Among Us.exe")]
    [BepInDependency(ReactorPlugin.Id)]
    [BepInDependency(CodeIsNotAmongUsPlugin.Id)]
    public class SocksAreAmongUsPlugin : BasePlugin
    {
        public const string Id = "pl.js6pak.SocksAreAmongUs";

        public Harmony Harmony { get; } = new Harmony(Id);
        public ForceRole ForceRole { get; private set; }
        public BaseGameMode[] GameModes { get; private set; }

        public SocksAreAmongUsPlugin()
        {
            PluginSingleton<SocksAreAmongUsPlugin>.Instance = this;
        }

        public override void Load()
        {
            RegisterInIl2CppAttribute.Register();
            RegisterCustomRpcAttribute.Register(this);

            GameOptionsData.KillDistanceStrings = GameOptionsData.KillDistanceStrings.AddItem(TranslationController.Instance.GetString((StringNames) CustomStringNames.Infinite, new Il2CppReferenceArray<Il2CppSystem.Object>(0))).ToArray();
            GameOptionsData.KillDistances = GameOptionsData.KillDistances.AddItem(20f).ToArray();

            var types = AccessTools.GetTypesFromAssembly(GetType().Assembly).Where(x => !x.IsAbstract && typeof(BaseGameMode).IsAssignableFrom(x));
            GameModes = types.Select(Activator.CreateInstance).Cast<BaseGameMode>().ToArray();

            GameModes.Do(GameModeManager.Register);
            GameModes.Do(x => x.BindConfig(Config));
            GameModes.Do(x => x.Initialize());

            CustomRoles.Roles.Do(role => role.BindConfig(Config));

            SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>) ((_, _) =>
            {
                CustomRoles.Players.Clear();
            }));

            var gameObject = new GameObject(nameof(SocksAreAmongUsPlugin)).DontDestroy();
            ForceRole = gameObject.AddComponent<ForceRole>();

            GameModes.Where(x => x.ComponentType != null).Do(x => gameObject.AddComponent(x.ComponentType));

            Harmony.PatchAll();

            var assetBundle = AssetBundle.LoadFromMemory(Assembly.GetExecutingAssembly().GetManifestResourceStream("SocksAreAmongUs.Assets.socksareamongus.bundle").ReadFully());

            FrostEffect.LoadAssets(assetBundle);
            PlayerSelectMenu.LoadAssets(assetBundle);
            GrayScaleEffect.LoadAssets(assetBundle);
            CustomHats.Load(assetBundle);

            foreach (var gameMode in GameModes)
            {
                gameMode.LoadAssets(assetBundle);
            }

            foreach (var customRole in CustomRoles.Roles)
            {
                customRole.LoadAssets(assetBundle);
            }

            assetBundle.Unload(false);

            ReactorVersionShower.TextUpdated += text =>
            {
                text.text += "\nSocksAreAmongUs: " + MetadataHelper.GetMetadata(this).Version;
            };
        }

        // [HarmonyPatch(typeof(GameOptionsData), nameof(GameOptionsData.))]
        [HarmonyPatch]
        public static class ToHudStringPatch
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(GameOptionsData).GetMethods(typeof(string), typeof(int));
            }

            public static void Postfix(ref string __result)
            {
                if (__result != null)
                {
                    __result += $"Game mode: {GameModeManager.CurrentGameMode?.Name ?? "None"}";

                    var roles = CustomRoles.Roles.Where(x => x.Count > 0).ToArray();

                    if (roles.Any())
                    {
                        __result += " with " + roles.Select(role => role.Count switch
                        {
                            1 => $"a {role.Name}",
                            > 1 => $"{role.Count} {role.Name}s",
                            _ => throw new ArgumentOutOfRangeException()
                        }).Join();
                    }

                    __result += "\n";
                }
            }
        }
    }
}
