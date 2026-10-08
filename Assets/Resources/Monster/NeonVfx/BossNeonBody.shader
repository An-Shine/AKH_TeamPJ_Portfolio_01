Shader "Monsters/BossNeonBody"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _PreviousTex ("Previous frame", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _CurrentUv ("Frame UV", Vector) = (0,0,1,1)
        _PreviousUv ("Previous UV", Vector) = (0,0,1,1)
        _CurrentLocal ("Frame local rectangle", Vector) = (-1,-1,2,2)
        _PreviousLocal ("Previous local rectangle", Vector) = (-1,-1,2,2)
        _FrameBlend ("Frame blend", Range(0,1)) = 1
        _NeonStrength ("Neon detail", Range(0,1)) = 0.17
        _NeonColor ("Neon accent", Color) = (0.1,0.88,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            sampler2D _PreviousTex;
            float4 _CurrentUv, _PreviousUv, _CurrentLocal, _PreviousLocal, _NeonColor;
            float _FrameBlend, _NeonStrength;
            float4 frag(v2f i) : SV_Target
            {
                float2 local=(i.texcoord-_CurrentUv.xy)/max(_CurrentUv.zw-_CurrentUv.xy,0.00001);
                float2 localPosition=_CurrentLocal.xy+local*_CurrentLocal.zw;
                float2 previousLocal=(localPosition-_PreviousLocal.xy)/max(_PreviousLocal.zw,0.00001);
                float2 previousUv=lerp(_PreviousUv.xy,_PreviousUv.zw,saturate(previousLocal));
                float4 a=tex2D(_PreviousTex,previousUv);
                a.a*=step(0,previousLocal.x)*step(0,previousLocal.y)*step(previousLocal.x,1)*step(previousLocal.y,1);
                float4 b=SampleSpriteTexture(i.texcoord);
                float alpha=lerp(a.a,b.a,_FrameBlend);
                float3 rgb=lerp(a.rgb*a.a,b.rgb*b.a,_FrameBlend)/max(alpha,0.00001);
                // Lift the dark armor, retain bright edge detail, then emit accents.
                rgb=pow(saturate(rgb),0.84)*1.09;
                float purple=saturate((rgb.r+rgb.b-rgb.g*1.8)*1.15);
                float detail=saturate(max(rgb.r,max(rgb.g,rgb.b))-0.22)*purple;
                rgb+=_NeonColor.rgb*detail*_NeonStrength*0.85;
                float core=saturate(rgb.g-max(rgb.r,rgb.b)*0.8);
                rgb+=float3(0.1,1,0.7)*core*_NeonStrength*0.28;
                alpha*=i.color.a;
                return float4(rgb*i.color.rgb*alpha,alpha);
            }
            ENDCG
        }
    }
}
