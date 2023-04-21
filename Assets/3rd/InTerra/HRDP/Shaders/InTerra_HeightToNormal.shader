Shader "Hidden/InTerra/CalculateNormal"
{
    Properties
    {
        _TerrainHeightmapTexture("Texture", 2D) = "red" {}
        _HeightmapScale("hs", Vector) = (0,0,0)
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            
			uniform sampler2D _TerrainHeightmapTexture; 
            float4 _TerrainHeightmapTexture_TexelSize;
            float3 _HeightmapScale;

            fixed4 frag(v2f_img i) : SV_Target {
                
            float4 hmUv = float4(i.uv,0,0);
            float hm = tex2Dlod(_TerrainHeightmapTexture, hmUv).r;
            float4 ts = float4(_TerrainHeightmapTexture_TexelSize.x, _TerrainHeightmapTexture_TexelSize.y, 0, 0);
            float4 hsX = _HeightmapScale.y / _HeightmapScale.x;
            float4 hsZ = _HeightmapScale.y / _HeightmapScale.z;

            float4 height;
            float3 norm;

            height[0] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, hmUv + float4(ts * float2(0, -1), 0, 0))).r * hsZ;
            height[1] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, hmUv + float4(ts * float2(-1, 0), 0, 0))).r * hsX;
            height[2] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, hmUv + float4(ts * float2( 1, 0), 0, 0))).r * hsX;
            height[3] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, hmUv + float4(ts * float2( 0, 1), 0, 0))).r * hsZ;
            
            norm.x = height[1] - height[2];
            norm.z = height[0] - height[3];            
            norm.y = 1;

		    return  float4((normalize(norm)+1)/2, 0);
            }
            ENDCG
        }
    }
}
