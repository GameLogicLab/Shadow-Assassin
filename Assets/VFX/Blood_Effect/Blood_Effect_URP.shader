Shader "Universal Render Pipeline/Particles/Blood Effect URP"
{
    Properties
    {
        [Header(Color Controls)]
        [HDR] _BaseColor ("Base Color Mult", Color) = (1,1,1,1)
        _LightStr ("Lighting Strength", Range(0,1)) = 0.85
        _AlphaMin ("Alpha Clip Min", Range(-0.01,1.01)) = 0.1
        _AlphaSoft ("Alpha Clip Softness", Range(0.001,1)) = 0.022
        _EdgeDarken ("Edge Darkening", Range(0,2)) = 1
        _ProcMask ("Procedural Mask Strength", Range(0,1)) = 1

        [Header(Mask Controls)]
        _MainTex ("Mask Texture", 2D) = "white" {}
        _MaskStr ("Mask Strength", Range(0,1)) = 0.7
        _Columns ("Flipbook Columns", Float) = 1
        _Rows ("Flipbook Rows", Float) = 1
        _ChannelMask ("Mask Channel Weights", Vector) = (1,0,0,0)
        [Toggle] _FlipU ("Flip U Randomly", Float) = 0
        [Toggle] _FlipV ("Flip V Randomly", Float) = 0

        [Header(Noise Controls)]
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseAlphaStr ("Noise Strength", Range(0,1)) = 0.8
        _ChannelMask2 ("Noise Channel Weights", Vector) = (1,0,0,0)
        _Randomize ("Randomize Noise", Float) = 1

        [Header(UV Warp)]
        _WarpTex ("Warp Texture", 2D) = "gray" {}
        _WarpStr ("Warp Strength", Float) = 0

        [Header(Vertex Physics)]
        _FallOffset ("Gravity Offset", Range(-1,0)) = -1
        _FallRandomness ("Gravity Randomness", Float) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_WarpTex); SAMPLER(sampler_WarpTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _LightStr;
                float _AlphaMin;
                float _AlphaSoft;
                float _EdgeDarken;
                float _ProcMask;
                float4 _MainTex_ST;
                float _MaskStr;
                float _Columns;
                float _Rows;
                float4 _ChannelMask;
                float _FlipU;
                float _FlipV;
                float4 _NoiseTex_ST;
                float _NoiseAlphaStr;
                float4 _ChannelMask2;
                float _Randomize;
                float4 _WarpTex_ST;
                float _WarpStr;
                float _FallOffset;
                float _FallRandomness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 uvRandomLifetime : TEXCOORD0; // UV.xy, random.z, lifetime.w
                float3 customData : TEXCOORD1;       // pan offset, warp strength, gravity
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 uv : TEXCOORD0;               // mask UV.xy, animated/noise UV.zw
                float4 color : COLOR;
                float3 customData : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;

                float lifetime = v.uvRandomLifetime.w;
                lifetime = lifetime * lifetime +
                    (_FallOffset + ((v.uvRandomLifetime.z - 0.5) * _FallRandomness)) * lifetime;

                float3 positionOS = v.positionOS.xyz;
                positionOS.y += lifetime * v.customData.z;

                float2 uvFlip = round(frac(float2(v.uvRandomLifetime.z * 13.0,
                                                   v.uvRandomLifetime.z * 8.0)));
                uvFlip = uvFlip * 2.0 - 1.0;
                uvFlip = lerp(float2(1,1), uvFlip, float2(_FlipU, _FlipV));

                o.positionHCS = TransformObjectToHClip(positionOS);
                o.color = v.color;
                o.color.a += _AlphaMin;

                float2 baseUV = (v.uvRandomLifetime.xy * uvFlip) * _MainTex_ST.xy + _MainTex_ST.zw;
                o.uv.xy = baseUV;
                o.uv.zw = baseUV * float2(max(_Columns,1), max(_Rows,1))
                    + v.uvRandomLifetime.z * float2(3,8) * _Randomize;

                o.customData = v.customData;
                o.fogFactor = ComputeFogFactor(o.positionHCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 noiseUV = i.uv.zw * _NoiseTex_ST.xy
                    + _NoiseTex_ST.zw * i.customData.x;

                float4 warpSample = SAMPLE_TEXTURE2D(
                    _WarpTex, sampler_WarpTex,
                    i.uv.zw * _WarpTex_ST.xy + _WarpTex_ST.zw * (i.customData.x + 1)
                    + float2(5,8) * i.customData.z);

                float2 warp = (warpSample.xy * 2.0 - 1.0) * _WarpStr * i.customData.y;

                half4 mask = SAMPLE_TEXTURE2D(
                    _MainTex, sampler_MainTex,
                    i.uv.xy * _MainTex_ST.xy + _MainTex_ST.zw + warp);
                mask = saturate(lerp(half4(1,1,1,1), mask, _MaskStr));

                float2 tileUV = frac(i.uv.xy * float2(max(_Columns,1), max(_Rows,1))) - 0.5;
                tileUV *= tileUV * 4.0;
                half edgeMask = 1.0 - saturate(tileUV.x + tileUV.y);
                edgeMask = lerp(1.0, edgeMask * edgeMask, _ProcMask);
                mask *= edgeMask;

                half4 col = max(0.001h, i.color) * _BaseColor;
                col.a = saturate(dot(mask, _ChannelMask));

                half4 noiseSample = SAMPLE_TEXTURE2D(
                    _NoiseTex, sampler_NoiseTex, noiseUV + warp);
                half noise = saturate(lerp(1.0h, dot(noiseSample, _ChannelMask2), _NoiseAlphaStr));

                col.a *= noise;
                half preClipAlpha = col.a;
                half alphaSoft = max(_AlphaSoft, 0.001);
                half clippedAlpha = saturate((preClipAlpha * i.color.a - _AlphaMin) / alphaSoft);
                col.a = clippedAlpha;

                half edge = 1.0h - saturate(preClipAlpha * clippedAlpha);
                edge *= edge;
                edge = 1.0h - edge;
                edge = saturate(lerp(0.71h, edge * edge, _EdgeDarken));
                col.a *= saturate(lerp(1.25h, _BaseColor.a, edge));

                col.rgb *= lerp(min(col.rgb * col.rgb * col.rgb * 0.3h, 1.0h), 0.71h, edge);
                col.rgb *= lerp(1.0h, 1.0h, saturate(_LightStr));

                col.rgb = MixFog(col.rgb, i.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
