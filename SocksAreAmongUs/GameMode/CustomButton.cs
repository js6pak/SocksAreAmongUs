using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SocksAreAmongUs.GameMode
{
    public class CustomButton
    {
        public static List<CustomButton> Buttons { get; } = new List<CustomButton>();

        public MonoBehaviour ButtonManager { get; }
        public Func<bool> IsActive { get; }

        public CustomButton(MonoBehaviour buttonManager, Func<bool> isActive)
        {
            ButtonManager = buttonManager;
            IsActive = isActive;
        }

        private static int _buttonI;

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class StartPatch
        {
            public static void Prefix()
            {
                Buttons.Clear();
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive))]
        public static class SetHudActivePatch
        {
            public static void Postfix([HarmonyArgument(0)] bool isActive)
            {
                _buttonI = 0;

                foreach (var button in Buttons)
                {
                    var active = isActive && button.IsActive.Invoke();
                    button.ButtonManager.gameObject.SetActive(active);

                    if (active)
                    {
                        var aspectPosition = button.ButtonManager.GetComponent<AspectPosition>();

                        var vector3 = aspectPosition.DistanceFromEdge;
                        vector3.x = 0.7f + _buttonI % 3;
                        vector3.y = 0.7f + _buttonI / 3;
                        _buttonI++;
                        aspectPosition.DistanceFromEdge = vector3;
                        aspectPosition.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
                        aspectPosition.AdjustPosition();
                    }
                }
            }
        }
    }
}
