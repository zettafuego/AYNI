Shader "AYNI/Rio"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Tinte", Color) = (0.16, 0.48, 0.44, 1)
        _Speed ("Velocidad", Float) = 0.16
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Speed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                float wave = sin(v.vertex.x * 1.4 + _Time.y * 1.7) * 0.045;
                wave += sin(v.vertex.z * 2.1 - _Time.y * 1.3) * 0.03;
                v.vertex.y += wave;
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float Ola(float2 p)
            {
                float w = sin(p.x * 16.0 + _Time.y * 1.6) * sin(p.y * 12.0 - _Time.y * 1.2);
                w += sin(p.x * 6.5 - p.y * 8.0 + _Time.y * 0.7) * 0.55;
                return w;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 a = uv + float2(_Time.y * _Speed, sin(_Time.y * 0.8) * 0.015);
                float2 b = uv * 1.8 + float2(-_Time.y * _Speed * 0.55, _Time.y * _Speed * 0.32);
                fixed4 tex = tex2D(_MainTex, a) * 0.62 + tex2D(_MainTex, b) * 0.38;
                float e = 0.018;
                float h = Ola(uv);
                float hx = Ola(uv + float2(e, 0));
                float hy = Ola(uv + float2(0, e));
                float3 n = normalize(float3(h - hx, hy - h, 0.22));
                float spec = pow(saturate(n.z), 18.0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fres = pow(1.0 - saturate(viewDir.y), 3.0);
                fixed3 rgb = tex.rgb * _Color.rgb;
                rgb *= lerp(0.78, 1.05, saturate(h * 0.5 + 0.5));
                rgb += spec * fixed3(0.82, 0.93, 0.98) * 0.42;
                rgb = lerp(rgb, fixed3(0.72, 0.86, 0.90), fres * 0.38);
                fixed4 col = fixed4(rgb, 1);
                #if defined(FOG_LINEAR)
                    float dist = distance(i.worldPos, _WorldSpaceCameraPos);
                    float fog = saturate(dist * unity_FogParams.z + unity_FogParams.w);
                    col.rgb = lerp(unity_FogColor.rgb, col.rgb, fog);
                #endif
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
