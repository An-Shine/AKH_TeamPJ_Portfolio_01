Shader "Monster/NormalFeelPixelRim"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Float) = 1.4
        _UvBounds ("Sprite UV Bounds", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _Brightness;
            float4 _UvBounds;
            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }
            float AlphaAt(float2 uv)
            {
                float inside = step(_UvBounds.x, uv.x) * step(_UvBounds.y, uv.y) *
                    step(uv.x, _UvBounds.z) * step(uv.y, _UvBounds.w);
                return tex2D(_MainTex, uv).a * inside;
            }
            float4 frag(v2f input) : SV_Target
            {
                float2 pixel = _MainTex_TexelSize.xy;
                float alpha = AlphaAt(input.uv);
                float neighbor = min(min(AlphaAt(input.uv + float2(pixel.x, 0)),
                    AlphaAt(input.uv - float2(pixel.x, 0))),
                    min(AlphaAt(input.uv + float2(0, pixel.y)),
                    AlphaAt(input.uv - float2(0, pixel.y))));
                float rim = saturate(alpha - neighbor);
                return float4(input.color.rgb * _Brightness, input.color.a * rim);
            }
            ENDCG
        }
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _Brightness;
            float4 _UvBounds;
            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }
            float AlphaAt(float2 uv)
            {
                float inside = step(_UvBounds.x, uv.x) * step(_UvBounds.y, uv.y) *
                    step(uv.x, _UvBounds.z) * step(uv.y, _UvBounds.w);
                return tex2D(_MainTex, uv).a * inside;
            }
            float4 frag(v2f input) : SV_Target
            {
                float2 pixel = _MainTex_TexelSize.xy;
                float alpha = AlphaAt(input.uv);
                float neighbor = min(min(AlphaAt(input.uv + float2(pixel.x, 0)),
                    AlphaAt(input.uv - float2(pixel.x, 0))),
                    min(AlphaAt(input.uv + float2(0, pixel.y)),
                    AlphaAt(input.uv - float2(0, pixel.y))));
                float rim = saturate(alpha - neighbor);
                return float4(input.color.rgb * _Brightness, input.color.a * rim);
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
