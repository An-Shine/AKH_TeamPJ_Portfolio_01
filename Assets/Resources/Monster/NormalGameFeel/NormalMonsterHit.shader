Shader "Monsters/NormalMonsterHit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _HitFlashAmount ("Solid Red Flash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment HitFragment
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _HitFlashAmount;
            fixed4 HitFragment(v2f input) : SV_Target
            {
                fixed4 texel = SampleSpriteTexture(input.texcoord);
                fixed4 color = texel * input.color;
                // RGB를 교체하므로 검정/파랑 픽셀도 정확히 빨간색이 됩니다. 알파는 유지합니다.
                color.rgb = lerp(color.rgb, fixed3(1, 0, 0), saturate(_HitFlashAmount));
                return color;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
