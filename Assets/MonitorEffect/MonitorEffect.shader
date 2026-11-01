
Shader "Custom/MonitorEffect"
{
    Properties
    {
        [Toggle] _IsBroken ("Is Broken", Float) = 0

        [Header(Normal Mode)]
        _GoodNoise ("Good Noise Strength", Range(0, 1)) = 0.45
        _GoodNoiseDensity ("Good Noise Density", Range(0, 1)) = 0.8

        [Header(Broken Mode)]
        _BadNoise ("Bad Noise Strength", Range(0, 1)) = 1
        _BadNoiseDensity ("Bad Noise Density", Range(0, 1)) = 1
        _BrokenDarkness ("Broken Background Darkness", Range(0, 1)) = 0.85
        _BrokenStaticOpacity ("Broken Static Opacity", Range(0, 1)) = 0.9

        [Header(Static Grain)]
        _NoiseScale ("Noise Scale", Range(0.25, 4)) = 1
        _NoiseSpeed ("Noise Speed", Range(1, 120)) = 60
        _ClusterStrength ("Cluster Strength", Range(0, 1)) = 0.5
        _LargeNoiseRatio ("Large Noise Ratio", Range(0, 1)) = 0.3
        _LargeNoiseSize ("Large Noise Size", Range(2, 20)) = 8

        [Header(Horizontal Band)]
        _BandSpeed ("Band Speed", Range(0, 2)) = 0.25
        _BandWidth ("Band Width", Range(0.01, 0.3)) = 0.09
        _BandStrength ("Band Strength", Range(0, 1)) = 0.55
        _BandDistortion ("Band Distortion", Range(0, 0.05)) = 0.003

        [Header(Scanline)]
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "MonitorEffect"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _IsBroken;
                float _GoodNoise;
                float _GoodNoiseDensity;
                float _BadNoise;
                float _BadNoiseDensity;
                float _BrokenDarkness;
                float _BrokenStaticOpacity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _ClusterStrength;
                float _LargeNoiseRatio;
                float _LargeNoiseSize;
                float _BandSpeed;
                float _BandWidth;
                float _BandStrength;
                float _BandDistortion;
                float _ScanlineStrength;
            CBUFFER_END

            uint HashUInt(uint x)
            {
                x ^= x >> 16;
                x *= 0x7feb352du;
                x ^= x >> 15;
                x *= 0x846ca68bu;
                x ^= x >> 16;
                return x;
            }

            float RandomPixel(uint2 pixel, uint frame, uint seed)
            {
                uint h = pixel.x * 0x9e3779b9u;
                h ^= pixel.y * 0x85ebca6bu;
                h ^= frame * 0xc2b2ae35u;
                h ^= seed;
                return (HashUInt(h) & 0x00ffffffu) * (1.0 / 16777216.0);
            }

            float RandomCell(int2 cell, uint frame, uint seed)
            {
                return RandomPixel(asuint(cell), frame, seed);
            }

            half3 SampleScreen(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, saturate(uv)).rgb;
            }

            float GetScrollingBand(float y, float t, float offset, float width)
            {
                float center = 1.0 + width - frac(t * _BandSpeed + offset) * (1.0 + 2.0 * width);
                return 1.0 - smoothstep(width * 0.25, width, abs(y - center));
            }

            float GetLargeFlakes(float2 p, uint frame)
            {
                float size = max(_LargeNoiseSize, 2.0);
                float2 grid = p / size;
                int2 baseCell = (int2)floor(grid);
                float mask = 0.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        int2 cell = baseCell + int2(x, y);

                        float active = RandomCell(cell, frame, 0x7a91c35du);
                        float2 jitter = float2(
                            RandomCell(cell, frame, 0x42b719e3u),
                            RandomCell(cell, frame, 0xd374a129u)
                        );

                        float2 center = float2(cell) + 0.2 + jitter * 0.6;
                        float radius = lerp(0.18, 0.48, RandomCell(cell, frame, 0x9a57d613u));
                        float2 delta = grid - center;
                        float distanceToCenter = length(delta * float2(1.0, 1.15));

                        float flake = 1.0 - smoothstep(radius * 0.65, radius, distanceToCenter);
                        flake *= step(0.63, active);
                        mask = max(mask, flake);
                    }
                }

                return mask;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 screenSize = _ScreenParams.xy;
                float t = _Time.y;
                float broken = step(0.5, _IsBroken);
                uint frame = (uint)floor(t * _NoiseSpeed);

                float band1 = GetScrollingBand(uv.y, t, 0.0, _BandWidth);
                float band2 = GetScrollingBand(uv.y, t, 0.55, _BandWidth * 0.35);
                float band = max(band1, band2 * 0.45);
                float interference = band * _BandStrength;

                float bandWave = sin(uv.y * 130.0 + t * 4.0);
                float shift = interference * _BandDistortion * (0.65 + 0.35 * bandWave);

                half3 sceneColor = SampleScreen(uv + float2(shift, 0.0));

                float2 p = uv * screenSize * _NoiseScale;
                int2 ip = (int2)floor(p);
                uint2 pixel = (uint2)max(float2(ip), 0.0);

                float r0 = RandomPixel(pixel, frame, 0x12345678u);
                float r1 = RandomPixel(pixel, frame, 0x87654321u);
                float r2 = RandomPixel(pixel, frame, 0x31415926u);
                float r3 = RandomPixel(pixel, frame, 0xa53c91e7u);

                float left = RandomCell(ip + int2(-1, 0), frame, 0x87654321u);
                float right = RandomCell(ip + int2(1, 0), frame, 0x87654321u);
                float up = RandomCell(ip + int2(0, 1), frame, 0x87654321u);
                float down = RandomCell(ip + int2(0, -1), frame, 0x87654321u);

                float neighborNoise = (left + right + up + down) * 0.25;
                float clusteredNoise = lerp(r1, neighborNoise, _ClusterStrength);

                float jaggedNoise = lerp(r1, clusteredNoise, 0.4);
                jaggedNoise = saturate(jaggedNoise + (r2 - 0.5) * 0.3);

                float largeFlakes = GetLargeFlakes(p, frame);
                float flakeBreakup = step(0.18, r3);
                largeFlakes *= flakeBreakup;

                float normalMask = step(1.0 - _GoodNoiseDensity, r0);
                float normalStatic = (jaggedNoise * 2.0 - 1.0) * normalMask;
                normalStatic += largeFlakes * _LargeNoiseRatio * 0.55;

                float3 normalColor = sceneColor;
                normalColor += normalStatic * _GoodNoise;

                float brokenFine = saturate((r1 - 0.5) * 1.65 + 0.5);
                float brokenCluster = saturate((clusteredNoise - 0.5) * 2.4 + 0.5);

                float staticGray = lerp(brokenFine, brokenCluster, _ClusterStrength * 0.45);
                staticGray = lerp(staticGray, step(0.5, r2), 0.2);

                float flakeSign = step(0.5, RandomPixel(pixel, frame, 0x91b42f73u)) * 2.0 - 1.0;
                staticGray = saturate(staticGray + largeFlakes * flakeSign * _LargeNoiseRatio * 1.2);

                float brokenMask = step(1.0 - _BadNoiseDensity, r0);
                float staticOpacity = brokenMask * _BrokenStaticOpacity * _BadNoise;

                float3 brokenBackground = sceneColor * (1.0 - _BrokenDarkness);
                float3 brokenColor = lerp(brokenBackground, staticGray.xxx, staticOpacity);

                float3 finalColor = lerp(normalColor, brokenColor, broken);

                float bandGrain = RandomPixel(pixel, frame, 0x7c29e4a1u) * 2.0 - 1.0;
                finalColor += bandGrain * interference * 0.3;
                finalColor *= 1.0 - interference * 0.28;

                float scanline = 0.5 + 0.5 * cos(uv.y * screenSize.y * 3.14159265);
                finalColor *= 1.0 - scanline * _ScanlineStrength;

                return half4(saturate(finalColor), 1.0);
            }

            ENDHLSL
        }
    }
}
