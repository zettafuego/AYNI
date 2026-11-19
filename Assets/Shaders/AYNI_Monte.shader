Shader "AYNI/Monte"
{
    Properties
    {
        _Wind ("Wind", Float) = 0.28
        _Speed ("Speed", Float) = 1.7
        _Phase ("Phase", Float) = 0
    }
    SubShader
    {
        // Side-on fight: sprites sort by order and do not write depth.
        // ZTest Always keeps the grass from punching holes in the night veil.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Wind;
            float _Speed;
            float _Phase;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                float sway = v.color.a;
                float phase = v.vertex.x * 0.37 + v.vertex.z * 0.55 + _Phase;
                float t = _Time.y * _Speed + phase;
                v.vertex.x += sin(t) * _Wind * sway;
                v.vertex.y += (cos(t * 0.5) - 1.0) * _Wind * 0.12 * sway;
                v.vertex.z += cos(t * 0.8) * _Wind * 0.35 * sway;

                // Authored normals, not the scaled object matrix, so a wide ledge keeps its light.
                float3 n = normalize(v.normal);
                float ndotl = dot(n, normalize(float3(-0.35, 0.85, -0.4)));
                float lit = abs(ndotl) * 0.55 + 0.45;

                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = float4(v.color.rgb * lit, 1);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack Off
}
