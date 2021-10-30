using System;
using Reactor;
using Reactor.Extensions;
using UnityEngine;

namespace SocksAreAmongUs.Effects
{
    [RegisterInIl2Cpp]
    public class GrayScaleEffect : MonoBehaviour
    {
        private static Shader _shaderAsset;

        public static void LoadAssets(AssetBundle assetBundle)
        {
            _shaderAsset = assetBundle.LoadAsset<Shader>("Assets/AssetBundle/GrayScale.shader").DontUnload();
        }

        public GrayScaleEffect(IntPtr ptr) : base(ptr)
        {
        }

        private Material _material;

        private void Awake()
        {
            _material = new Material(_shaderAsset);
        }

        // TODO
        // private void OnRenderImage(RenderTexture source, RenderTexture destination)
        // {
        //     Graphics.Blit(source, destination, _material);
        // }
    }
}
