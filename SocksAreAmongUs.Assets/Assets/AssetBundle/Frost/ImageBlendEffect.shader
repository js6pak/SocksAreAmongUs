Shader "Custom/ImageBlendEffect"
{
    Properties
    {
        _MainTex ("Base", 2D) = "" {}
        blend_texure ("Image", 2D) = "" {}
        bump_map ("Normalmap", 2D) = "bump" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct v2f
    {
        float4 pos : POSITION;
        float2 uv : TEXCOORD0;
    };

    sampler2D _MainTex;
    sampler2D blend_texure;
    sampler2D bump_map;

    float blend_amount;

    v2f vert(const appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        return o;
    }

    half4 frag(const v2f i) : COLOR
    {
        const float edge_sharpness = 1;
        const float see_throughness = 0.2f;
        const float distortion = 0.1f;

        float4 blend_color = tex2D(blend_texure, i.uv);

        blend_color.a = blend_color.a + (blend_amount * 2 - 1);
        blend_color.a = saturate(blend_color.a * edge_sharpness - (edge_sharpness - 1) * 0.5);

        const half2 bump = UnpackNormal(tex2D(bump_map, i.uv)).rg;
        const float4 mainColor = tex2D(_MainTex, i.uv + bump * blend_color.a * distortion);

        float4 overlay_color = blend_color;
        overlay_color.rgb = mainColor.rgb * (blend_color.rgb + 0.5) * (blend_color.rgb + 0.5);

        blend_color = lerp(blend_color, overlay_color, see_throughness);

        return lerp(mainColor, blend_color, blend_color.a);
    }
    ENDCG

    Subshader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog
            {
                Mode off
            }

            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }

    Fallback off
}