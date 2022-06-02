using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {

	public Renderer textureRenderer;
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;

	public void DrawTexture(Texture2D texture) {
		
		textureRenderer.sharedMaterial.mainTexture = texture;
		textureRenderer.transform.localScale = new Vector3 (texture.width, 1, texture.height);
	}

	/*public void DrawMesh(MeshData meshData){
		StartCoroutine(meshData.CreateMesh());
        while (meshData.creatingMesh) {
			yield return null;
        }
		meshFilter.mesh = meshData.GetMesh();

		meshFilter.transform.localScale = Vector3.one * TerrainData.uniformScale;
	}*/
}
