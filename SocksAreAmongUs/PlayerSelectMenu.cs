using System;
using System.Collections.Generic;
using Reactor.Extensions;
using Reactor.Unstrip;
using SocksAreAmongUs.GameMode.GameModes;
using UnityEngine;
using UnityEngine.UI;

namespace SocksAreAmongUs
{
    public abstract class PlayerSelectMenu : ListMenu
    {
        private static readonly int _backColor = Shader.PropertyToID("_BackColor");
        private static readonly int _bodyColor = Shader.PropertyToID("_BodyColor");
        private static readonly int _visorColor = Shader.PropertyToID("_VisorColor");

        public static Shader PlayerShaderAsset;
        private static Sprite _playerIconAsset;

        internal static void LoadAssets(AssetBundle assetBundle)
        {
            PlayerShaderAsset = assetBundle.LoadAsset<Shader>("Assets/AssetBundle/PlayerShader.shader").DontUnload();
            _playerIconAsset = assetBundle.LoadAsset<Sprite>("Assets/AssetBundle/playerVoteResults.png").DontUnload();
        }

        protected virtual void UpdateButton(PlayerControl player, Button button, Text text, Image image)
        {
            var character = Morphing.Characters.GetValueOrDefault(player.PlayerId) ?? new Morphing.Character(player.Data);

            text.text = character.PlayerName;

            if (player.Data.IsDead)
            {
                text.color = Palette.DisabledGrey;
            }
            else if (player.Data.IsImpostor)
            {
                text.color = Palette.ImpostorRed;
            }
            else
            {
                text.color = Palette.Black;
            }

            var colorId = character.ColorId;
            image.material = new Material(PlayerShaderAsset);
            image.material.SetColor(_backColor, Palette.ShadowColors[colorId]);
            image.material.SetColor(_bodyColor, Palette.PlayerColors[colorId]);
            image.material.SetColor(_visorColor, Palette.VisorColor);
        }

        protected virtual bool ArePlayersEqual(PlayerControl a, PlayerControl b)
        {
            return a.Equals(b);
        }

        protected virtual Button AddButton(GameObject content, PlayerControl player)
        {
            var uiButton = DefaultControls.CreateButton(GUIExtensions.StandardResources);

            PlayerEvents.PlayerLeft += left =>
            {
                if (uiButton && player.Equals(left))
                {
                    uiButton.Destroy();
                }
            };

            var rectTransform = uiButton.GetComponent<RectTransform>();
            rectTransform.SetSize(130, 30);

            var button = uiButton.GetComponent<Button>();

            button.onClick.AddListener((Action) (() =>
            {
                Canvas.Destroy();
                OnClick(player);
            }));

            if (ArePlayersEqual(PlayerControl.LocalPlayer, player))
            {
                button.interactable = false;
            }

            var text = uiButton.GetComponentInChildren<Text>();

            text.fontSize = 0;
            text.alignment = TextAnchor.MiddleLeft;
            text.rectTransform.anchoredPosition = new Vector2(45, 0);

            var image = DefaultControls.CreateImage(GUIExtensions.StandardResources).GetComponent<Image>();
            image.gameObject.transform.SetParent(uiButton.transform, false);

            image.rectTransform.anchorMax = image.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.SetSize(30, 25);
            image.rectTransform.anchoredPosition = new Vector2(25, 0);
            image.sprite = _playerIconAsset;
            image.maskable = true;

            UpdateButton(player, button, text, image);

            PlayerEvents.PlayerInfoUpdated += updated =>
            {
                if (uiButton && player.Equals(updated))
                {
                    UpdateButton(player, button, text, image);
                }
            };

            uiButton.transform.SetParent(content.transform, false);

            return button;
        }

        public override void Show()
        {
            base.Show();

            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player.Data == null || player.Data.Disconnected)
                    continue;

                AddButton(Content, player);
            }

            PlayerEvents.PlayerJoined += player => AddButton(Content, player);

            // scrollView.verticalScrollbar.value = 0;
        }

        public abstract void OnClick(PlayerControl playerControl);
    }
}
