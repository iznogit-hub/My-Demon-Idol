Shader "CollegeRaimon/GrimDark_Mask"
{
    Properties
    {
        [Header(Base Appearance)]
        _BV_BaseTexture("Base Texture (Albedo)", 2D) = "white" {}
        _BV_BaseColor("Base Color Tint", Color) = (1, 1, 1, 1)
        _BV_Cutoff("Alpha Cutoff Threshold", Range(0.0, 1.0)) = 0.5

        [Header(Atmospheric Grim Dark Night)]
        _GrimDarkDesat("Night Desaturation", Range(0.0, 1.0)) = 0.28
        _NightAmbientTint("Night Tone Filter", Color) = (0.58, 0.65, 0.82, 1.0)
        _ShadowDarkness("Shadow Depth Floor", Range(0.0, 1.0)) = 0.75

        [Header(Wet Surfaces and Rain Slickness)]
        _Wetness("Surface Wetness", Range(0.0, 1.0)) = 0.85
        _PuddleGloss("Puddle Specular Gloss", Range(0.0, 1.0)) = 0.92
        _PuddleSmoothness("Base Smoothness", Range(0.0, 1.0)) = 0.35
        _RainRippleStrength("Rain Ripple Distortion", Range(0.0, 1.0)) = 0.35
        _RainRippleScale("Rain Ripple Frequency", Float) = 4.5
        _RainRippleSpeed("Rain Ripple Speed", Float) = 2.8

        [Header(Horror Glow and Streetlamps)]
        _HorrorGlowColor("Horror Glow Color", Color) = (1.0, 0.85, 0.5, 1.0)
        _HorrorGlowIntensity("Horror Glow Intensity", Range(0.0, 5.0)) = 0.0
        _FlickerSpeed("Horror Flicker Speed", Float) = 3.5

        [HideInInspector] _Cull("Culling", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 250

        // =========================================================================
        // Pass 1: Forward Lit (Alpha Cutout)
        // =========================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
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
                float  fogFactor  : TEXCOORD3;
            };

            TEXTURE2D(_BV_BaseTexture);
            SAMPLER(sampler_BV_BaseTexture);

            // Global storm variables driven by GrimDarkStormDirector
            float  _GlobalLightningIntensity;
            float4 _GlobalLightningColor;
            float4 _GlobalLightningDirection;
            float  _GlobalWetness;

            CBUFFER_START(UnityPerMaterial)
                float4 _BV_BaseTexture_ST;
                float4 _BV_BaseColor;
                float4 _NightAmbientTint;
                float4 _HorrorGlowColor;
                float  _BV_Cutoff;
                float  _GrimDarkDesat;
                float  _ShadowDarkness;
                float  _Wetness;
                float  _PuddleGloss;
                float  _PuddleSmoothness;
                float  _RainRippleStrength;
                float  _RainRippleScale;
                float  _RainRippleSpeed;
                float  _HorrorGlowIntensity;
                float  _FlickerSpeed;
                float  _Cull;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = normalInput.normalWS;
                output.uv         = TRANSFORM_TEX(input.uv, _BV_BaseTexture);
                output.color      = input.color;
                output.fogFactor  = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 LitPassFragment(Varyings input, FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // Sample base texture with vertex color and tint
                half4 texColor = SAMPLE_TEXTURE2D(_BV_BaseTexture, sampler_BV_BaseTexture, input.uv);
                half4 baseColor = texColor * _BV_BaseColor * input.color;

                // Alpha Cutout
                clip(baseColor.a - _BV_Cutoff);

                // Double-sided normal correction
                half facing = IS_FRONT_VFACE(frontFace, 1.0, -1.0);
                float3 normalWS = normalize(input.normalWS) * facing;
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                // 1. Atmospheric Grim Dark Tone Grading
                half lum = dot(baseColor.rgb, half3(0.299, 0.587, 0.114));
                half3 gradedAlbedo = lerp(baseColor.rgb, half3(lum, lum, lum), _GrimDarkDesat);
                gradedAlbedo *= _NightAmbientTint.rgb;

                // 2. Wet Surfaces & Rain Slickness
                float effectiveWetness = saturate(_Wetness * (_GlobalWetness > 0.001 ? _GlobalWetness : 1.0));
                half upFacing = saturate(normalWS.y * 1.5 - 0.2);
                half puddle = upFacing * effectiveWetness;

                half wetDarken = lerp(1.0, 0.45, effectiveWetness * (0.6 + 0.4 * upFacing));
                gradedAlbedo *= wetDarken;

                if (puddle > 0.05 && _RainRippleStrength > 0.01)
                {
                    float2 rUV1 = input.positionWS.xz * _RainRippleScale + float2(_Time.y * (0.45 * _RainRippleSpeed), _Time.y * (0.35 * _RainRippleSpeed));
                    float2 rUV2 = input.positionWS.xz * (_RainRippleScale * 1.55) - float2(_Time.y * (0.40 * _RainRippleSpeed), _Time.y * (0.60 * _RainRippleSpeed));
                    float wave1 = sin((rUV1.x + rUV1.y) * 10.0 + sin(rUV1.x * 7.0) * 1.5);
                    float wave2 = cos((rUV2.x - rUV2.y) * 13.0 + cos(rUV2.y * 8.0) * 1.5);
                    float2 ripplePerturb = float2(wave1, wave2) * (0.05 * puddle * _RainRippleStrength);
                    normalWS = normalize(normalWS + float3(ripplePerturb.x, 0.0, ripplePerturb.y));
                }

                half surfaceSmoothness = lerp(_PuddleSmoothness, _PuddleGloss, puddle);

                // 3. Main Moonlight & Shadow Sampling
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half shadowTerm = lerp(_ShadowDarkness, 1.0, mainLight.shadowAttenuation);
                half3 directDiffuse = mainLight.color * (NdotL * shadowTerm);

                half3 ambientGI = SampleSH(normalWS) * 0.45;
                half3 ambientFloor = half3(0.03, 0.04, 0.07);
                half3 effectiveAmbient = max(ambientGI, ambientFloor);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specPower = exp2(surfaceSmoothness * 8.5 + 2.0);
                float specTerm = pow(NdotH, specPower) * surfaceSmoothness;
                half3 specularLight = mainLight.color * (specTerm * mainLight.shadowAttenuation * 1.8);

                // 4. Real-time Stochastic Lightning Storm Reaction
                half3 lightningContribution = half3(0, 0, 0);
                if (_GlobalLightningIntensity > 0.005)
                {
                    float3 lgtDir = normalize(_GlobalLightningDirection.xyz + float3(0.001, 1.0, 0.001));
                    float lgtNdotL = saturate(dot(normalWS, lgtDir));
                    half3 lgtDiffuse = _GlobalLightningColor.rgb * (_GlobalLightningIntensity * (lgtNdotL * 0.7 + 0.3));

                    float3 lgtHalf = normalize(lgtDir + viewDirWS);
                    float lgtNdotH = saturate(dot(normalWS, lgtHalf));
                    float lgtSpecTerm = pow(lgtNdotH, exp2(surfaceSmoothness * 8.0 + 2.0)) * (surfaceSmoothness * 2.2);
                    half3 lgtSpecular = _GlobalLightningColor.rgb * (lgtSpecTerm * _GlobalLightningIntensity);

                    lightningContribution = (gradedAlbedo * lgtDiffuse) + lgtSpecular;
                }

                // 5. Additional Lights
                half3 addDiffuse = half3(0, 0, 0);
                half3 addSpecular = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS)
                uint addLightsCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < addLightsCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    half addNdotL = saturate(dot(normalWS, addLight.direction));
                    half addAtten = addLight.distanceAttenuation * addLight.shadowAttenuation;
                    addDiffuse += addLight.color * (addNdotL * addAtten);

                    float3 addHalf = normalize(addLight.direction + viewDirWS);
                    float addNdotH = saturate(dot(normalWS, addHalf));
                    float addSpec = pow(addNdotH, specPower) * surfaceSmoothness * addAtten;
                    addSpecular += addLight.color * addSpec;
                }
                #endif

                // 6. Horror Glow
                half3 horrorGlow = half3(0, 0, 0);
                if (_HorrorGlowIntensity > 0.01)
                {
                    float flicker = sin(_Time.y * _FlickerSpeed) * 0.12 + cos(_Time.y * (_FlickerSpeed * 2.1)) * 0.08 + 0.85;
                    horrorGlow = baseColor.rgb * _HorrorGlowColor.rgb * (_HorrorGlowIntensity * flicker);
                }

                // 7. Shading Composite
                half3 totalDiffuse = directDiffuse + effectiveAmbient + addDiffuse;
                half3 finalColor = (gradedAlbedo * totalDiffuse) + specularLight + addSpecular + lightningContribution + horrorGlow;

                // 8. Atmospheric Midnight Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 2: Shadow Caster (Alpha Cutout)
        // =========================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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
                float4 color      : COLOR;
            };

            TEXTURE2D(_BV_BaseTexture);
            SAMPLER(sampler_BV_BaseTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BV_BaseTexture_ST;
                float4 _BV_BaseColor;
                float4 _NightAmbientTint;
                float4 _HorrorGlowColor;
                float  _BV_Cutoff;
                float  _GrimDarkDesat;
                float  _ShadowDarkness;
                float  _Wetness;
                float  _PuddleGloss;
                float  _PuddleSmoothness;
                float  _RainRippleStrength;
                float  _RainRippleScale;
                float  _RainRippleSpeed;
                float  _HorrorGlowIntensity;
                float  _FlickerSpeed;
                float  _Cull;
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
                output.uv = TRANSFORM_TEX(input.uv, _BV_BaseTexture);
                output.color = input.color;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BV_BaseTexture, sampler_BV_BaseTexture, input.uv);
                half alpha = texColor.a * _BV_BaseColor.a * input.color.a;
                clip(alpha - _BV_Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 3: Depth Only (Alpha Cutout)
        // =========================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_BV_BaseTexture);
            SAMPLER(sampler_BV_BaseTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BV_BaseTexture_ST;
                float4 _BV_BaseColor;
                float4 _NightAmbientTint;
                float4 _HorrorGlowColor;
                float  _BV_Cutoff;
                float  _GrimDarkDesat;
                float  _ShadowDarkness;
                float  _Wetness;
                float  _PuddleGloss;
                float  _PuddleSmoothness;
                float  _RainRippleStrength;
                float  _RainRippleScale;
                float  _RainRippleSpeed;
                float  _HorrorGlowIntensity;
                float  _FlickerSpeed;
                float  _Cull;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BV_BaseTexture);
                output.color = input.color;
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BV_BaseTexture, sampler_BV_BaseTexture, input.uv);
                half alpha = texColor.a * _BV_BaseColor.a * input.color.a;
                clip(alpha - _BV_Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // =========================================================================
        // Pass 4: Depth Normals (Alpha Cutout)
        // =========================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 normalWS   : TEXCOORD0;
                float2 uv         : TEXCOORD1;
                float4 color      : COLOR;
            };

            TEXTURE2D(_BV_BaseTexture);
            SAMPLER(sampler_BV_BaseTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BV_BaseTexture_ST;
                float4 _BV_BaseColor;
                float4 _NightAmbientTint;
                float4 _HorrorGlowColor;
                float  _BV_Cutoff;
                float  _GrimDarkDesat;
                float  _ShadowDarkness;
                float  _Wetness;
                float  _PuddleGloss;
                float  _PuddleSmoothness;
                float  _RainRippleStrength;
                float  _RainRippleScale;
                float  _RainRippleSpeed;
                float  _HorrorGlowIntensity;
                float  _FlickerSpeed;
                float  _Cull;
            CBUFFER_END

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.positionCS = vertexInput.positionCS;
                output.normalWS   = normalInput.normalWS;
                output.uv         = TRANSFORM_TEX(input.uv, _BV_BaseTexture);
                output.color      = input.color;
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BV_BaseTexture, sampler_BV_BaseTexture, input.uv);
                half alpha = texColor.a * _BV_BaseColor.a * input.color.a;
                clip(alpha - _BV_Cutoff);

                float3 normalWS = normalize(input.normalWS);
                return half4(normalWS * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
