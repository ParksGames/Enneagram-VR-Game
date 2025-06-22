Shader "Custom/WorldPositionRandomColorWithLighting"
{
    Properties
    {
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _MainTex_ST ("Tiling and Offset", Vector) = (1,1,0,0) // Tiling and Offset
        _Color1 ("Color 1", Color) = (1,0,0,1)
        _Color2 ("Color 2", Color) = (0,0,1,1)
        _Threshold ("Threshold", Float) = 10.0
        _Ambient ("Ambient", Float) = 0
        _Aggressiveness ("Aggressiveness", Float) = 2.0
        _RandomScale ("Random Scale", Float) = 3.0
        _RandomOffset ("Random Offset", Float) = 1.0
        _Steps ("Color Steps", Float) = 5.0
        _UseRandom ("Use Randomness", Int) = 1 // Boolean toggle for randomness
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0; // Texture UV
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;     // Object's world position
                float3 worldNormal : TEXCOORD1; // World-space normal
                float2 uv : TEXCOORD2;          // Adjusted UV
                float randomValue : TEXCOORD3;  // Random value or position-based increment
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float _Threshold;
                float _Ambient;
                float _Aggressiveness;
                float _RandomScale;
                float _RandomOffset;
                float _Steps;
                int _UseRandom;
                sampler2D _MainTex;
                float4 _MainTex_ST; // Tiling and Offset
            CBUFFER_END

            // Stable random function
            float StableRandom(float3 seed)
            {
                return frac(sin(dot(seed, float3(12.9898, 78.233, 45.164))) * 43758.5453);
            }

            // Vertex shader
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Transform object space to homogeneous clip space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Calculate world position and normal
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                // Apply tiling and offset to UV coordinates
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                // Use randomness or gradual increment based on world position
                if (_UseRandom == 1)
                {
                    float3 worldPivot = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                    OUT.randomValue = StableRandom(worldPivot * _RandomScale + _RandomOffset);
                }
                else
                {
                    OUT.randomValue = length(OUT.worldPos) * 0.1; // Gradual increase based on position
                }

                return OUT;
            }

            // Fragment shader
            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize world normal for proper lighting calculations
                float3 normalWS = normalize(IN.worldNormal);

                // Get the main directional light
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                // Calculate diffuse lighting
                float NdotL = saturate(dot(normalWS, lightDir));
                float3 diffuse = NdotL * lightColor;

                // Ambient light approximation (if no light, ambient is very low)
                float3 ambient =  _Ambient * lightColor;

                // Combine lighting components
                float3 lighting = ambient + diffuse;

                // Sample the albedo texture with adjusted UVs
                half4 albedoColor = tex2D(_MainTex, IN.uv);

                // Calculate scalar value based on world position
                float scalarValue = length(IN.worldPos);

                // Modify scalar value with randomness or gradual increment
                scalarValue += IN.randomValue * _RandomScale;

                // Normalize scalar value with threshold and aggressiveness
                float t = saturate(pow(scalarValue / _Threshold, _Aggressiveness));

                // Quantize t into steps with enhanced gaps
                float stepSize = 1.0 / _Steps;
                t = floor(t / stepSize) * stepSize;

                // Interpolate between Color1 and Color2 based on t
                half4 proceduralColor = lerp(_Color1, _Color2, t);

                // Multiply the procedural color with the albedo texture
                half4 finalColor = proceduralColor * albedoColor;

                // Apply lighting to the final color
                finalColor.rgb *= lighting;

                // Clamp the final color to avoid over-brightness
                finalColor.rgb = clamp(finalColor.rgb, 0.0, 1.0);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
