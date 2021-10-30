using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode.GameModes
{
    public class FreeVent : BaseGameMode
    {
        public override string Id => "free_vent";
        internal static bool Enabled => GameModeManager.CurrentGameMode is FreeVent;

        [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
        public static class CanUsePatch
        {
            public static bool Prefix(Vent __instance, [HarmonyArgument(0)] GameData.PlayerInfo playerInfo, [HarmonyArgument(1)] ref bool canUse, [HarmonyArgument(2)] ref bool couldUse)
            {
                if (Enabled && Vector2.Distance(playerInfo.Object.GetTruePosition(), __instance.transform.position) <= __instance.UsableDistance)
                {
                    canUse = couldUse = true;

                    return false;
                }

                return true;
            }
        }

        // [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
        // public static class FixedUpdatePatch
        // {
        //     private static readonly Il2CppReferenceArray<Collider2D> _hitBuffer = new Il2CppReferenceArray<Collider2D>(200);
        //     private static Vent _last;
        //
        //     public static void Postfix(PlayerControl __instance)
        //     {
        //         if (!Enabled || !__instance.AmOwner)
        //             return;
        //
        //         var data = __instance.Data;
        //         if (data == null || data.IsImpostor || data.IsDead)
        //         {
        //             return;
        //         }
        //
        //         if (__instance.CanMove || __instance.inVent)
        //         {
        //             var truePosition = __instance.GetTruePosition();
        //             Vent vent = null;
        //             var distance = float.MaxValue;
        //
        //             var hitCount = Physics2D.OverlapCircleNonAlloc(truePosition, __instance.MaxReportDistance, _hitBuffer, Constants.Usables);
        //             for (var i = 0; i < hitCount; i++)
        //             {
        //                 var collider2D = _hitBuffer[i];
        //                 var vent2 = collider2D.GetComponent<Vent>();
        //
        //                 if (vent2 != null)
        //                 {
        //                     var distance2 = Vector2.Distance(truePosition, vent2.transform.position);
        //
        //                     if (distance2 < distance && distance2 <= vent2.UsableDistance)
        //                     {
        //                         distance = distance2;
        //                         vent = vent2;
        //                     }
        //                 }
        //             }
        //
        //             if (vent != null)
        //             {
        //                 _last = vent;
        //                 vent.SetOutline(true, true);
        //
        //                 __instance.Field_26 = vent.Cast<IUsable>();
        //                 HudManager.Instance.UseButton.SetTarget(vent.Cast<IUsable>());
        //                 return;
        //             }
        //
        //             if (_last)
        //             {
        //                 _last.SetOutline(false, false);
        //                 _last = null;
        //             }
        //         }
        //     }
        //
        //     [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.UseClosest))]
        //     public static class UseClosestPatch
        //     {
        //         public static bool Prefix(PlayerControl __instance)
        //         {
        //             if (!Enabled)
        //                 return true;
        //
        //             var vent = __instance.Field_26?.TryCast<Vent>();
        //             if (vent)
        //             {
        //                 if (__instance.inVent)
        //                 {
        //                     __instance.MyPhysics.RpcExitVent(vent.Id);
        //                     vent.SetButtons(false);
        //                 }
        //                 else
        //                 {
        //                     __instance.MyPhysics.RpcEnterVent(vent.Id);
        //                     vent.SetButtons(true);
        //                 }
        //
        //                 return false;
        //             }
        //
        //             return true;
        //         }
        //     }
    }
}
