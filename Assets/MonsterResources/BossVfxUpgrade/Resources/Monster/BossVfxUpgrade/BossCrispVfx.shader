Shader "Monsters/BossVfx/Crisp"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
        _Desaturate ("Theme Tint", Range(0,1)) = 0
        _Gain ("Local Exposure", Range(0.5,2)) = 1
    }
    CGINCLUDE
    #include "UnitySprites.cginc"
    float _Desaturate;
    float _Gain;
    float4 BossFrag(v2f input) : SV_Target
    {
        float4 sample = SampleSpriteTexture(input.texcoord);
        float luminance = dot(sample.rgb, float3(0.299, 0.587, 0.114));
        sample.rgb = lerp(sample.rgb, luminance.xxx, _Desaturate);
        sample.rgb *= input.color.rgb * _Gain;
        sample.a *= input.color.a;
        clip(sample.a - 0.006);
        return sample;
    }
    ENDCG
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha [_DstBlend]
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossFrag
            #pragma multi_compile_instancing
            ENDCG
        }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossFrag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha [_DstBlend]
        Pass
        {
            Name "BuiltIn"
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossFrag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
