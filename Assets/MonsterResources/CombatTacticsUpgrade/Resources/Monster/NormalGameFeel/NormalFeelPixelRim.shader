Shader "Monsters/NormalGameFeel/PixelRim"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _UvBounds ("Frame Bounds", Vector) = (0,0,1,1)
    }
    CGINCLUDE
    #include "UnitySprites.cginc"
    float4 _MainTex_TexelSize;
    float4 _UvBounds;
    float BoundedAlpha(float2 uv)
    {
        float inside = step(_UvBounds.x, uv.x) * step(uv.x, _UvBounds.z)
            * step(_UvBounds.y, uv.y) * step(uv.y, _UvBounds.w);
        return SampleSpriteTexture(uv).a * inside;
    }
    float4 BossRimFrag(v2f input) : SV_Target
    {
        float2 stepUv = _MainTex_TexelSize.xy;
        float center = BoundedAlpha(input.texcoord);
        float neighbor = max(max(BoundedAlpha(input.texcoord + float2(stepUv.x,0)),
            BoundedAlpha(input.texcoord - float2(stepUv.x,0))),
            max(BoundedAlpha(input.texcoord + float2(0,stepUv.y)),
            BoundedAlpha(input.texcoord - float2(0,stepUv.y))));
        float edge = saturate(neighbor - center);
        float4 color = input.color;
        color.a *= edge * 0.78;
        clip(color.a - 0.01);
        return color;
    }
    ENDCG
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossRimFrag
            #pragma multi_compile_instancing
            ENDCG
        }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossRimFrag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            Name "BuiltIn"
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossRimFrag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    Fallback Off
}
