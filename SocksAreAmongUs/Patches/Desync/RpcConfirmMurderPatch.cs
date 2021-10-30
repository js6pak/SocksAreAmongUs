using HarmonyLib;
using Hazel;
using InnerNet;

namespace SocksAreAmongUs.Patches.Desync
{
    public static class RpcConfirmMurderPatch
    {
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        public static class RpcMurderPlayerPatch
        {
            public static bool Prefix(bool __runOriginal, PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
            {
                if (!__runOriginal)
                {
                    return false;
                }

                if (TutorialManager.InstanceExists)
                {
                    return true;
                }

                var writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, 120, SendOption.Reliable, -1);
                MessageExtensions.WriteNetObject(writer, target);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                return false;
            }
        }
    }
}
