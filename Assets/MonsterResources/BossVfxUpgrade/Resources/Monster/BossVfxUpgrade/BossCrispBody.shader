Shader "Monsters/BossVfx/Body"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _PreviousTex ("Previous Frame", 2D) = "white" {}
        _CurrentUv ("Current UV", Vector) = (0,0,1,1)
        _PreviousUv ("Previous UV", Vector) = (0,0,1,1)
        _CurrentLocal ("Current Local", Vector) = (-0.5,-0.5,1,1)
        _PreviousLocal ("Previous Local", Vector) = (-0.5,-0.5,1,1)
        _FrameBlend ("Frame Blend", Range(0,1)) = 1
        _NeonColor ("Cast Color", Color) = (0.1,0.88,1,1)
        _NeonStrength ("Local Emission", Range(0,2)) = 0.28
    }
    CGINCLUDE
    #include "UnitySprites.cginc"
    sampler2D _PreviousTex;
    float4 _CurrentUv;
    float4 _PreviousUv;
    float4 _CurrentLocal;
    float4 _PreviousLocal;
    float _FrameBlend;
    float4 _NeonColor;
    float _NeonStrength;
    float4 BossBodyFrag(v2f input) : SV_Target
    {
        float4 color = SampleSpriteTexture(input.texcoord);
        if (_FrameBlend < 0.999)
        {
            float2 norm = (input.texcoord - _CurrentUv.xy) / max(_CurrentUv.zw - _CurrentUv.xy, 0.00001);
            float2 local = _CurrentLocal.xy + norm * _CurrentLocal.zw;
            float2 previousNorm = (local - _PreviousLocal.xy) / max(_PreviousLocal.zw, 0.00001);
            float2 previousUv = lerp(_PreviousUv.xy, _PreviousUv.zw, saturate(previousNorm));
            float4 previous = tex2D(_PreviousTex, previousUv);
            previous.a *= step(0, previousNorm.x) * step(previousNorm.x, 1)
                * step(0, previousNorm.y) * step(previousNorm.y, 1);
            color = lerp(previous, color, _FrameBlend);
        }
        float brightest = max(color.r, max(color.g, color.b));
        float darkest = min(color.r, min(color.g, color.b));
        float mask = smoothstep(0.4, 0.82, brightest) * smoothstep(0.08, 0.35, brightest - darkest);
        color.rgb += _NeonColor.rgb * _NeonStrength * mask * 0.32;
        color *= input.color;
        clip(color.a - 0.006);
        return color;
    }
    ENDCG
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossBodyFrag
            #pragma multi_compile_instancing
            ENDCG
        }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossBodyFrag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "BuiltIn"
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment BossBodyFrag
            #pragma multi_compile_instancing
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
