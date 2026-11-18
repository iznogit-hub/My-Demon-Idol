Shader "DemonIdol/MDI/AnimeToon"
{
    Properties
    {
        [Header(Base Texture and Color)]
        _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor("Base Tint", Color) = (1, 1, 1, 1)

        [Header(RGBA Tint Mask)]
        [NoScaleOffset] _TintMask("Tint Mask (R:Skin, G:Hair, B:Primary, A:Secondary)", 2D) = "black" {}
        _SkinTint("Skin Tint (R)", Color) = (1, 1, 1, 1)
        _HairTint("Hair Tint (G)", Color) = (1, 1, 1, 1)
        _PrimaryTint("Primary Tint (B)", Color) = (1, 1, 1, 1)
        _SecondaryTint("Secondary Tint (A)", Color) = (1, 1, 1, 1)
        _AccentTint("Accent Tint", Color) = (1, 1, 1, 1)

        [Header(Cel Shading 90s Hand Painted)]
        _RampThreshold1("Cel Shade Step 1 (Shadow)", Range(0.0, 1.0)) = 0.42
        _RampThreshold2("Cel Shade Step 2 (Highlight)", Range(0.0, 1.0)) = 0.78
        _RampSmoothness("Cel Ink Feathering (Crisp Paint Edge)", Range(0.001, 0.08)) = 0.008
        _ShadowColor("90s Cel Shadow Tone", Color) = (0.68, 0.58, 0.72, 1)

        [Header(Hand Inked Cel Outlines)]
        [Toggle(_OUTLINE_ON)] _EnableOutline("Enable Inked Outline", Float) = 1.0
        _OutlineWidth("Ink Line Width (mm)", Range(0.0, 5.0)) = 1.2
        _OutlineColor("Cel Ink Color", Color) = (0.09, 0.07, 0.12, 1)

        [Header(Stylized Specular Glint 90s)]
        _SpecColor("Glint Tint", Color) = (1, 0.98, 0.92, 1)
        _SpecIntensity("Glint Intensity", Range(0.0, 5.0)) = 0.8
        _SpecPower("Glint Tightness", Range(1.0, 128.0)) = 48.0
        _SpecThreshold("Glint Step Cutoff", Range(0.0, 1.0)) = 0.55

        [Header(Cel Rim and Hair Light 90s)]
        _RimColor("Celluloid Backlight Color", Color) = (0.95, 0.92, 0.86, 0.6)
        _RimPower("Rim Exponent", Range(0.5, 8.0)) = 3.5
        _RimThreshold("Rim Cutoff (Sharp Paint)", Range(0.0, 1.0)) = 0.40

        [Header(Celluloid Film and Vintage 35mm Aesthetics)]
        _VintageWarmth("35mm Cel Film Warmth", Range(0.0, 1.0)) = 0.40
        _FilmGrain("Celluloid Film Grain", Range(0.0, 1.0)) = 0.08
        _HalationIntensity("Film Emulsion Halation (Glow)", Range(0.0, 1.0)) = 0.25

        [Header(Legacy and Emissive)]
        [NoScaleOffset] _EmissiveMap("Emissive Mask Map", 2D) = "black" {}
        [HDR] _NeonColor("Signature Tint (Legacy)", Color) = (0, 0, 0, 0)
        _NeonIntensity("Neon Multiplier (Disabled for 90s)", Range(0.0, 10.0)) = 0.0
        _ChromaticEdgeIntensity("Contour (Disabled for 90s)", Range(0.0, 5.0)) = 0.0
        _ChromaticEdgePower("Contour Sharpness", Range(0.5, 8.0)) = 5.2
        _ChromaticEdgeThreshold("Contour Cutoff", Range(0.0, 1.0)) = 0.22

        [Header(Rendering and Cutout Options)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Culling Mode", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping (Cutout)", Float) = 0
        _Cutoff("Alpha Cutoff Threshold", Range(0.0, 1.0)) = 0.5
        [Toggle] _ZWrite("ZWrite", Float) = 1.0

        [Header(Uniform Recoloring)]
        _UniformDesat("Costume Desaturation Amount", Range(0.0, 1.0)) = 0.0
        _UniformTint("Costume Tint", Color) = (1, 1, 1, 1)

        [HideInInspector] _MainTex("Base Map (Legacy Fallback)", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 200

        // =========================================================================
        // Pass 1: Forward Lit Toon Cel Shading (SRP Batcher Compatible, Forward & Deferred)
        // =========================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 color      : COLOR;
                float fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_TintMask);
            SAMPLER(sampler_TintMask);

            TEXTURE2D(_EmissiveMap);
            SAMPLER(sampler_EmissiveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _SkinTint;
                float4 _HairTint;
                float4 _PrimaryTint;
                float4 _SecondaryTint;
                float4 _AccentTint;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _NeonColor;
                float4 _OutlineColor;
                float4 _UniformTint;
                float4 _SpecColor;
                float _RampThreshold1;
                float _RampThreshold2;
                float _RampSmoothness;
                float _RimPower;
                float _RimThreshold;
                float _NeonIntensity;
                float _OutlineWidth;
                float _EnableOutline;
                float _Cutoff;
                float _AlphaClip;
                float _Cull;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _UniformDesat;
                float _ChromaticEdgeIntensity;
                float _ChromaticEdgePower;
                float _ChromaticEdgeThreshold;
                float _SpecIntensity;
                float _SpecPower;
                float _SpecThreshold;
                float _VintageWarmth;
                float _FilmGrain;
                float _HalationIntensity;
                float _Pad0;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                // 1. Sample Albedo and Tint Mask
                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                #if defined(_ALPHATEST_ON)
                clip(albedoTex.a * _BaseColor.a - _Cutoff);
                #endif
                half3 uniformBase = albedoTex.rgb;
                if (_UniformDesat > 0.001)
                {
                    half lum = dot(albedoTex.rgb, half3(0.299, 0.587, 0.114));
                    uniformBase = lerp(albedoTex.rgb, lum * _UniformTint.rgb, _UniformDesat);
                }
                half4 tintMask = SAMPLE_TEXTURE2D(_TintMask, sampler_TintMask, input.uv);
                half3 compositeColor = uniformBase * _BaseColor.rgb;

                // 2. Channel-mapped tinting:
                // Red = Skin, Green = Hair, Blue = Primary Outfit, Alpha = Secondary Outfit
                half maskWeight = tintMask.r + tintMask.g + tintMask.b + tintMask.a;
                if (maskWeight > 0.01)
                {
                    compositeColor = lerp(compositeColor, compositeColor * _SkinTint.rgb, tintMask.r);
                    compositeColor = lerp(compositeColor, compositeColor * _HairTint.rgb, tintMask.g);
                    compositeColor = lerp(compositeColor, compositeColor * _PrimaryTint.rgb, tintMask.b);
                    compositeColor = lerp(compositeColor, compositeColor * _SecondaryTint.rgb, tintMask.a);
                }
                // Vertex color modulation (for meshes with baked AO or stylized vertex tones)
                compositeColor *= lerp(half3(1, 1, 1), input.color.rgb, any(input.color.rgb < 0.99) ? 1.0 : 0.0);

                // 3. Lighting Vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // Sample Main Light with Shadow Coordination
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // 4. Directional Sun vs. Indoor / Studio Fallback
                // In indoor scenes, dungeons, or character viewer stages without a directional sun,
                // URP returns mainLight.color == (0,0,0). In high-end anime engines (Genshin, Arc System Works),
                // an automatic camera-relative studio key light is synthesized so characters are never pitch black.
                float mainLightLum = dot(mainLight.color.rgb, half3(0.299, 0.587, 0.114));
                float3 effLightDirWS;
                half3 effLightColor;
                half effShadowAtten;

                if (mainLightLum < 0.05)
                {
                    // Studio Key Light: 35 degrees up and slightly to the right of camera view
                    effLightDirWS = normalize(viewDirWS + float3(0.18, 0.40, 0.15));
                    effLightColor = half3(0.96, 0.94, 0.97);
                    effShadowAtten = 1.0;
                }
                else
                {
                    effLightDirWS = normalize(mainLight.direction);
                    effLightColor = mainLight.color;
                    effShadowAtten = mainLight.shadowAttenuation;
                }

                // 5. Environmental Ambient GI with Guaranteed Baseline Floor
                // Sample Spherical Harmonics environmental bounce
                half3 ambientGI = SampleSH(normalWS);
                // In indoor or unbaked scenes, SampleSH can be near-zero.
                // Enforce a vibrant anime ambient floor so character textures and details are always clear and vibrant.
                half3 minAmbientFloor = half3(0.55, 0.54, 0.58);
                half3 effectiveAmbient = max(ambientGI, minAmbientFloor);

                // 6. 90s Hand-Painted Cel Shading (Crisp, Graphic Steps)
                float halfLambert = dot(normalWS, effLightDirWS) * 0.5 + 0.5;
                halfLambert *= effShadowAtten;

                float thresh1 = min(_RampThreshold1, _RampThreshold2);
                float thresh2 = max(_RampThreshold1, _RampThreshold2);
                // Sharp feathering for hand-painted cel boundary (0.006 - 0.012 anti-aliased hard edge)
                float smoothDelta = max(_RampSmoothness, 0.006);

                float isLit = smoothstep(thresh1 - smoothDelta, thresh1 + smoothDelta, halfLambert);
                float isHighlight = smoothstep(thresh2 - smoothDelta, thresh2 + smoothDelta, halfLambert);

                // 90s Cel Shadow Tone: saturated, rich anime shadow (never pitch black)
                // In authentic 90s anime cel animation, shadow cel paint is 68%-75% of lit paint
                half3 shadowTone = lerp(_ShadowColor.rgb, half3(0.72, 0.70, 0.75), 0.45);

                // 2-Tone Painted Cel Core
                half3 directLighting = lerp(shadowTone * 0.72, half3(1, 1, 1), isLit) * effLightColor;
                // Add graphic 3rd highlight lift on topmost surfaces
                directLighting += isHighlight * 0.12 * effLightColor;

                // Ambient baseline: keeps shadow areas legible and rich
                half3 ambientLighting = effectiveAmbient * 0.25;

                // Subtle front camera fill (preserves facial expressions and hand-inked line readability)
                half nDotV = saturate(dot(normalWS, viewDirWS));
                half3 cameraFill = nDotV * effectiveAmbient * 0.18;

                // Combined diffuse lighting with minimum readability floor
                half3 diffuseLighting = directLighting + ambientLighting + cameraFill;
                diffuseLighting = max(diffuseLighting, half3(0.52, 0.50, 0.55));

                // 7. 90s Stylized Specular Glint (Hair Sheen / "Angel Ring")
                half3 celGlint = half3(0, 0, 0);
                if (_SpecIntensity > 0.01)
                {
                    float3 halfDirWS = normalize(effLightDirWS + viewDirWS);
                    float nDotH = saturate(dot(normalWS, halfDirWS));
                    float specTerm = pow(nDotH, max(_SpecPower, 16.0));
                    // Sharp step transition: painted glint shape
                    float specStep = smoothstep(_SpecThreshold - 0.03, _SpecThreshold + 0.03, specTerm);
                    celGlint = _SpecColor.rgb * (specStep * _SpecIntensity * isLit) * effLightColor;
                }

                // 8. 90s Celluloid Backlight Rim ("Komorebi Cel Rim")
                half3 celRim = half3(0, 0, 0);
                if (_RimColor.a > 0.01 || any(_RimColor.rgb > 0.01))
                {
                    float rimDot = 1.0 - nDotV;
                    float rimTerm = smoothstep(_RimThreshold - 0.03, _RimThreshold + 0.03, pow(rimDot, max(_RimPower, 1.5)));
                    // Accentuated along backlit angles
                    float backLightWeight = saturate(dot(-effLightDirWS, viewDirWS) * 0.5 + 0.5);
                    celRim = _RimColor.rgb * rimTerm * (backLightWeight * 0.75 + 0.25) * _RimColor.a;
                }

                // 9. Additional Lights (point lights, spotlights, lanterns)
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                half3 additionalDiffuse = half3(0, 0, 0);
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light addLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float addNdotL = dot(normalWS, addLight.direction) * 0.5 + 0.5;
                    float addAtten = addLight.distanceAttenuation * addLight.shadowAttenuation;
                    float addStep = smoothstep(0.42 - smoothDelta, 0.42 + smoothDelta, addNdotL);
                    additionalDiffuse += addLight.color * addAtten * (addStep * 0.65 + 0.35);
                }
                diffuseLighting += additionalDiffuse;
                #endif

                // 10. Legacy Emissive (if texture assigned)
                half4 emissiveTex = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, input.uv);
                half3 neonEmission = emissiveTex.rgb * _NeonColor.rgb * _NeonIntensity;

                // 11. Initial Shaded Composition
                half3 baseShaded = (compositeColor * diffuseLighting) + celGlint + celRim + neonEmission;

                // 12. 90s Vintage 35mm Celluloid Film Grade & Analog Texture
                half3 finalColor = baseShaded;

                // A. Warm Rostrum Film Curve (Golden-cream highlights & deep twilight shadows)
                if (_VintageWarmth > 0.001)
                {
                    half3 warmHighlight = half3(1.05, 1.01, 0.94); // warm golden cream highlights
                    half3 coolShadow = half3(0.96, 0.97, 1.03);    // deep twilight shadows
                    half lum = dot(baseShaded, half3(0.299, 0.587, 0.114));
                    half3 graded = lerp(baseShaded * coolShadow, baseShaded * warmHighlight, saturate(lum * 1.25));
                    finalColor = lerp(finalColor, graded, _VintageWarmth);
                }

                // B. Warm Emulsion Halation (Soft vintage celluloid bleed on bright highlights)
                if (_HalationIntensity > 0.001)
                {
                    half luma = dot(baseShaded, half3(0.299, 0.587, 0.114));
                    half highlightGate = saturate((luma - 0.62) * 2.8);
                    half3 halationColor = half3(1.0, 0.52, 0.28); // vintage 35mm warm peach halation
                    finalColor += halationColor * (highlightGate * _HalationIntensity * 0.14);
                }

                // C. Celluloid Film Grain (Physical acetate texture)
                if (_FilmGrain > 0.001)
                {
                    float2 screenPos = input.positionCS.xy;
                    float grain = frac(sin(dot(screenPos, float2(12.9898, 78.233))) * 43758.5453);
                    float grainValue = (grain - 0.5) * 2.0;
                    finalColor += grainValue * (_FilmGrain * 0.035);
                }

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, albedoTex.a * _BaseColor.a);
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 2: Inverted Hull Outline (Silhouette Pass)
        // =========================================================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex OutlinePassVertex
            #pragma fragment OutlinePassFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float fogFactor   : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _SkinTint;
                float4 _HairTint;
                float4 _PrimaryTint;
                float4 _SecondaryTint;
                float4 _AccentTint;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _NeonColor;
                float4 _OutlineColor;
                float4 _UniformTint;
                float4 _SpecColor;
                float _RampThreshold1;
                float _RampThreshold2;
                float _RampSmoothness;
                float _RimPower;
                float _RimThreshold;
                float _NeonIntensity;
                float _OutlineWidth;
                float _EnableOutline;
                float _Cutoff;
                float _AlphaClip;
                float _Cull;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _UniformDesat;
                float _ChromaticEdgeIntensity;
                float _ChromaticEdgePower;
                float _ChromaticEdgeThreshold;
                float _SpecIntensity;
                float _SpecPower;
                float _SpecThreshold;
                float _VintageWarmth;
                float _FilmGrain;
                float _HalationIntensity;
                float _Pad0;
            CBUFFER_END

            Varyings OutlinePassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                if (_EnableOutline < 0.5 || _OutlineWidth <= 0.001)
                {
                    output.positionCS = float4(0, 0, 0, 0);
                    return output;
                }

                // Extrude vertex along normal in Clip Space for uniform mobile screen width
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                float3 normalCS = TransformWorldToHClipDir(normalInput.normalWS);
                float2 offset = normalize(normalCS.xy);

                // Aspect ratio compensation for uniform mobile screen width (e.g. 16:9, 20:9)
                float aspect = _ScreenParams.y / max(_ScreenParams.x, 1.0);
                offset.x *= aspect;

                offset *= (_OutlineWidth * 0.0015) * vertexInput.positionCS.w;

                output.positionCS = vertexInput.positionCS;
                output.positionCS.xy += offset;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 OutlinePassFragment(Varyings input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(albedoTex.a * _BaseColor.a - _Cutoff);
                #endif
                half3 color = MixFog(_OutlineColor.rgb, input.fogFactor);
                return half4(color, _OutlineColor.a);
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 3: Shadow Caster Pass
        // =========================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _SkinTint;
                float4 _HairTint;
                float4 _PrimaryTint;
                float4 _SecondaryTint;
                float4 _AccentTint;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _NeonColor;
                float4 _OutlineColor;
                float4 _UniformTint;
                float4 _SpecColor;
                float _RampThreshold1;
                float _RampThreshold2;
                float _RampSmoothness;
                float _RimPower;
                float _RimThreshold;
                float _NeonIntensity;
                float _OutlineWidth;
                float _EnableOutline;
                float _Cutoff;
                float _AlphaClip;
                float _Cull;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _UniformDesat;
                float _ChromaticEdgeIntensity;
                float _ChromaticEdgePower;
                float _ChromaticEdgeThreshold;
                float _SpecIntensity;
                float _SpecPower;
                float _SpecThreshold;
                float _VintageWarmth;
                float _FilmGrain;
                float _HalationIntensity;
                float _Pad0;
            CBUFFER_END

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalInput.normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(albedoTex.a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 4: Depth Only Pass
        // =========================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _SkinTint;
                float4 _HairTint;
                float4 _PrimaryTint;
                float4 _SecondaryTint;
                float4 _AccentTint;
                float4 _ShadowColor;
                float4 _RimColor;
                float4 _NeonColor;
                float4 _OutlineColor;
                float4 _UniformTint;
                float4 _SpecColor;
                float _RampThreshold1;
                float _RampThreshold2;
                float _RampSmoothness;
                float _RimPower;
                float _RimThreshold;
                float _NeonIntensity;
                float _OutlineWidth;
                float _EnableOutline;
                float _Cutoff;
                float _AlphaClip;
                float _Cull;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _UniformDesat;
                float _ChromaticEdgeIntensity;
                float _ChromaticEdgePower;
                float _ChromaticEdgeThreshold;
                float _SpecIntensity;
                float _SpecPower;
                float _SpecThreshold;
                float _VintageWarmth;
                float _FilmGrain;
                float _HalationIntensity;
                float _Pad0;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                half4 albedoTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(albedoTex.a * _BaseColor.a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
