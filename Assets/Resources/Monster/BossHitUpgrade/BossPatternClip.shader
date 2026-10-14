Shader "Monsters/BossPatternClip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _BossArenaMask ("Arena Mask", 2D) = "white" {}
        _BossArena ("Boss Arena", Vector) = (-100000, -100000, 100000, 100000)
        _BossArenaClip ("Arena Clipping", Float) = 0
        _BossMaskClip ("Wall Clipping", Float) = 1
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Desaturate ("Desaturation", Range(0, 1)) = 0
        _Gain ("Brightness", Float) = 1
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha [_DstBlend]
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex BossVert
            #pragma fragment BossFrag
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            sampler2D _BossArenaMask;
            float4 _BossArena;
            float _BossArenaClip;
            float _BossMaskClip;
            float _Desaturate;
            float _Gain;

            struct BossOutput
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 world : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            BossOutput BossVert(appdata_t input)
            {
                v2f sprite = SpriteVert(input);
                BossOutput output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = sprite.vertex;
                output.color = sprite.color;
                output.texcoord = sprite.texcoord;
                float4 local = input.vertex;
                local.xy *= _Flip.xy;
                output.world = mul(unity_ObjectToWorld, local).xy;
                return output;
            }

            float4 BossFrag(BossOutput input) : SV_Target
            {
                if (_BossArenaClip > 0.5)
                {
                    clip(input.world.x - _BossArena.x);
                    clip(input.world.y - _BossArena.y);
                    clip(_BossArena.z - input.world.x);
                    clip(_BossArena.w - input.world.y);
                    if (_BossMaskClip > 0.5)
                    {
                        float2 maskUV = (input.world - _BossArena.xy) /
                            max(_BossArena.zw - _BossArena.xy, float2(0.001, 0.001));
                        clip(tex2D(_BossArenaMask, maskUV).r - 0.5);
                    }
                }

                float4 color = SampleSpriteTexture(input.texcoord) * input.color;
                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                color.rgb = lerp(color.rgb, luminance.xxx, _Desaturate) * _Gain;
                return color;
            }
            ENDCG
        }
    }
}
