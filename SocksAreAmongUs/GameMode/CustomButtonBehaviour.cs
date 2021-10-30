using System;
using System.Collections.Generic;
using Reactor;
using TMPro;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace SocksAreAmongUs.GameMode
{
    // TODO fix abstract in unhollower
    [RegisterInIl2Cpp]
    public class CustomButtonBehaviour : MonoBehaviour
    {
        public CustomButtonBehaviour(IntPtr ptr) : base(ptr)
        {
        }

        protected virtual void SetSprite()
        {
            renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = Sprite;
            renderer.transform.localScale = new Vector3(Scale, Scale, 1f);
            layers.Add(renderer);
        }

        public List<SpriteRenderer> layers = new List<SpriteRenderer>();

        protected SpriteRenderer AddLayer(Sprite sprite, Material material)
        {
            var holder = new GameObject(sprite.name);
            holder.transform.parent = transform;
            var layerRenderer = holder.AddComponent<SpriteRenderer>();
            layerRenderer.sprite = sprite;
            layerRenderer.material = material;
            layerRenderer.transform.localScale = new Vector3(Scale, Scale, 1f);

            holder.transform.localPosition = Vector3.zero;

            layers.Add(layerRenderer);
            return layerRenderer;
        }

        public void Start()
        {
            SetSprite();

            timerText = GetComponentInChildren<TextMeshPro>();

            var passiveButton = GetComponent<PassiveButton>();

            passiveButton.OnClick.RemoveAllListeners();
            passiveButton.OnClick.AddListener((Action) (() => OnClick()));

            timer = MaxTimer;
            CooldownHelpers.SetCooldownNormalizedUvs(renderer);
            IsActive = false;
            UpdateCoolDown();
        }

        [HideFromIl2Cpp]
        public virtual bool OnClick()
        {
            if (timer > 0 || !IsActive)
                return false;

            timer = MaxTimer;
            return true;
        }

        protected bool _isActive;

        [HideFromIl2Cpp]
        public bool IsActive
        {
            get => _isActive;

            set
            {
                _isActive = value;

                foreach (var layerRenderer in layers)
                {
                    layerRenderer.color = value ? Palette.EnabledColor : Palette.DisabledClear;
                    layerRenderer.material.SetFloat("_Desat", value ? 0f : 1f);
                    PlayerControl.SetPlayerMaterialColors(0, layerRenderer);
                }
            }
        }

        public void UpdateCoolDown()
        {
            var num = MaxTimer <= 0 ? 0 : Mathf.Clamp(timer / MaxTimer, 0f, 1f);
            if (renderer)
            {
                renderer.material.SetFloat("_Percent", num);
            }

            isCoolingDown = num > 0f;

            if (isCoolingDown)
            {
                timerText.text = Mathf.CeilToInt(timer).ToString();
                timerText.gameObject.SetActive(true);
                return;
            }

            timerText.gameObject.SetActive(false);
        }

        public void UpdateTimer()
        {
            var playerControl = PlayerControl.LocalPlayer;
            var data = playerControl.Data;

            if (timer > 0 && data.IsImpostor && playerControl.CanMove && !data.IsDead)
            {
                timer = Mathf.Clamp(timer - Time.fixedDeltaTime, 0, MaxTimer);
                UpdateCoolDown();
            }
        }

        public virtual void FixedUpdate()
        {
            UpdateTimer();
            IsActive = !isCoolingDown;
        }

        public SpriteRenderer renderer;
        public TextMeshPro timerText;

        public bool isCoolingDown = true;

        public float timer;

        [HideFromIl2Cpp]
        public virtual Sprite Sprite => throw new NotImplementedException();

        [HideFromIl2Cpp]
        public virtual float MaxTimer => 10;

        public virtual float Scale => 0.8f;
    }
}
