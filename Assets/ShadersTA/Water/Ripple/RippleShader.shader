Shader "Unlit/RippleShader"
{
    Properties
    {
        [HideInInspector]_RippleSpeed ("RippleSpeed", Range(1, 10)) = 1
        [HideInInspector]_RippleLifeTime("RippleLifeTime", Range(-0.99, 1)) = 0
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
            };

            sampler2D _prevRT;
            sampler2D _currentRT;
            float4 _currentRT_TexelSize;

            float _RippleSpeed;
            float _RippleLifeTime;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //最小偏移单位
                float3 e = float3(_currentRT_TexelSize.xy, 0) * _RippleSpeed;
                float2 uv = i.uv;
                //获取当前帧的上下左右
                float p10 = tex2D(_currentRT, uv - e.zy).x; //下
                float p01 = tex2D(_currentRT, uv - e.xz).x; //左
                float p21 = tex2D(_currentRT, uv + e.xz).x; //右
                float p12 = tex2D(_currentRT, uv + e.zy).x; //上

                float p11 = tex2D(_prevRT, uv).x; //中心
                float d = (p10 + p01 + p21 + p12) * 0.5f - p11; //计算偏移量
                d *= (0.99f + _RippleLifeTime); //衰减
                
                return d;
            }
            ENDCG
        }
    }
}
