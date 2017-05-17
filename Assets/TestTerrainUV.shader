Shader "Unlit/TestTerrainUV"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
			};

			struct v2f
			{
				float3 worldPos : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 lightDir : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _TriPlanarBlendSharpness;
			float _TextureScale;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldNormal(v.vertex).xyz;
				o.lightDir = WorldSpaceLightDir(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldNormal = normalize(i.worldNormal);
				float3 lightDir = normalize(i.lightDir);
				float dotNL = max(0.0, dot(worldNormal, lightDir));

				float3 blending = pow(abs(i.worldNormal), _TriPlanarBlendSharpness);
				blending = normalize(max(blending, 0.00001));
				blending /= (blending.x + blending.y + blending.z);
				fixed3 xCol = tex2D(_MainTex, i.worldPos.yz * _TextureScale);
				fixed3 yCol = tex2D(_MainTex, i.worldPos.xz * _TextureScale);
				fixed3 zCol = tex2D(_MainTex, i.worldPos.xy * _TextureScale);
				fixed3 finalColor = xCol * blending.x + yCol * blending.y + zCol * blending.z;
				return fixed4(finalColor * dotNL, 1);
			}
			ENDCG
		}
	}
}
