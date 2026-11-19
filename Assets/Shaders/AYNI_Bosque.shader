Shader "AYNI/Bosque"
{
    Properties
    {
        _Color ("Color", Color) = (0.23, 0.55, 0.28, 1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back
        ZWrite On
        ZTest LEqual
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                float3 n = normalize(v.normal);
                float lit = abs(dot(n, normalize(float3(-0.28, 0.86, -0.4)))) * 0.4 + 0.6;
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = fixed4(_Color.rgb * lit, 1);
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
