Shader "Unlit/DrawShader"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 posWS : TEXCOORD1;
            };

            sampler2D _SourceTex;
            float4 _Pos;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                //return length(uv - _Pos.xy);
                //return _Pos.z - length(uv - _Pos.xy)/_Pos.z;
                return max(_Pos.z - length(uv - _Pos.xy)/ _Pos.z, 0) + tex2D(_SourceTex, uv).x;
            }
            ENDCG
        }
    }
}
