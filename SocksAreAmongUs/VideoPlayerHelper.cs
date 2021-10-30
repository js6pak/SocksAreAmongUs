using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SocksAreAmongUs
{
    public static class VideoPlayerHelper
    {
        private static void SetFade(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
        }

        public static VideoPlayer Create(VideoClip videoClip)
        {
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "VideoPlane";

            plane.transform.localScale = new Vector3(16f / 10f, 9f / 10f, 1);

            var renderTexture = RenderTexture.GetTemporary(1920, 1080);

            var material = new Material(Graphic.defaultGraphicMaterial.shader)
            {
                name = "VideoMaterial"
            };

            SetFade(material);
            material.mainTexture = renderTexture;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white);
            material.SetTexture("_EmissionMap", renderTexture);

            Object.DestroyImmediate(plane.GetComponent<Collider>());
            plane.GetComponent<Renderer>().material = material;

            var videoPlayer = plane.AddComponent<VideoPlayer>();
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.clip = videoClip;
            videoPlayer.playOnAwake = false;

            videoPlayer.add_loopPointReached((System.Action<VideoPlayer>) (_ =>
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                Object.Destroy(videoPlayer.gameObject);
                Object.Destroy(material);
            }));

            return videoPlayer;
        }
    }
}
