using System;
using HarmonyLib;
using Reactor;
using Reactor.Extensions;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace SocksAreAmongUs.GameMode.Roles
{
    public class Troller : BaseGameMode
    {
        public override string Id => "troller";
        internal static bool Enabled => GameModeManager.CurrentGameMode is Troller;

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
        public static class ButtonsPatch
        {
            public static void Postfix()
            {
                static bool IsActive()
                {
                    var data = PlayerControl.LocalPlayer.Data;

                    return Enabled && data.IsImpostor && !data.IsDead;
                }

                CustomButton.Buttons.Add(new CustomButton(Extensions.CreateButton<ButtonManager>(), IsActive));
            }
        }


        [RegisterInIl2Cpp]
        public class ButtonManager : CustomButtonBehaviour
        {
            public ButtonManager(IntPtr ptr) : base(ptr)
            {
                Menu = new TrollMenu();
            }

            [HideFromIl2Cpp]
            private TrollMenu Menu { get; }

            private void OnDestroy()
            {
                Menu?.Hide();
            }

            public override bool OnClick()
            {
                if (timer > 0 || !IsActive)
                    return false;

                Menu.Toggle();

                return true;
            }

            public override Sprite Sprite { get; } = null;
            public override float MaxTimer => 0.1f; //60;
        }

        public class TrollMenu : ListMenu
        {
            public override Vector2 Size { get; } = new Vector2(295, 300);
            public override Vector2 ButtonSize { get; } = new Vector2(250, 40);
            public override int Columns => 1;

            private Button AddButton(GameObject content, string a)
            {
                var uiButton = DefaultControls.CreateButton(GUIExtensions.StandardResources);

                var rectTransform = uiButton.GetComponent<RectTransform>();
                rectTransform.SetSize(ButtonSize.x, ButtonSize.y);

                var button = uiButton.GetComponent<Button>();

                // button.onClick.AddListener((Action) (() =>
                // {
                //     Canvas.Destroy();
                //     OnClick(player);
                // }));

                var text = uiButton.GetComponentInChildren<Text>();

                text.fontSize = 0;
                text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.anchoredPosition = new Vector2(5, 0);
                text.text = a;

                uiButton.transform.SetParent(content.transform, false);

                return button;
            }

            public override void Show()
            {
                base.Show();

                AddButton(Content, "Door magnet");
                AddButton(Content, "Admin swipe curse");
                AddButton(Content, "Who didn't do their tasks?");
                AddButton(Content, "Reversed movement");
                AddButton(Content, "Task teleport");
                AddButton(Content, "Random vote");
                AddButton(Content, "Vote quick!");
                AddButton(Content, "nou");
            }
        }
    }
}
