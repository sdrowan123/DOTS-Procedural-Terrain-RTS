using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using UnityEditor.VersionControl;
using UnityEngine.AI;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public class MapGenerator : MonoBehaviour {
	//===========================
	//		Singleton
	//===========================
	private static MapGenerator singletonInstance;
	public static MapGenerator Instance() {
		if (singletonInstance == null) {
			singletonInstance = (MapGenerator)FindObjectOfType<MapGenerator>();
		}
		return singletonInstance;
	}

	public static TerrainData TerrainData {
        get {return Instance().terrainData;}
    }

	public static NoiseData NoiseData {
		get { return Instance().noiseData; }
    }

	public static TextureData TextureData {
        get { return Instance().textureData; }
    }

	public static void GenerateHeightMap(Vector2 center, HeightMapWrapper heightMapWrapper) {
		
		Instance().GenerateHeightMapInstance(center, heightMapWrapper);
    }

	public static float MapHeightToActual(float mapHeight) {
		return Instance().MapHeightToActualInstance(mapHeight);
    }

	public static void RequestMapData(Vector2 center, TerrainChunk terrainChunk) {
		Instance().RequestMapDataInstance(center, terrainChunk);
    }

	public static void RequestMeshData(MeshData meshData, float[] heightMap, Vector2 position, TerrainChunk terrainChunk) {
		Instance().RequestMeshDataInstance(meshData, heightMap, TerrainData.meshHeightMultiplier, TerrainData.meshHeightCurveRounded, TerrainData.heightRound, TerrainData.maxSteepness, terrainChunk);
    }

	public static void RequestFoliageData(float[,] heightMap, Vector2 position, GameObject parent, FoliagePool foliagePool, Action<FoliageData> callback) {
		//Instance().threadManager.RequestFoliageData(heightMap, position, parent, foliagePool, callback);
    }

	//=============================
	//			Class
	//=============================
	//Templates
	public TerrainData terrainData;
	public NoiseData noiseData;
	public TextureData textureData;

	//Other Public variables
	public enum DrawMode { NoiseMap, DrawMesh, FalloffMap }
	public DrawMode drawMode;
	public Material terrainMaterial;
	public bool autoUpdate;
	public bool generateFoliage;
	[Range(0,6)]
	public int editorPreviewLOD;

	float[,] falloffMap;

	public static readonly int mapChunkSize = 97;

	void Awake(){
		Instance();
	}

	float MapHeightToActualInstance(float mapHeight){
		return (Mathf.Round((terrainData.meshHeightCurve.Evaluate (mapHeight) * terrainData.meshHeightMultiplier)/(terrainData.heightRound)) * (terrainData.heightRound)) * TerrainData.uniformScale;
	}


	//==============================
	//		Request MapData
	//==============================

	//Coroutines are not strictly necessary, but it's the way I started doing it, so it's the way I'll continue doing it.
	void RequestMapDataInstance(Vector2 center, TerrainChunk terrainChunk) {
		HeightMapWrapper heightMapWrapper = new HeightMapWrapper();
		
		GenerateHeightMap(center, heightMapWrapper);
		StartCoroutine(RequestMapDataCoroutine(heightMapWrapper, terrainChunk));
    }

	IEnumerator RequestMapDataCoroutine(HeightMapWrapper heightMapWrapper,  TerrainChunk terrainChunk) {
		
		while (!heightMapWrapper.complete) { yield return new WaitForEndOfFrame(); }
		//Debug.Log("Map Data Received");
		//Callback
		terrainChunk.heightMap = heightMapWrapper.returnValue;
		terrainChunk.mapDataReceived = true;
	}

	void GenerateHeightMapInstance(Vector2 center, HeightMapWrapper heightMapWrapper){

		GenerateHeightMapJob job = new GenerateHeightMapJob {
			center = center,
			noiseSeed = noiseData.seed,
			noiseScale = noiseData.noiseScale,
			noiseOctaves = noiseData.octaves,
			noisePersistance = noiseData.persistance,
			noiseLacunarity = noiseData.lacunarity,
			noiseOffset = noiseData.offset,
			normalizeLocal = noiseData.normalizeLocal,

			returnArray = heightMapWrapper.returnArray,
			returnComplete = heightMapWrapper.returnComplete,
		};

		JobHandler.AddJob(job, heightMapWrapper);
	}


	//================================
	//		Request MeshData
	//================================

	void RequestMeshDataInstance(MeshData meshData, float[] heightmap, float heightMultiplier, float[] roundedMeshHeightCurve, float heightRound, float maxSteepness, TerrainChunk terrainChunk) {
		MeshDataWrapper meshDataWrapper = new MeshDataWrapper(meshData, heightmap, roundedMeshHeightCurve);
		MeshGenerator.GenerateTerrainMesh(meshDataWrapper, heightMultiplier, heightRound, maxSteepness);
		StartCoroutine(RequestMeshDataCoroutine(meshDataWrapper, terrainChunk));
	}

	IEnumerator RequestMeshDataCoroutine(MeshDataWrapper meshDataWrapper, TerrainChunk terrainChunk) {
		//Debug.Log("Requesting Mesh Data");
		while (!meshDataWrapper.complete) yield return new WaitForEndOfFrame();
		//Debug.Log("Mesh Data Received");
		//Callback
		terrainChunk.meshData = meshDataWrapper.returnValue;
		terrainChunk.hasMesh = true;
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial(terrainMaterial);
	}
	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditorInstance();
		}
	}

	void DrawMapInEditorInstance() {
		textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight, terrainData.heightRound);
		//float[,] heightMap = GenerateHeightMapInstance(Vector2.zero);

		//MapDisplay display = FindObjectOfType<MapDisplay>();
		//if (drawMode == DrawMode.NoiseMap)
			//display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
		//else if (drawMode == DrawMode.FalloffMap)
			//display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
	}

	void OnValidate() {
		if (terrainData != null) {
			terrainData.OnValuesUpdated -= OnValuesUpdated;
			terrainData.OnValuesUpdated += OnValuesUpdated;
		}

		if (noiseData != null) {
			noiseData.OnValuesUpdated -= OnValuesUpdated;
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}

		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated;
			textureData.OnValuesUpdated += OnTextureValuesUpdated;
		}

		if (editorPreviewLOD > 4) {
			editorPreviewLOD = 6;
		}

		falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
	}
}


//================================
//	Generate Height Map Job
//================================

[BurstCompile]
public struct GenerateHeightMapJob : IJob{
	public float2 center;
	public int noiseSeed;
	public float noiseScale;
	public int noiseOctaves;
	public float noisePersistance;
	public float noiseLacunarity;
	public float2 noiseOffset;
	public bool normalizeLocal;

	public NativeArray<float> returnArray;
	public NativeArray<bool> returnComplete;

	public void Execute() {
		//Debug.Log("Starting Height Map Generation");
		Noise.GenerateNoiseMap(returnArray, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize, noiseSeed, noiseScale, noiseOctaves, 
													noisePersistance, noiseLacunarity, center + noiseOffset, normalizeLocal);
		//Debug.Log("Heightmap Generation Complete");
		//Debug.Log("Mid-point return array: " + returnArray[120]);
		returnComplete[0] = true;
	}
}