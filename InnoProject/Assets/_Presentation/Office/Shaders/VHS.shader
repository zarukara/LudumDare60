Shader "Office/VHS"
{
    Properties
    {
        [PerRendererData] _MainTex ("Frame", 2D) = "black" {}
        _Strength ("Strength", Range(0, 1)) = 1
        _Noise ("Grain", Range(0, 0.5)) = 0.02
        _Bleed ("Colour Bleed (px)", Range(0, 8)) = 1.2
        _Jitter ("Line Jitter", Range(0, 0.01)) = 0.0003
        _Tracking ("Tracking Band", Range(0, 1)) = 0.2
        _HeadSwitch ("Bottom Noise", Range(0, 1)) = 0.35
        _Dropouts ("Dropouts", Range(0, 1)) = 0.06
        _Scanlines ("Scanlines", Range(0, 1)) = 0.08
        _Saturation ("Saturation", Range(0, 2)) = 0.85
        _Tint ("Tint", Color) = (0.96, 1, 0.94, 1)
        _Vignette ("Vignette", Range(0, 3)) = 0.7
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Strength, _Noise, _Bleed, _Jitter, _Tracking, _HeadSwitch, _Dropouts, _Scanlines, _Saturation, _Vignette;
            float4 _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float drift(float row, float time)
            {
                float k = floor(time);
                float f = frac(time);
                return lerp(hash(float2(row, k)), hash(float2(row, k + 1.0)), f * f * (3.0 - 2.0 * f));
            }

            float3 tap(float2 uv, float shift)
            {
                return float3(tex2D(_MainTex, uv + float2(shift, 0)).r, tex2D(_MainTex, uv).g, tex2D(_MainTex, uv - float2(shift, 0)).b);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;
                float rows = _MainTex_TexelSize.w;
                float row = floor(uv.y * rows * 0.5);

                float shift = (drift(row, t * 6.0) - 0.5) * _Jitter + sin(uv.y * 30.0 + t * 1.7) * _Jitter * 0.4;

                float band = saturate(1.0 - abs(uv.y - (1.0 - frac(t * 0.06))) / 0.03);
                band = band * band * (3.0 - 2.0 * band) * _Tracking;
                shift += band * (drift(row, t * 8.0) - 0.5) * 0.025;

                float bottom = saturate(1.0 - uv.y / 0.02) * _HeadSwitch;
                shift += bottom * (0.015 + (drift(row, t * 10.0) - 0.5) * 0.02);
                uv.x += shift * _Strength;

                float px = _MainTex_TexelSize.x;
                float bleed = _Bleed * px * _Strength;
                float3 col = tap(uv, bleed);
                float3 smear = (tap(uv + float2(px * 2.0, 0), bleed) + tap(uv - float2(px * 2.0, 0), bleed)) * 0.5;
                col = lerp(col, smear, 0.45 * _Strength);

                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(float3(luma, luma, luma), col, lerp(1.0, _Saturation, _Strength));
                col *= lerp(float3(1, 1, 1), _Tint.rgb, _Strength);
                col = lerp(col, col * 0.9 + 0.02, _Strength);

                float scan = 1.0 - _Scanlines * (0.5 + 0.5 * cos(i.uv.y * rows * 3.14159));
                col *= lerp(1.0, scan, _Strength);
                float n = hash(i.uv * _MainTex_TexelSize.zw + frac(t * 7.31) * 100.0) - 0.5;
                col += n * _Noise * _Strength * (0.3 + 0.7 * saturate(luma * 2.0));
                float tick = floor(t * 20.0);
                float dropout = step(1.0 - 0.0025 * _Dropouts, hash(float2(row, tick))) * step(0.85, hash(float2(floor(i.uv.x * 40.0), row + tick)));
                col += dropout * 0.35 * _Strength;
                col += band * 0.1 * _Strength;
                col = lerp(col, 0.35 + n * 0.8, bottom * 0.6 * _Strength);

                float2 d = i.uv - 0.5;
                col *= 1.0 - dot(d, d) * _Vignette * _Strength;
                return fixed4(saturate(col) * i.color.rgb, 1.0);
            }
            ENDCG
        }
    }
}
