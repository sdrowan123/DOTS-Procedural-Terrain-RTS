using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData {

	const int textureSize = 80;
	const TextureFormat textureFormat = TextureFormat.RGBA32;

	public int heightTrim;
	public Layer[] layers;
	public Material material;
	bool foundTerrainData = false;

	float savedMinHeight;
	float savedMaxHeight;
	float savedHeightRound;

	
	public void ApplyToMaterial (Material material){
		if(MapGenerator.TerrainData != null) {
			savedMinHeight = MapGenerator.TerrainData.minHeight;
			savedMaxHeight = MapGenerator.TerrainData.maxHeight * heightTrim;
			savedHeightRound = MapGenerator.TerrainData.heightRound;
		}

		Texture2DArray texturesUpArray = GenerateTextureArray(layers.Select(x => x.textureUp).ToArray());
		Texture2DArray texturesSlopesArrayLight = GenerateTextureArray(layers.Select(x => x.textureSlopesLight).ToArray());
		Texture2DArray texturesSlopesArray = GenerateTextureArray(layers.Select(x => x.textureSlopes).ToArray());
		Texture2DArray texturesDiagonalsArrayLight = GenerateTextureArray(layers.Select(x => x.textureDiagonalsLight).ToArray());
		Texture2DArray texturesDiagonalsArray = GenerateTextureArray(layers.Select(x => x.textureDiagonals).ToArray());
		Texture2DArray texturesCliffsArray = GenerateTextureArray(layers.Select(x => x.textureCliffs).ToArray());

		Debug.Log("Layers: " + layers.Length);

		material.SetInt("layerCount", layers.Length);
		material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
		material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
		//Debug.Log("Max Height: " + savedMaxHeight);
		//foreach (float height in material.GetFloatArray("baseStartHeights")) Debug.Log("Heights: ")
		material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
		material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
		material.SetTexture("baseTextures", texturesUpArray);
		//for (int i = 0; i < materials.Length; i++) {
		//materials[i].SetInt("layerCount", layers.Length);
		//materials[i].SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
		//materials[i].SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
		//materials[i].SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
		//materials[i].SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());

		//if (i >= 9) {
		//	materials[i].SetTexture("baseTextures", texturesCliffsArray);
		//}else if(i >= 7) {
		//	materials[i].SetTexture("baseTextures", texturesDiagonalsArray);
		//} else if (i >= 5) {
		//	materials[i].SetTexture("baseTextures", texturesSlopesArray);
		//} else if (i >= 3) {
		//	materials[i].SetTexture("baseTextures", texturesDiagonalsArrayLight);
		//} else if (i >= 1) {
		//	materials[i].SetTexture("baseTextures", texturesSlopesArrayLight);
		//} else {materials[i].SetTexture("baseTextures", texturesUpArray);}
		//}

		UpdateMeshHeights(material, savedMinHeight, savedMaxHeight, savedHeightRound);
	}

	public void UpdateMeshHeights(Material material, float minHeight, float maxHeight, float heightRound){
		savedMinHeight = minHeight;
		savedMaxHeight = maxHeight * TerrainData.uniformScale;
		savedHeightRound = heightRound;

		//Debug.Log("heights Updated");
		//Debug.Log("MaxHeight = " + maxHeight);

		material.SetFloat("minHeight", minHeight);
		material.SetFloat("maxHeight", maxHeight);
		material.SetFloat("heightRound", heightRound);
	}

	Texture2DArray GenerateTextureArray(Texture2D[] textures) {
		Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, false);
		textureArray.filterMode = FilterMode.Point;
		textureArray.wrapMode = TextureWrapMode.Repeat;
		textureArray.anisoLevel = 1;
		for (int i = 0; i < textures.Length; i++) {
			textureArray.SetPixels32(textures[i].GetPixels32(), i);
		}
		textureArray.Apply();
		return textureArray;
	}

	[System.Serializable]
	public class Layer {
		public Texture2D textureUp;
		public Texture2D textureSlopesLight;
		public Texture2D textureSlopes;
		public Texture2D textureDiagonalsLight;
		public Texture2D textureDiagonals;
		public Texture2D textureCliffs;
		public Color tint;
		[Range(0,1)]
		public float tintStrength;
		[Range(0,1)]
		public float startHeight;
		public float textureScale;
	}
}
