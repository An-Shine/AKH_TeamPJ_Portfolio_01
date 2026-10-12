Shader "Monsters/NeonParticle"
{
    Properties
    {
        _MainTex ("Particle", 2D) = "white" {}
        _Additive ("Additive", Float) = 1
        _Desaturate ("Neon recolor", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float _Additive, _Desaturate;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
            float4 frag(v2f i) : SV_Target
            {
                float4 t=tex2D(_MainTex,i.uv);
                float l=max(t.r,max(t.g,t.b));
                t.rgb=lerp(t.rgb,l.xxx,_Desaturate);
                float a=t.a*i.color.a;
                // Alpha is retained for smoke; energy emits light through a brighter color.
                return float4(t.rgb*i.color.rgb*(1+_Additive*0.4)*a,a*(1-_Additive));
            }
            ENDCG
        }
    }
}
