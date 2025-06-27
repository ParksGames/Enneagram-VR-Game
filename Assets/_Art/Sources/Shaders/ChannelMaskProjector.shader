Shader "Hidden/ChannelMaskProjector" {
    Properties { _ProjTex("Projection Tex", 2D) = "white" {} }
    SubShader {
        Cull Off ZWrite Off ZTest LEqual
        Pass { // Red channel
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragR
            #include "UnityCG.cginc"
            sampler2D _ProjTex;
            float4x4 _ProjMatrix;
            struct v2f { float4 pos : SV_POSITION; float4 world : TEXCOORD0; };
            v2f vert(appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex); return o; }
            fixed4 fragR(v2f i) : SV_Target {
                float4 proj = mul(_ProjMatrix, float4(i.world.xyz,1)); proj /= proj.w;
                float mask = tex2D(_ProjTex, proj.xy * 0.5 + 0.5).a;
                return fixed4(mask,0,0,0);
            }
            ENDCG
        }
        Pass { // Green channel (similar, write to .g)
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragG
            #include "UnityCG.cginc"
            sampler2D _ProjTex;
            float4x4 _ProjMatrix;
            struct v2f { float4 pos : SV_POSITION; float4 world : TEXCOORD0; };
            v2f vert(appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex); return o; }
            fixed4 fragG(v2f i) : SV_Target {
                float4 proj = mul(_ProjMatrix, float4(i.world.xyz,1)); proj /= proj.w;
                float mask = tex2D(_ProjTex, proj.xy * 0.5 + 0.5).a;
                return fixed4(0,mask,0,0);
            }
            ENDCG
        }
        Pass { // Blue channel
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragB
            #include "UnityCG.cginc"
            sampler2D _ProjTex;
            float4x4 _ProjMatrix;
            struct v2f { float4 pos : SV_POSITION; float4 world : TEXCOORD0; };
            v2f vert(appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex); return o; }
            fixed4 fragB(v2f i) : SV_Target {
                float4 proj = mul(_ProjMatrix, float4(i.world.xyz,1)); proj /= proj.w;
                float mask = tex2D(_ProjTex, proj.xy * 0.5 + 0.5).a;
                return fixed4(0,0,mask,0);
            }
            ENDCG
        }
        Pass { // Alpha channel
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragA
            #include "UnityCG.cginc"
            sampler2D _ProjTex;
            float4x4 _ProjMatrix;
            struct v2f { float4 pos : SV_POSITION; float4 world : TEXCOORD0; };
            v2f vert(appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.world = mul(unity_ObjectToWorld, v.vertex); return o; }
            fixed4 fragA(v2f i) : SV_Target {
                float4 proj = mul(_ProjMatrix, float4(i.world.xyz,1)); proj /= proj.w;
                float mask = tex2D(_ProjTex, proj.xy * 0.5 + 0.5).a;
                return fixed4(0,0,0,mask);
            }
            ENDCG
        }
    }
}