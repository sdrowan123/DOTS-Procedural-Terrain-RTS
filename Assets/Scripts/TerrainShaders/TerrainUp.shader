Shader "Custom/TerrainUp" {
	Properties {
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxLayerCount = 16;
		const static float epsilon = 1E-4;

		int layerCount;
		float3 baseColors[maxLayerCount];
		float baseStartHeights[maxLayerCount];
		float baseColorStrength[maxLayerCount];
		float baseTextureScales[maxLayerCount];

		float minHeight;
		float maxHeight;
		float heightRound;

		sampler2D testTexture;
		float testScale;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		struct Input {
			float3 worldPos;
		};

		float inverseLerp(float a, float b, float value){
			return saturate((value-a)/(b-a));
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			float heightPercent = inverseLerp(minHeight, maxHeight, (round((IN.worldPos.y-2.5)/5 + epsilon) * 5 + 1E-1));

			for(int i = 0; i < layerCount; i++){
				float drawStrength = saturate(sign(heightPercent - baseStartHeights[i]));

				float3 baseColor = baseColors[i] * baseColorStrength[i];
				float3 scaledWorldPos = IN.worldPos / baseTextureScales[i];
				float3 textureColor = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, i)) * (1-baseColorStrength[i]);
				o.Albedo = o.Albedo * (1-drawStrength) + (baseColor+textureColor) * drawStrength;
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}
