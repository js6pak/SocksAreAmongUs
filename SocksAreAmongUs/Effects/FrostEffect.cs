using System;
using System.Collections;
using Reactor;
using Reactor.Extensions;
using UnhollowerBaseLib.Attributes;
using UnityEngine;

namespace SocksAreAmongUs.Effects
{
    [RegisterInIl2Cpp]
    public class FrostEffect : MonoBehaviour
    {
        private static Shader _materialAsset;
        private static Texture2D _blendTexureAsset;
        private static Texture2D _bumpMapAsset;

        public static void LoadAssets(AssetBundle assetBundle)
        {
            _materialAsset = assetBundle.LoadAsset<Shader>("Assets/AssetBundle/Frost/ImageBlendEffect.shader").DontUnload();
            _blendTexureAsset = assetBundle.LoadAsset<Texture2D>("Assets/AssetBundle/Frost/Ice.tga").DontUnload();
            _bumpMapAsset = assetBundle.LoadAsset<Texture2D>("Assets/AssetBundle/Frost/Ice_N.tga").DontUnload();
        }

        public FrostEffect(IntPtr ptr) : base(ptr)
        {
        }

        // [Range(0, 1)]
        private float _frostAmount;

        private Material _material;

        private static readonly int _blendTexure = Shader.PropertyToID("blend_texure");
        private static readonly int _bumpMap = Shader.PropertyToID("bump_map");

        private void Awake()
        {
            _material = new Material(_materialAsset);
            _material.SetTexture(_blendTexure, _blendTexureAsset);
            _material.SetTexture(_bumpMap, _bumpMapAsset);
        }

        private static readonly int _blendAmount = Shader.PropertyToID("blend_amount");

        // TODO
        // private void OnRenderImage(RenderTexture source, RenderTexture destination)
        // {
        //     _material.SetFloat(_blendAmount, Mathf.Clamp01(_frostAmount));
        //
        //     Graphics.Blit(source, destination, _material);
        // }

        [HideFromIl2Cpp]
        public IEnumerator SetActive(bool active)
        {
            const float off = 0f;
            const float on = 0.3f;

            if (active)
            {
                _frostAmount = off;

                while (_frostAmount < on)
                {
                    yield return new WaitForEndOfFrame();
                    _frostAmount += Time.deltaTime / 5;
                }
            }
            else
            {
                _frostAmount = on;

                while (_frostAmount > off)
                {
                    yield return new WaitForEndOfFrame();
                    _frostAmount -= Time.deltaTime / 5;
                }
            }
        }
    }
}
