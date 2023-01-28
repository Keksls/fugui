Shader "FuGui/3DUIWindow"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _BackColor ("Back Color", Color) = (1, 1, 1, 1)
        _Displacement ("Displacement", Range(0, 10)) = 0.1
    }
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 200

        Pass{
        CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        sampler2D _MainTex;
        fixed4 _BackColor;
        float _Displacement;

        v2f vert (appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            fixed4 col = tex2Dlod(_MainTex, float4(v.uv,0,0));
            if(col.r != _BackColor.r && col.g != _BackColor.g && col.b != _BackColor.b) {
                o.vertex.xyz += col.rgb * _Displacement;
            }

            return o;
        }

        fixed4 frag (v2f i) : SV_Target {
            fixed4 col = tex2D(_MainTex, i.uv);
            return col;
        }
        ENDCG 
    }
    }
    FallBack "Diffuse"
}
