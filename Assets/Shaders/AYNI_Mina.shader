Shader "AYNI/Mina"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Emision ("Emision", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Blend Off
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
            float _Emision;
            float _MinaAmb;
            float4 _MinaLuzPos[12];
            float4 _MinaLuzDir[12];
            int _MinaLuzN;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                float nlen = dot(v.normal, v.normal);
                float3 n = nlen > 1e-6 ? v.normal * rsqrt(nlen) : float3(0, 1, 0);
                float lit = abs(dot(n, normalize(float3(-0.25, 0.9, -0.3)))) * 0.62 + 0.38;
                float3 tint = v.color.rgb;
                if (max(tint.r, max(tint.g, tint.b)) < 0.04) tint = float3(1, 1, 1);
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = float4(tint * lit, 1);
                return o;
            }

            float Farol(float3 worldPos)
            {
                float sum = 0;
                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float on = i < _MinaLuzN ? 1 : 0;
                    float3 fromL = worldPos - _MinaLuzPos[i].xyz;
                    float d = length(fromL);
                    float range = max(_MinaLuzPos[i].w, 0.2);
                    float att = saturate(1 - d / range);
                    att *= att;
                    float cone = _MinaLuzDir[i].w;
                    if (cone >= 0 && d > 0.05)
                    {
                        float facing = dot(fromL / max(d, 1e-4), _MinaLuzDir[i].xyz);
                        att *= saturate((facing - cone) / max(0.08, 1 - cone));
                    }

                    sum += att * on;
                }

                return saturate(sum);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float amb = _MinaAmb < 0.001 ? 1 : _MinaAmb;
                float luz = Farol(i.worldPos);
                float shade = _Emision > 0.5 ? 1 : amb + luz;
                col.rgb *= shade;
                col.a = 1;
                #if defined(FOG_LINEAR)
                    if (unity_FogParams.z < -1e-5)
                    {
                        float dist = distance(i.worldPos, _WorldSpaceCameraPos);
                        float fog = saturate(dist * unity_FogParams.z + unity_FogParams.w);
                        col.rgb = lerp(unity_FogColor.rgb, col.rgb, fog);
                    }
                #endif
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
