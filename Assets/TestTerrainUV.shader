Shader "Unlit/TestTerrainUV"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SecondTex ("Texture", 2D) = "white" {}
		_TextureScale ("TextureScale", Range(0.01, 5)) = 1
		_TriPlanarBlendSharpness ("TriPlanarBlendSharpness", Range(1, 20)) = 1
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
				float3 normal : NORMAL;
				float3 color :COLOR;
			};

			struct v2f
			{
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 lightDir : TEXCOORD2;
				fixed3 color : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _SecondTex;

			float _TriPlanarBlendSharpness;
			float _TextureScale;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.normal).xyz;
				o.lightDir = WorldSpaceLightDir(v.vertex);
				o.color = v.color;
				return o;
			}

			fixed3 getColor(sampler2D tex, float3 uv, float3 blending) {
				fixed3 xCol = tex2D(tex, uv.yz * _TextureScale);
				fixed3 yCol = tex2D(tex, uv.xz * _TextureScale);
				fixed3 zCol = tex2D(tex, uv.xy * _TextureScale);
				fixed3 finalColor = xCol * blending.x + yCol * blending.y + zCol * blending.z;
				return finalColor;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldNormal = normalize(i.worldNormal);
				float3 lightDir = normalize(i.lightDir);
				float dotNL = max(0.0, dot(worldNormal, lightDir));
				
				float3 blending = pow(abs(i.worldNormal), _TriPlanarBlendSharpness);
				blending /= (blending.x + blending.y + blending.z);
				fixed3 color1 = getColor(_MainTex, i.worldPos, blending);
				fixed3 color2 = getColor(_SecondTex, i.worldPos, blending);
				fixed3 color3 = fixed3(0, 0, 1);

				fixed3 finalColor = color1 * i.color.r + color2 * i.color.g + color3 * i.color.b;

				return fixed4(finalColor, 1);
			}
			ENDCG
		}
	}
}
