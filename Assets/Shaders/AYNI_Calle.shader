Shader "AYNI/Calle"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
    }
    SubShader
    {
        // Streets are looked at from above and from a tilt. Depth has to hold.
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
            float _Noche;
            float _FarolN;
            float4 _Faroles[36];
            float _ShowN;
            float4 _ShowLuz[8];
            float4 _ShowColor[8];

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
                // Sin canal de color el vértice llega negro y la calle desaparece.
                if (max(tint.r, max(tint.g, tint.b)) < 0.04) tint = float3(1, 1, 1);
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = float4(tint * lit, 1);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                col.a = 1;
                // De noche la calle se apaga. Cerca de una farola vuelve el tono amarillo.
                float noche = saturate(_Noche);
                if (noche > 0.001)
                {
                    float mejor = 0;
                    int n = (int)_FarolN;
                    if (n > 36) n = 36;
                    for (int k = 0; k < 36; k++)
                    {
                        if (k >= n) break;
                        float alcance = max(_Faroles[k].w, 0.01);
                        float d = distance(i.worldPos, _Faroles[k].xyz);
                        float kLuz = saturate(1.0 - d / alcance);
                        kLuz *= kLuz;
                        mejor = max(mejor, kLuz);
                    }

                    float3 apagado = col.rgb * float3(0.07, 0.08, 0.12);
                    float3 encendido = col.rgb * float3(1.12, 0.86, 0.46);
                    col.rgb = lerp(col.rgb, lerp(apagado, encendido, mejor), noche);
                }

                int nShow = (int)_ShowN;
                if (nShow > 8) nShow = 8;
                if (nShow > 0)
                {
                    float3 gel = float3(0, 0, 0);
                    float suma = 0;
                    for (int s = 0; s < 8; s++)
                    {
                        if (s >= nShow) break;
                        float alcance = max(_ShowLuz[s].w, 0.01);
                        float d = distance(i.worldPos, _ShowLuz[s].xyz);
                        float kLuz = saturate(1.0 - d / alcance);
                        kLuz *= kLuz;
                        gel += kLuz * _ShowColor[s].rgb;
                        suma += kLuz;
                    }

                    if (suma > 0.001)
                    {
                        float3 tinta = gel / suma;
                        col.rgb = lerp(col.rgb, col.rgb * tinta * 1.35, saturate(suma));
                    }
                }
                // One quad covers the whole field. Fog has to be per pixel or the center fogs with the corners.
                // If unity_FogParams has no range, skip it: every pixel would become the sky color
                // and the street would look transparent.
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
