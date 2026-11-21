Shader "CollegeRaimon/GrimDark_Blend"
{
    Properties
    {
        [Header(Base Appearance)]
        _BV_BaseTexture("Base Texture (Albedo)", 2D) = "white" {}
        _BV_BaseColor("Base Color Tint", Color) = (1, 1, 1, 0.8)

        [Header(Atmospheric Grim Dark Night)]
        _GrimDarkDesat("Night Desaturation", Range(0.0, 1.0)) = 0.28
        _NightAmbientTint("Night Tone Filter", Color) = (0.58, 0.65, 0.82, 1.0)
        _ShadowDarkness("Shadow Depth Floor", Range(0.0, 1.0)) = 0.75

        [Header(Wet Surfaces and Rain Slickness)]
        _Wetness("Surface Wetness", Range(0.0, 1.0)) = 0.85
        _PuddleGloss("Glass Specular Gloss", Range(0.0, 1.0)) = 0.95
        _PuddleSmoothness("Base Smoothness", Range(0.0, 1.0)) = 0.65

        [Header(Horror Glow and Windows)]
        _HorrorGlowColor("Horror Glow Color", Color) = (1.0, 0.85, 0.5, 1.0)
        _HorrorGlowIntensity("Horror Glow Intensity", Range(0.0, 5.0)) = 0.0
        _FlickerSpeed("Horror Lamp Flicker Speed", Float) = 3.5

        [HideInInspector] _Cull("Culling", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 250

        // =========================================================================
        // Pass 1: Forward Lit (Transparent Blend)
        // =========================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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

            float  _GlobalLightningIntensity;
            float4 _GlobalLightningColor;
            float4 _GlobalLightningDirection;
            float  _GlobalWetness;

            CBUFFER_START(UnityPerMaterial)
                float4 _BV_BaseTexture_ST;
                float4 _BV_BaseColor;
                float4 _NightAmbientTint;
                float4 _HorrorGlowColor;
                float  _GrimDarkDesat;
                float  _ShadowDarkness;
                float  _Wetness;
                float  _PuddleGloss;
                float  _PuddleSmoothness;
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
                half facing = IS_FRONT_VFACE(frontFace, 1.0, -1.0);
                float3 normalWS = normalize(input.normalWS) * facing;
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                half4 texColor = SAMPLE_TEXTURE2D(_BV_BaseTexture, sampler_BV_BaseTexture, input.uv);
                half4 baseColor = texColor * _BV_BaseColor * input.color;

                half lum = dot(baseColor.rgb, half3(0.299, 0.587, 0.114));
                half3 gradedAlbedo = lerp(baseColor.rgb, half3(lum, lum, lum), _GrimDarkDesat);
                gradedAlbedo *= _NightAmbientTint.rgb;

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 directDiffuse = mainLight.color * NdotL;

                half3 ambientGI = SampleSH(normalWS) * 0.45;
                half3 ambientFloor = half3(0.04, 0.05, 0.08);
                half3 effectiveAmbient = max(ambientGI, ambientFloor);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specPower = exp2(_PuddleGloss * 8.5 + 2.0);
                float specTerm = pow(NdotH, specPower) * _PuddleGloss;
                half3 specularLight = mainLight.color * (specTerm * 2.0);

                half3 lightningContribution = half3(0, 0, 0);
                if (_GlobalLightningIntensity > 0.005)
                {
                    float3 lgtDir = normalize(_GlobalLightningDirection.xyz + float3(0.001, 1.0, 0.001));
                    float lgtNdotL = saturate(dot(normalWS, lgtDir));
                    half3 lgtDiffuse = _GlobalLightningColor.rgb * (_GlobalLightningIntensity * (lgtNdotL * 0.7 + 0.3));

                    float3 lgtHalf = normalize(lgtDir + viewDirWS);
                    float lgtNdotH = saturate(dot(normalWS, lgtHalf));
                    float lgtSpecTerm = pow(lgtNdotH, exp2(_PuddleGloss * 8.0 + 2.0)) * (_PuddleGloss * 2.5);
                    half3 lgtSpecular = _GlobalLightningColor.rgb * (lgtSpecTerm * _GlobalLightningIntensity);

                    lightningContribution = (gradedAlbedo * lgtDiffuse) + lgtSpecular;
                }

                half3 horrorGlow = half3(0, 0, 0);
                if (_HorrorGlowIntensity > 0.01)
                {
                    float flicker = sin(_Time.y * _FlickerSpeed) * 0.12 + cos(_Time.y * (_FlickerSpeed * 2.1)) * 0.08 + 0.85;
                    horrorGlow = baseColor.rgb * _HorrorGlowColor.rgb * (_HorrorGlowIntensity * flicker);
                }

                half3 totalDiffuse = directDiffuse + effectiveAmbient;
                half3 finalColor = (gradedAlbedo * totalDiffuse) + specularLight + lightningContribution + horrorGlow;

                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
