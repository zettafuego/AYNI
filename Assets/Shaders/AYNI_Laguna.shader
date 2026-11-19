Shader "AYNI/Laguna"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Tinte", Color) = (0.7, 0.88, 0.84, 0.66)
        _Speed ("Velocidad", Float) = 0.04
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float wave = sin(v.vertex.x * 1.1 + _Time.y * 1.2) * 0.02;
                wave += sin(v.vertex.z * 1.6 - _Time.y * 0.9) * 0.015;
                v.vertex.y += wave;
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float Ola(float2 p)
            {
                float w = sin(p.x * 14.0 + _Time.y * 1.3) * sin(p.y * 11.0 - _Time.y * 1.0);
                w += sin(p.x * 5.5 - p.y * 7.0 + _Time.y * 0.6) * 0.5;
                return w;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 a = i.uv + float2(_Time.y * _Speed, sin(_Time.y * 0.6) * 0.01);
                float2 b = i.uv * 1.7 + float2(-_Time.y * _Speed * 0.4, _Time.y * _Speed * 0.25);
                fixed4 tex = tex2D(_MainTex, a) * 0.65 + tex2D(_MainTex, b) * 0.35;
                float e = 0.02;
                float h = Ola(i.uv);
                float hx = Ola(i.uv + float2(e, 0));
                float hy = Ola(i.uv + float2(0, e));
                float3 n = normalize(float3(h - hx, hy - h, 0.24));
                float spec = pow(saturate(n.z), 16.0);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fres = pow(1.0 - saturate(viewDir.y), 3.0);
                fixed3 rgb = tex.rgb * _Color.rgb;
                rgb += spec * fixed3(0.9, 0.97, 1.0) * 0.38;
                rgb = lerp(rgb, fixed3(0.78, 0.9, 0.94), fres * 0.28);
                float alpha = saturate(_Color.a + fres * 0.18);
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
