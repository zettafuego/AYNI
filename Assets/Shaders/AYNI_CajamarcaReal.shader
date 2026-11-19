Shader "AYNI/CajamarcaReal"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _ColorHighlight ("Luz Lilas", Color) = (0.886, 0.808, 0.965, 1.0) // #E2CEF6
        _ColorOrchid ("Resplandor Orquidea", Color) = (0.851, 0.502, 0.976, 1.0) // #D980F9
        _ColorPurple ("Purpura Medio", Color) = (0.686, 0.314, 0.898, 1.0) // #AF50E5
        _ColorViolet ("Violeta Electrico", Color) = (0.541, 0.043, 0.824, 1.0) // #8A0BD2
        _ColorShadow ("Sombra Ciruela", Color) = (0.357, 0.024, 0.447, 1.0) // #5B0672
        _LightDir ("Direccion Luz", Vector) = (-0.35, 0.75, -0.45, 0.0)
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
            fixed4 _ColorHighlight;
            fixed4 _ColorOrchid;
            fixed4 _ColorPurple;
            fixed4 _ColorViolet;
            fixed4 _ColorShadow;
            float4 _LightDir;

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
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 n = UnityObjectToWorldNormal(v.normal);
                if (length(n) < 0.01) n = float3(0, 1, 0);
                else n = normalize(n);

                float3 ldir = normalize(_LightDir.xyz);
                float ndotl = dot(n, ldir);
                float direct = saturate(ndotl * 0.75 + 0.25);

                // Gradiente trilight según la normal vertical (cielo/horizonte/suelo)
                float up = n.y; // -1 .. 1
                float3 amb;
                if (up >= 0.0)
                    amb = lerp(_ColorPurple.rgb, _ColorOrchid.rgb, up);
                else
                    amb = lerp(_ColorPurple.rgb, _ColorShadow.rgb, -up);

                // Luz directa en tonalidad lila suave (#E2CEF6)
                float3 lit = amb * 0.58 + _ColorHighlight.rgb * (direct * 0.62);

                // Conservar color de vértice del modelo (tinte de fachadas, terreno, etc.)
                float3 vertCol = v.color.rgb;
                if (dot(vertCol, vertCol) < 0.001) vertCol = float3(1, 1, 1);

                o.color = float4(vertCol * lit, 1.0);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * i.color;

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
