using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
//using NUnit.Framework;
using System.Xml.Linq;
using UnityEditor.UI;
using UnityEngine.XR.WSA.Input;
using UnityEngine.UI;
using System;
using System.Diagnostics;
using System.Collections.Specialized;
using Unity.Collections;
//using NUnit.Framework.Constraints;
using Debug = UnityEngine.Debug;
using System.Drawing;
using Unity.Mathematics;

//EndlessTerrain combines terrainchunks and lodmeshes and all that to make a world. This is where all the math converges.
//After much deliberation, I decided to make this into a singleton as there should only be one instance of it and it needs to be accessed a lot
//That said, this whole setup is a little messy and should be revisited, there are still calls to findobject for this script in the codebase
//All references should be changed to use singleton methods.

public class EndlessTerrain : MonoBehaviour {
	//===========================
	//		Singleton
	//===========================
	private static EndlessTerrain singletonInstance;
	static EndlessTerrain Instance() {
		if (singletonInstance == null) {
			singletonInstance = (EndlessTerrain)FindObjectOfType<EndlessTerrain>();
        }
		return singletonInstance;
    }

	public static float GetHeightFromMesh(Vector2 pos) {
		return Instance().GetHeightFromMeshInstance(pos);
    }

	public static float GetSteepnessFromMesh(Vector2 pos) {
		return Instance().GetSteepnessFromMeshInstance(pos);
    }

	public static float GetHeightFromMap(Vector2 pos) {
		return Instance().GetHeightFromMapInstance(pos);
    }

	public static Mesh GetSubMeshFromMesh(Vector2 pos, int size) {
		return Instance().GetSubMeshFromMeshInstance(pos, size);
	}

	public static Vector3[] GetQuadFromMesh(Vector2 pos) {
		return Instance().GetQuadFromMeshInstance(pos);
    }

	public static bool IsCoordInDict(Vector2 pos) {
		return Instance().IsCoordInDictInstance(pos);
    }

	public static float MaxViewDst {
		get { return Instance().maxViewDst; }
	}

	public static int ChunksVisibleInViewDst {
		get { return Instance().chunksVisibleInViewDst; }
    }


	//===================
	//		Class
	//===================

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrviewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

	public float maxViewDst;
	public static FoliagePool foliagePool;
	int chunkSize;
	public int chunksVisibleInViewDst;
	Material mapMaterial;
	Mesh.MeshDataArray dataArray;

	public Transform viewer;
	public static Vector2 viewerPosition;
	Vector2 viewerPositionOld;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	public static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

	void Start (){
		Instance();
		foliagePool = FindObjectOfType<FoliagePool>();
		mapMaterial = MapGenerator.TextureData.material;
		chunkSize = MapGenerator.mapChunkSize - 1;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
		
		UpdateVisibleChunks ();
	}

	void Update(){
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

		UpdateVisibleChunks ();
	}

	void UpdateVisibleChunks() {

		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
		//Debug.Log("Current viewed Chunk Coord =	" + currentChunkCoordX + ", " + currentChunkCoordY);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

				if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
					terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
				else
					terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, mapMaterial));
			}
		}
	}

	//======================================
	//		Helpful Static Methods
	//======================================

	/// <summary>Converts world coordinate to coordinate of chunk</summary>
	public static Vector2 WorldCoordToChunkCoord(Vector2 pos){
		int roundedX = Mathf.RoundToInt (pos.x / TerrainData.uniformScale / (MapGenerator.mapChunkSize - 1));
		int roundedY = Mathf.RoundToInt (pos.y / TerrainData.uniformScale / (MapGenerator.mapChunkSize - 1));
		return new Vector2 (roundedX, roundedY);
	}

	public static int2 WorldCoordToChunkCoordInt2(float2 pos) {
		int roundedX = Mathf.RoundToInt(pos.x / TerrainData.uniformScale / (MapGenerator.mapChunkSize - 1));
		int roundedY = Mathf.RoundToInt(pos.y / TerrainData.uniformScale / (MapGenerator.mapChunkSize - 1));
		return new int2(roundedX, roundedY);
	}

	/// <summary>Converts world coord to coord within chunk</summary>
	public static Vector2 WorldCoordToLocalCoord(Vector2 pos, bool round=false){
		Vector2 chunkCoord = WorldCoordToChunkCoord (pos);
		float x, y;
		if (round) {
			x = Mathf.RoundToInt((pos.x / TerrainData.uniformScale) - (chunkCoord.x * (MapGenerator.mapChunkSize - 1)) + ((MapGenerator.mapChunkSize - 1) / 2));
			y = (MapGenerator.mapChunkSize - 1) - Mathf.RoundToInt((pos.y / TerrainData.uniformScale) - (chunkCoord.y * (MapGenerator.mapChunkSize - 1)) + ((MapGenerator.mapChunkSize - 1) / 2));
		} else {
			x = (pos.x / TerrainData.uniformScale) - (chunkCoord.x * (MapGenerator.mapChunkSize - 1)) + ((MapGenerator.mapChunkSize - 1) / 2);
			y = (MapGenerator.mapChunkSize - 1) - ((pos.y / TerrainData.uniformScale) - (chunkCoord.y * (MapGenerator.mapChunkSize - 1)) + ((MapGenerator.mapChunkSize - 1) / 2));
		}
		return new Vector2 (x, y);
	}

	/// <summary>Converts coord in local space to world coord</summary>
	public static Vector2 LocalCoordToWorldCoord(Vector2 pos, Vector2 chunkCoord) {
		float x = (pos.x + (chunkCoord.x * (MapGenerator.mapChunkSize - 1)) - ((MapGenerator.mapChunkSize - 1) / 2)) * TerrainData.uniformScale;
		float y = ((MapGenerator.mapChunkSize - 1) - pos.y + (chunkCoord.y * (MapGenerator.mapChunkSize - 1)) - ((MapGenerator.mapChunkSize - 1) / 2)) * TerrainData.uniformScale;
		return new Vector2(x, y);
	}

	public static Vector2 LocalCoordToWorldCoord(int2 pos, float2 chunkCoord) {
		return LocalCoordToWorldCoord(pos, chunkCoord);
    }


	//=======================================
	//		Helpful instance methods
	//=======================================
	float GetHeightFromMapInstance(Vector2 pos){
		float[] heightMap = GetHeightMapFromDict (WorldCoordToChunkCoord (pos));
		Vector2 mapCoord = WorldCoordToLocalCoord (pos, true);
		float unfixedHeight = heightMap[ArrayFlatten.IndexToFlat2D((int)mapCoord.x, (int)mapCoord.y, MapGenerator.mapChunkSize)];
		return MapGenerator.MapHeightToActual (unfixedHeight);
	}

	///<summary>Returns Height via Mesh. Takes in world Coordinate</summary>
	float GetHeightFromMeshInstance(Vector2 pos) {
		TerrainChunk chunk = GetChunkFromDict(WorldCoordToChunkCoord(pos));
		Vector2 localPos = WorldCoordToLocalCoord(pos, false);

		Plane plane = PlaneGen(localPos, chunk);
		if (plane.Valid()) return plane.GetY(localPos.x, localPos.y) * TerrainData.uniformScale;
		return 0;
	}

	///<summary>Returns Height via Mesh. Takes in world Coordinate</summary>
	float GetSteepnessFromMeshInstance(Vector2 pos) {
		TerrainChunk chunk = GetChunkFromDict(WorldCoordToChunkCoord(pos));
		Vector2 localPos = WorldCoordToLocalCoord(pos, false);

		Plane plane = PlaneGen(localPos, chunk);

		if (plane.Valid()) return plane.GetSteepness();
		return 0;
	}

	///<summary>Returns 4 points of Plane. Takes in world Coordinate</summary>
	Vector3[] GetQuadFromMeshInstance(Vector2 pos) {
		
		Vector2 chunkCoord = WorldCoordToChunkCoord(pos);
		TerrainChunk chunk = GetChunkFromDict(chunkCoord);
		Vector2 localPos = WorldCoordToLocalCoord(pos, false);
		if (!chunk.meshData.hasMesh) Debug.LogWarning("Getting Quad from MeshInstance with no mesh");
		int meshSize = chunk.meshData.meshSize;
		NativeArray<float> heightMap = chunk.meshData.GetVertexHeightMap();

		Vector3 pos1, pos2, pos3, pos4;

		Vector2 pos1World = LocalCoordToWorldCoord(new Vector2((int)localPos.x, (int)localPos.y), chunkCoord);
		pos1 = new Vector3(pos1World.x, heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 0, meshSize, 4)] * TerrainData.uniformScale, pos1World.y);

		Vector2 pos2World = LocalCoordToWorldCoord(new Vector2((int)localPos.x + 1, (int)localPos.y), chunkCoord);
		pos2 = new Vector3(pos2World.x, heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 1, meshSize, 4)] * TerrainData.uniformScale, pos2World.y);

		Vector2 pos3World = LocalCoordToWorldCoord(new Vector2((int)localPos.x, (int)localPos.y + 1), chunkCoord);
		pos3 = new Vector3(pos3World.x, heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 2, meshSize, 4)] * TerrainData.uniformScale, pos3World.y);

		Vector2 pos4World = LocalCoordToWorldCoord(new Vector2((int)localPos.x + 1, (int)localPos.y + 1), chunkCoord);
		pos4 = new Vector3(pos4World.x, heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 3, meshSize, 4)] * TerrainData.uniformScale, pos4World.y);

	
		return new Vector3[] {pos1, pos2, pos3, pos4};
	}


	//======================================
	//		Mesh Update Functions
	//======================================


	Mesh GetSubMeshFromMeshInstance(Vector2 pos, int size) {
		float maxSteepness = MapGenerator.TerrainData.maxSteepness;
		int mid = (int)(size / 2);
		int vertexIndex = 0;
		if (size % 2 == 0) size++;
		int triangleIndex = 0;

		Vector3[] vertices = new Vector3[size * size * 4];
		Vector2[] uvs = new Vector2[vertices.Length];
		int[] triangles = new int[(int)(vertices.Length * 1.5f)];
		float[] heights = new float[4];

		void addQuad(float x, float y){
			Vector3[] currQuad = GetQuadFromMesh(new Vector2(x, y));

			for (int i = 0; i < 4; i++) {
				vertices[vertexIndex + i] = currQuad[i];
				uvs[vertexIndex + i] = new Vector2(vertices[vertexIndex + i].x, vertices[vertexIndex + i].z);
				heights[i] = currQuad[i].y;
			}

			int topHeightIndex = MeshGenerator.GetTopHeightIndexFromQuadHeights(heights);
			int[] neighborIndices = MeshGenerator.GetNeighborIndices(topHeightIndex);
			int oppositeIndex = MeshGenerator.GetOppositeIndex(topHeightIndex);
			heights = MeshGenerator.DesteepQuadHeights(heights, maxSteepness, topHeightIndex);

			triangles[triangleIndex] = vertexIndex + topHeightIndex;
			triangles[triangleIndex + 1] = vertexIndex + neighborIndices[0];
			triangles[triangleIndex + 2] = vertexIndex + neighborIndices[1];

			triangles[triangleIndex + 3] = vertexIndex + oppositeIndex;
			triangles[triangleIndex + 4] = vertexIndex + neighborIndices[1];
			triangles[triangleIndex + 5] = vertexIndex + neighborIndices[0];

			vertexIndex += 4;
			triangleIndex += 6;
		}

		for (int y = 0; y <= mid; y++) {
			for (int x = 0; x <= mid; x++) {
				//-x -y
				addQuad(pos.x - x * TerrainData.uniformScale, pos.y - y * TerrainData.uniformScale);
				//-x +y
				if (y != 0) addQuad(pos.x - x * TerrainData.uniformScale, pos.y + y * TerrainData.uniformScale);
				//+x -y
				if (x != 0) addQuad(pos.x + x * TerrainData.uniformScale, pos.y - y * TerrainData.uniformScale);
				//+x +y
				if (x != 0 && y != 0) addQuad(pos.x + x * TerrainData.uniformScale, pos.y + y * TerrainData.uniformScale);
			}
		}
		Mesh returnMesh = new Mesh();
		returnMesh.vertices = vertices;
		returnMesh.uv = uvs;
		returnMesh.triangles = triangles;
		return returnMesh;
	}

	/// <summary>Updates Mesh of 1 Chunk</summary>
	/// <param name="newVertices">Should be in Quad form. In world coordinates.</param>
	/// DEPRECATED
	/*
	public void UpdateSingleChunkMesh(Vector3[] newVertices, int levelOfDetail, float maxSteepness) {
		Vector2 chunkCoord = WorldCoordToChunkCoord(new Vector2(newVertices[0].x, newVertices[0].z));
		TerrainChunk chunk = GetChunkFromDict(chunkCoord);
		chunk.UpdateTerrainChunk(newVertices, levelOfDetail, maxSteepness);
	}*/


	//================================================
	//		Get from terrainchunk Dictionary
	//================================================

	bool IsCoordInDictInstance(Vector2 pos) {
		return terrainChunkDictionary.ContainsKey(pos);
	}

	///<summary>Gets map data from Map Dictionary, containing heightmap.</summary>
	float[] GetHeightMapFromDict(Vector2 pos) {
		TerrainChunk chunk = GetChunkFromDict(pos);
		if (chunk.mapDataReceived) {
			return chunk.heightMap;
		}
		else {
			Debug.LogWarning("Chunk has no map data:" + pos);
			return new float[0];
		}
	}

	///<summary>Gets TerrainChunk from Chunk Dictionary.</summary>
	TerrainChunk GetChunkFromDict(Vector2 pos) {
		if (terrainChunkDictionary.TryGetValue(pos, out TerrainChunk chunk)) {
			return chunk;
		}
		else {
			Debug.LogWarning("Chunk Not Found" + pos);
			return null;
		}
	}


	//===================================
	//	Helpers on Helpers on Helpers
	//===================================

	Plane PlaneGen(Vector2 localPos, TerrainChunk chunk) {
		if (!chunk.hasMesh) return new Plane(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
		Vector3 p, q, r;

		NativeArray<float> heightMap = chunk.meshData.GetVertexHeightMap();
		NativeArray<bool> flipMap = chunk.meshData.GetFlipMap();
		int size = chunk.meshData.meshSize;

		float height1, height2, height3;
		if (!flipMap[ArrayFlatten.IndexToFlat2D((int)localPos.x, (int)localPos.y, size)]) {
			if (localPos.x % 1 >= localPos.y % 1) {
				// 1----2
				//  \ x |
				//    \ |
				//      3

				height1 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 0, size, 4)];
				height2 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 1, size, 4)];
				height3 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 3, size, 4)];

				p = new Vector3((int)localPos.x, height1, (int)localPos.y);
				q = new Vector3((int)localPos.x + 1, height2, (int)localPos.y);
				r = new Vector3((int)localPos.x + 1, height3, (int)localPos.y + 1);
			}
			else {
				// 1
				// | \
				// | x \
				// 2----3

				height1 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 0, size, 4)];
				height2 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 2, size, 4)];
				height3 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 3, size, 4)];

				p = new Vector3((int)localPos.x, height1, (int)localPos.y);
				q = new Vector3((int)localPos.x, height2, (int)localPos.y + 1);
				r = new Vector3((int)localPos.x + 1, height3, (int)localPos.y + 1);
			}
		}
		else {
			if (localPos.x % 1 >= localPos.y % 1) {
				// 1----2
				// | x /
				// | /
				// 3

				height1 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 0, size, 4)];
				height2 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 1, size, 4)];
				height3 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 2, size, 4)];

				p = new Vector3((int)localPos.x, height1, (int)localPos.y);
				q = new Vector3((int)localPos.x + 1, height2, (int)localPos.y);
				r = new Vector3((int)localPos.x, height3, (int)localPos.y + 1);
			}
			else {
				//      1
				//    / |
				//  / x |
				// 2----3

				height1 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 1, size, 4)];
				height2 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 2, size, 4)];
				height3 = heightMap[ArrayFlatten.IndexToFlat3D((int)localPos.x, (int)localPos.y, 3, size, 4)];

				p = new Vector3((int)localPos.x + 1, height1, (int)localPos.y);
				q = new Vector3((int)localPos.x, height2, (int)localPos.y + 1);
				r = new Vector3((int)localPos.x + 1, height3, (int)localPos.y + 1);
			}
		}

		return new Plane(p, q, r);
	}


	//Our wittle stwuct all the way at the end of the page :)
	[System.Serializable]
	public struct LODInfo{
		public int lod;
		public float visibleDstThreshold;
		public bool useForCollider;
		public bool useForFoliage;
	}
}
