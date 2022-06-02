using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

//Main mesh Gen Job, TODO: Update other methods to jobs.
[BurstCompile]
public struct GenerateTerrainMeshJob : IJob {
	//Values In
	public float heightMultiplier;
	public float heightRound;
	public float maxSteepness;

	public NativeArray<float> heightmap;
	public NativeArray<float> heightCurve;

	//Return Values
	public NativeArray<int> returnVerticesPerLine; //Size 1

	public NativeArray<bool> returnFlipMap; //2D flattened
	public NativeArray<float> returnVertexHeightMap; //3D Flattened
	public NativeArray<int> returnVertexIndexMap; //3D Flattened

	public NativeList<float3> returnFloat3Vertices;
	public NativeList<int3> returnInt3Triangles;
	public NativeList<float2> returnFloat2UVs;

	public NativeList<Vector3> returnVertices;
	public NativeList<uint> returnTriangles;
	public NativeList<Vector2> returnUVs;

	public NativeArray<int2> navMesh;

	public NativeArray<bool> complete; //Size 1

	public void Execute() {
		//Vertices go:
		//  v0--v1
		//  |   |
		//  v2--v3

		//Necessary initializations
		int meshIncrement = 1;
		int meshSize = MapGenerator.mapChunkSize;
		float topLeftX = (meshSize - 1) / -2f;
		float topLeftZ = (meshSize - 1) / 2f;
		int verticesPerLine = meshSize - meshIncrement;
		returnVerticesPerLine[0] = verticesPerLine;
		int vertexIndex = 0;
		int vertexIndex2 = 0;

		NativeArray<float> heights = new NativeArray<float>(4, Allocator.Temp);
		NativeArray<int> neighborIndices = new NativeArray<int>(2, Allocator.Temp);
		NativeArray<int> vertexIndices = new NativeArray<int>(4, Allocator.Temp);
		//Debug.LogWarning("Starting Mesh Generation");
		//Increment over every height in heightmap and do lots of maths :)
		for (int y = 0; y < verticesPerLine; y += meshIncrement) {
			for (int x = 0; x < verticesPerLine; x += meshIncrement) {

				//Find four heights for current quad based on the maxHeight and maximum steepness
				int temp0 = (int)(heightmap[ArrayFlatten.IndexToFlat2D(x, y, MapGenerator.mapChunkSize)] * 500);
				int temp1 = (int)(heightmap[ArrayFlatten.IndexToFlat2D(x + meshIncrement, y, MapGenerator.mapChunkSize)] * 500);
				int temp2 = (int)(heightmap[ArrayFlatten.IndexToFlat2D(x, y + meshIncrement, MapGenerator.mapChunkSize)] * 500);
				int temp3 = (int)(heightmap[ArrayFlatten.IndexToFlat2D(x + meshIncrement, y + meshIncrement, MapGenerator.mapChunkSize)] * 500);
				temp0 = temp0 > 999 ? 999 : temp0;
				temp1 = temp1 > 999 ? 999 : temp1;
				temp2 = temp2 > 999 ? 999 : temp2;
				temp3 = temp3 > 999 ? 999 : temp3;

				heights[0] = math.round((heightCurve[temp0] * heightMultiplier) / heightRound) * heightRound;
				heights[1] = math.round((heightCurve[temp1] * heightMultiplier) / heightRound) * heightRound;
				heights[2] = math.round((heightCurve[temp2] * heightMultiplier) / heightRound) * heightRound;
				heights[3] = math.round((heightCurve[temp3] * heightMultiplier) / heightRound) * heightRound;
				//heights[0] = math.round(heightmap[ArrayFlatten.IndexToFlat2D(x, y, MapGenerator.mapChunkSize)] * heightMultiplier / heightRound) * heightRound;
				//heights[1] = math.round(heightmap[ArrayFlatten.IndexToFlat2D(x + meshIncrement, y, MapGenerator.mapChunkSize)] * heightMultiplier / heightRound) * heightRound;
				//heights[2] = math.round(heightmap[ArrayFlatten.IndexToFlat2D(x, y + meshIncrement, MapGenerator.mapChunkSize)] * heightMultiplier / heightRound) * heightRound;
				//heights[3] = math.round(heightmap[ArrayFlatten.IndexToFlat2D(x + meshIncrement, y + meshIncrement, MapGenerator.mapChunkSize)] * heightMultiplier / heightRound) * heightRound;

				int topHeightIndex = MeshGenerator.GetTopHeightIndexFromQuadHeights(heights);
				
				MeshGenerator.GetNeighborIndices(neighborIndices, topHeightIndex);
				int oppositeIndex = MeshGenerator.GetOppositeIndex(topHeightIndex);
				heights = MeshGenerator.DesteepQuadHeights(heights, maxSteepness, topHeightIndex);
				returnFlipMap[ArrayFlatten.IndexToFlat2D(x, y, verticesPerLine)] = topHeightIndex == 2 || topHeightIndex == 1;

				//Remove Redundant vertices
				int vertexIndexAdder = 0;
				vertexIndices[0] = 0; vertexIndices[1] = 1; vertexIndices[2] = 2; vertexIndices[3] = 3;

				void ShiftDownVertexIndices(int i) {
					for (int j = i + 1; j < 4; j++) {
						vertexIndices[j] -= 1;
					}
				}

				

				if (x != 0 && y != 0) {
					//v0
					if (heights[0] == returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 1, verticesPerLine, 4)]) {
						vertexIndices[0] = returnVertexIndexMap[ArrayFlatten.IndexToFlat3D((x - 1), y, 1, verticesPerLine, 4)] - vertexIndex;
						ShiftDownVertexIndices(0);
					}
					else if (heights[0] == returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 2, verticesPerLine, 4)]) {
						vertexIndices[0] = returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 2, verticesPerLine, 4)] - vertexIndex;
						ShiftDownVertexIndices(0);
					}
					else if (heights[0] == returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x - 1, y - 1, 3, verticesPerLine, 4)]) {
						vertexIndices[0] = returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y - 1, 3, verticesPerLine, 4)] - vertexIndex;
						ShiftDownVertexIndices(0);
					}
					else {
						returnFloat3Vertices.Add(new float3(topLeftX + x, heights[0], topLeftZ - y) * TerrainData.uniformScale);
						returnFloat2UVs.Add(new float2(x / (float)meshSize, y / (float)meshSize));
						vertexIndexAdder += 1;
					}

					//v1
					if (heights[1] == returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 3, verticesPerLine, 4)]) {
						vertexIndices[1] = returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 3, verticesPerLine, 4)] - vertexIndex;
						ShiftDownVertexIndices(1);
					}
					else {
						returnFloat3Vertices.Add(new float3(topLeftX + x + meshIncrement, heights[1], topLeftZ - y) * TerrainData.uniformScale);
						returnFloat2UVs.Add(new float2((x + meshIncrement) / (float)meshSize, y / (float)meshSize));
						vertexIndexAdder += 1;
					}

					//v2
					if (heights[2] == returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 3, verticesPerLine, 4)]) {
						vertexIndices[2] = returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 3, verticesPerLine, 4)] - vertexIndex;
						ShiftDownVertexIndices(2);
					}
					else {
						returnFloat3Vertices.Add(new float3(topLeftX + x, heights[2], topLeftZ - y - meshIncrement) * TerrainData.uniformScale);
						returnFloat2UVs.Add(new float2(x / (float)meshSize, (y + meshIncrement) / (float)meshSize));
						vertexIndexAdder += 1;
					}
				}

				else {
					returnFloat3Vertices.Add(new float3(topLeftX + x, heights[0], topLeftZ - y) * TerrainData.uniformScale);
					returnFloat2UVs.Add(new float2(x / (float)meshSize, y / (float)meshSize));

					returnFloat3Vertices.Add(new float3(topLeftX + x + meshIncrement, heights[1], topLeftZ - y) * TerrainData.uniformScale);
					returnFloat2UVs.Add(new float2((x + meshIncrement) / (float)meshSize, y / (float)meshSize));

					returnFloat3Vertices.Add(new float3(topLeftX + x, heights[2], topLeftZ - y - meshIncrement) * TerrainData.uniformScale);
					returnFloat2UVs.Add(new float2(x / (float)meshSize, (y + meshIncrement) / (float)meshSize));
					vertexIndexAdder += 3;
				}

				//v3
				returnFloat2UVs.Add(new float2((x + meshIncrement) / (float)meshSize, (y + meshIncrement) / (float)meshSize));
				returnFloat3Vertices.Add(new float3(topLeftX + x + meshIncrement, heights[3], topLeftZ - y - meshIncrement) * TerrainData.uniformScale);
				vertexIndexAdder += 1;

				//Set vertexHeightMap (A handy matrix in meshData for accessing these heights easier).
				returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x, y, 0, verticesPerLine, 4)] = heights[0];
				returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x, y, 1, verticesPerLine, 4)] = heights[1];
				returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x, y, 2, verticesPerLine, 4)] = heights[2];
				returnVertexHeightMap[ArrayFlatten.IndexToFlat3D(x, y, 3, verticesPerLine, 4)] = heights[3];

				//Set vertexIndex map
				returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, verticesPerLine, 4)] = vertexIndex + vertexIndices[0];
				returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 1, verticesPerLine, 4)] = vertexIndex + vertexIndices[1];
				returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 2, verticesPerLine, 4)] = vertexIndex + vertexIndices[2];
				returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 3, verticesPerLine, 4)] = vertexIndex + vertexIndices[3];

				//Set Triangles
				if (x < meshSize - 1 && y < meshSize - 1) {
					returnInt3Triangles.Add(new int3(
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, topHeightIndex, verticesPerLine, 4)],
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[0], verticesPerLine, 4)],
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[1], verticesPerLine, 4)]));
					

					returnInt3Triangles.Add(new int3(
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, oppositeIndex, verticesPerLine, 4)],
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[1], verticesPerLine, 4)],
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[0], verticesPerLine, 4)]));

					MeshGenerator.AddTriangleNav(
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, topHeightIndex, verticesPerLine, 4)], 
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[0], verticesPerLine, 4)], 
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[1], verticesPerLine, 4)],
						x, y, navMesh, returnFloat3Vertices);

					MeshGenerator.AddTriangleNav(
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, oppositeIndex, verticesPerLine, 4)], 
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[1], verticesPerLine, 4)], 
						returnVertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, neighborIndices[0], verticesPerLine, 4)],
						x, y, navMesh, returnFloat3Vertices);
				}

				//Set rendermesh Verts and triangles:
				returnVertices.Add(new Vector3(topLeftX + x, heights[0], topLeftZ - y));
				returnUVs.Add(new Vector2(x / (float)meshSize, y / (float)meshSize));
				returnVertices.Add(new Vector3(topLeftX + x + meshIncrement, heights[1], topLeftZ - y));
				returnUVs.Add(new Vector2((x + meshIncrement) / (float)meshSize, y / (float)meshSize));
				returnVertices.Add(new Vector3(topLeftX + x, heights[2], topLeftZ - y - meshIncrement));
				returnUVs.Add(new Vector2(x / (float)meshSize, (y + meshIncrement) / (float)meshSize));
				returnVertices.Add(new Vector3(topLeftX + x + meshIncrement, heights[3], topLeftZ - y - meshIncrement));
				returnUVs.Add(new Vector2((x + meshIncrement) / (float)meshSize, (y + meshIncrement) / (float)meshSize));

				returnTriangles.Add((uint)(vertexIndex2 + topHeightIndex));
				returnTriangles.Add((uint)(vertexIndex2 + neighborIndices[0]));
				returnTriangles.Add((uint)(vertexIndex2 + neighborIndices[1]));

				returnTriangles.Add((uint)(vertexIndex2 + oppositeIndex));
				returnTriangles.Add((uint)(vertexIndex2 + neighborIndices[1]));
				returnTriangles.Add((uint)(vertexIndex2 + neighborIndices[0]));

				//Generates Cliffs
				if (y != 0 && x != 0) {
					MeshGenerator.GenerateCliffs(returnFloat3Vertices, returnVertexIndexMap, returnInt3Triangles, returnTriangles, verticesPerLine, x, y, vertexIndex2);
				}
				vertexIndex2 += 4;
				vertexIndex += vertexIndexAdder;
			}
		}

		complete[0] = true;
		//Debug.LogWarning("Finished Making Mesh");
		heights.Dispose();
		neighborIndices.Dispose();
		vertexIndices.Dispose();
	}
	
}


/// <summary>A collection of static methods necessary for generating 3D terrain from a 2D map</summary>
public static class MeshGenerator {

	//============================================
	//			Main Generation Methods
	//============================================

	public static void GenerateTerrainMesh(MeshDataWrapper meshDataWrapper, float heightMultiplier, float heightRound, float maxSteepness) {
		

		GenerateTerrainMeshJob generateTerrainMeshJob = new GenerateTerrainMeshJob {
			heightMultiplier = heightMultiplier,
			heightRound = heightRound,
			maxSteepness = maxSteepness,

			heightmap = meshDataWrapper.nativeHeightMap,
			heightCurve = meshDataWrapper.nativeHeightCurve,

			returnVerticesPerLine = meshDataWrapper.returnVerticesPerLine,

			returnFlipMap = meshDataWrapper.returnFlipMap,
			returnVertexHeightMap = meshDataWrapper.returnVertexHeightMap,
			returnVertexIndexMap = meshDataWrapper.returnVertexIndexMap,

			returnFloat3Vertices = meshDataWrapper.returnFloat3Vertices,
			returnInt3Triangles = meshDataWrapper.returnInt3Triangles,
			returnFloat2UVs = meshDataWrapper.returnFloat2UVs,
			returnVertices = meshDataWrapper.returnVertices,
			returnTriangles = meshDataWrapper.returnTriangles,
			returnUVs = meshDataWrapper.returnUVs,

			complete = meshDataWrapper.returnComplete,
		};

		JobHandler.AddJob(generateTerrainMeshJob, meshDataWrapper);
    }

	//==================================
	//			Cliff Methods
	//===================================

	///<summary>Generates cliffs for north and west edges</summary>
	public static void GenerateCliffs(NativeList<float3> verticesList, NativeArray<int> vertexIndexMap, NativeList<int3> int3TrianglesList, NativeList<uint> trianglesList, NativeArray<int2> navMesh, int size, int x, int y, int vertexIndex) {
		NativeList<float3> newVertices = new NativeList<float3>(Allocator.Temp);

		//		   v-4VpL--v-4VpL+1
		//			  |    |
		//           3v1--3v2
		//  v-4--2v1 1v1--1v2
		//   |    |   |   |
		//	v-2--2v2 1v3--v3

		float3 oneVertex1 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)]];
		float3 oneVertex2 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 1, size, 4)]];
		float3 oneVertex3 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 2, size, 4)]];

		float3 twoVertex1 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 1, size, 4)]];
		float3 twoVertex2 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 3, size, 4)]];

		float3 threeVertex1 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 2, size, 4)]];
		float3 threeVertex2 = verticesList[vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 3, size, 4)]];

		//For West Edge
		ListUnevenVertices(newVertices, oneVertex1, twoVertex1);
		ListUnevenVertices(newVertices, oneVertex3, twoVertex2);

		if (newVertices.Length >= 2) {
			if (newVertices.Length >= 3) {

				int3TrianglesList.Add(new int3(
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 1, size, 4)], 
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)], 
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 3, size, 4)]));
				trianglesList.Add((uint)vertexIndex - 3);
				trianglesList.Add((uint)vertexIndex);
				trianglesList.Add((uint)vertexIndex - 1);
				
				int3TrianglesList.Add(new int3(
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 3, size, 4)],
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)],
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 2, size, 4)]));
				trianglesList.Add((uint)vertexIndex - 1);
				trianglesList.Add((uint)vertexIndex);
				trianglesList.Add((uint)vertexIndex + 2);
			}
			else {
				if (newVertices[0].Equals(oneVertex1)) {
					int3TrianglesList.Add(new int3(
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 1, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 2, size, 4)]));
					trianglesList.Add((uint)vertexIndex - 3);
					trianglesList.Add((uint)vertexIndex);
					trianglesList.Add((uint)vertexIndex + 2);
				}
				else {
					int3TrianglesList.Add(new int3(
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x - 1, y, 3, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 2, size, 4)]));
					trianglesList.Add((uint)vertexIndex - 1);
					trianglesList.Add((uint)vertexIndex);
					trianglesList.Add((uint)vertexIndex + 2);
				}
			}
			NavMesh.SetEast(x, y, navMesh, 0);
		}
		newVertices.Clear();

		//For North Edge
		ListUnevenVertices(newVertices, threeVertex1, oneVertex1);
		ListUnevenVertices(newVertices, threeVertex2, oneVertex2);

		if (newVertices.Length >= 2) {
			if (newVertices.Length >= 3) {
				int3TrianglesList.Add(new int3(
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 3, size, 4)],
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 1, size, 4)],
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 2, size, 4)]));
				trianglesList.Add((uint)(vertexIndex - (size - 1) * 4 + 3));
				trianglesList.Add((uint)vertexIndex + 1);
				trianglesList.Add((uint)(vertexIndex - (size - 1) * 4 + 2));

				int3TrianglesList.Add(new int3(
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 2, size, 4)],
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 1, size, 4)],
					vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)]));
				trianglesList.Add((uint)(vertexIndex - (size - 1) * 4 + 2));
				trianglesList.Add((uint)vertexIndex + 1);
				trianglesList.Add((uint)vertexIndex);
			}
			else {
				if (newVertices[0].Equals(threeVertex1)) {
					int3TrianglesList.Add(new int3(
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 2, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 1, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)]));
					trianglesList.Add((uint)(vertexIndex - (size - 1) * 4 + 2));
					trianglesList.Add((uint)vertexIndex + 1);
					trianglesList.Add((uint)vertexIndex);
				}
				else {
					int3TrianglesList.Add(new int3(
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y - 1, 3, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 1, size, 4)],
						vertexIndexMap[ArrayFlatten.IndexToFlat3D(x, y, 0, size, 4)]));
					trianglesList.Add((uint)(vertexIndex - (size - 1) * 4 + 3));
					trianglesList.Add((uint)vertexIndex + 1);
					trianglesList.Add((uint)vertexIndex);
				}
			}
			NavMesh.SetNorth(x, y, navMesh, 0);
		}
		newVertices.Dispose();
	}


	//==============================================
	//			Helper Methods
	//================================================

	//Returns two indices that are NOT opposite
	/// <param name="returnIndices">NativeArray of size 2</param>
	/// <param name="index"></param>
	public static void GetNeighborIndices(NativeArray<int> returnIndices, int index) {
		returnIndices[0] = 1; returnIndices[1] = 2;
		if (index == 1) {
			returnIndices[0] = 3; returnIndices[1] = 0;
		} else if (index == 2) {
			returnIndices[0] = 0; returnIndices[1] = 3;
		} else if (index == 3) {
			returnIndices[0] = 2; returnIndices[1] = 1;
		}
	}
	public static int[] GetNeighborIndices(int index) {
		int[] returnIndices = new int[2];
		returnIndices[0] = 1; returnIndices[1] = 2;
		if (index == 1) {
			returnIndices[0] = 3; returnIndices[1] = 0;
		}
		else if (index == 2) {
			returnIndices[0] = 0; returnIndices[1] = 3;
		}
		else if (index == 3) {
			returnIndices[0] = 2; returnIndices[1] = 1;
		}
		return returnIndices;
	}

	//Returns index that IS opposite
	public static int GetOppositeIndex(int index) {
		return 3 - index;
	}

	//Find the quad with the highest height
	public static int GetTopHeightIndexFromQuadHeights(NativeArray<float> heights) {
		int topHeightIndex = 0;
		NativeList<int> topHeightIndices = new NativeList<int>(Allocator.Temp);
		for (int i = 0; i < heights.Length; i++) {
			if (heights[i] == heights[topHeightIndex]) {
				topHeightIndices.Add(i);
			} else if (heights[i] > heights[topHeightIndex]) {
				topHeightIndex = i;
				topHeightIndices.Clear();
				topHeightIndices.Add(i);
			}
		}
		//Cases for 3 heights the same
		if (topHeightIndices.Length == 3) {
			if (topHeightIndices[0] == 1) {
				topHeightIndex = 3;
			} else if (topHeightIndices[0] == 0 && topHeightIndices[1] == 1) {
				topHeightIndex = 0;
			} else {
				topHeightIndex = topHeightIndices[1];
			}
		}
		topHeightIndices.Dispose();
		return topHeightIndex;
	}

	public static int GetTopHeightIndexFromQuadHeights(float[] heights) {
		int topHeightIndex = 0;
		List<int> topHeightIndices = new List<int>();
		for (int i = 0; i < heights.Length; i++) {
			if (heights[i] == heights[topHeightIndex]) {
				topHeightIndices.Add(i);
			}
			else if (heights[i] > heights[topHeightIndex]) {
				topHeightIndex = i;
				topHeightIndices.Clear();
				topHeightIndices.Add(i);
			}
		}
		//Cases for 3 heights the same
		if (topHeightIndices.Count == 3) {
			if (topHeightIndices[0] == 1) {
				topHeightIndex = 3;
			}
			else if (topHeightIndices[0] == 0 && topHeightIndices[1] == 1) {
				topHeightIndex = 0;
			}
			else {
				topHeightIndex = topHeightIndices[1];
			}
		}
		return topHeightIndex;
	}

	///<summary>Takes in heightmap of quad and returns it desteeped based on maxSteepness and topHeight</summary>
	public static NativeArray<float> DesteepQuadHeights(NativeArray<float> heights, float maxSteepness, int topHeightIndex) {
		//Determine what medheight should be: First checks for majority, then checks if all vertices are different and makes middle 2 the same, makes that medheight. 
		//Finally will just make medheight max if other two cases fail.
		NativeArray<int> neighborIndices = new NativeArray<int>(2, Allocator.Temp);
		GetNeighborIndices(neighborIndices, topHeightIndex);
		int oppositeIndex = GetOppositeIndex(topHeightIndex);
		int lowestNeighborIndex = 0;
		for (int i = 0; i < neighborIndices.Length; i++) {
			float difference = heights[topHeightIndex] - heights[neighborIndices[i]];
			if (math.abs(difference) > maxSteepness) {
				heights[neighborIndices[i]] = heights[topHeightIndex] - maxSteepness;
			}
			if (heights[neighborIndices[lowestNeighborIndex]] > heights[neighborIndices[i]]) {
				lowestNeighborIndex = i;
			}
		}
		if (heights[oppositeIndex] < heights[neighborIndices[1 - lowestNeighborIndex]] - maxSteepness) {
			heights[oppositeIndex] = heights[neighborIndices[1 - lowestNeighborIndex]] - maxSteepness;
		}
		neighborIndices.Dispose();
		return heights;
	}
	public static float[] DesteepQuadHeights(float[] heights, float maxSteepness, int topHeightIndex) {
		//Determine what medheight should be: First checks for majority, then checks if all vertices are different and makes middle 2 the same, makes that medheight. 
		//Finally will just make medheight max if other two cases fail.
		int[] neighborIndices = GetNeighborIndices(topHeightIndex);
		int oppositeIndex = GetOppositeIndex(topHeightIndex);
		int lowestNeighborIndex = 0;
		for (int i = 0; i < neighborIndices.Length; i++) {
			float difference = heights[topHeightIndex] - heights[neighborIndices[i]];
			if (Mathf.Abs(difference) > maxSteepness) {
				heights[neighborIndices[i]] = heights[topHeightIndex] - maxSteepness;
			}
			if (heights[neighborIndices[lowestNeighborIndex]] > heights[neighborIndices[i]]) {
				lowestNeighborIndex = i;
			}
		}
		if (heights[oppositeIndex] < heights[neighborIndices[1 - lowestNeighborIndex]] - maxSteepness) {
			heights[oppositeIndex] = heights[neighborIndices[1 - lowestNeighborIndex]] - maxSteepness;
		}
		return heights;
	}

	static void ListUnevenVertices(NativeList<float3> returnVertices, float3 vertex1, float3 vertex2) {
		if (vertex1.x != vertex2.x) {
			Debug.LogWarning("X Mismatch");
		}
		if (vertex1.z != vertex2.z) {
			Debug.LogWarning("Z Mismatch");
		}

		if (vertex1.y != vertex2.y) {
			returnVertices.Add(vertex1);
			returnVertices.Add(vertex2);
		}
	}

	public static void AddTriangleNav(int a, int b, int c, int x, int y, NativeArray<int2> navMesh, NativeList<float3> float3Vertices) {
		float steepness = GetTriangleSteepness(a, b, c, float3Vertices);

		//Weights based on steepness
		//If you want to change steepness' weight, this is the place to do it
		int weight;
		if (steepness == 0) {
			weight = 1;
		}
		else if (steepness < 45) {
			weight = 64;
		}
		else if (steepness < 50) {
			weight = 32;
		}
		else {
			weight = 8;
		}


		// ----0----
		// |       |
		// 1       2
		// |       |
		// ----3----
		int[] sides = new int[2];
		if (a % 4 == 0) {
			sides[0] = 0;
			sides[1] = 1;
		}
		else if (a % 4 == 1) {
			sides[0] = 0;
			sides[1] = 2;
		}
		else if (a % 4 == 2) {
			sides[0] = 1;
			sides[1] = 3;
		}
		else if (a % 4 == 3) {
			sides[0] = 2;
			sides[1] = 3;
		}

		foreach (int side in sides) {
			if (side == 0) {
				NavMesh.AddNorth(x, y, navMesh, weight);
				
			}
			else if (side == 1) {
				NavMesh.AddWest(x, y, navMesh, weight);
			}
			else if (side == 2) {
				NavMesh.AddEast(x, y, navMesh, weight);
			}
			else if (side == 3) {
				NavMesh.AddSouth(x, y, navMesh, weight);
			}
		}
	}

	static float GetTriangleSteepness(int a, int b, int c, NativeList<float3> float3Vertices) {
		Vector3 vertex1 = float3Vertices[a];
		Vector3 vertex2 = float3Vertices[b];
		Vector3 vertex3 = float3Vertices[c];
		Vector3 cross = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).normalized;
		return Mathf.Abs(Vector3.Angle(new Vector3(cross.x, 0, cross.z), cross));
	}

	static int GetTriangleOrientation(int a, int b, int c, NativeList<float3> float3Vertices) {
		Vector3 vertex1 = float3Vertices[a];
		Vector3 vertex2 = float3Vertices[b];
		Vector3 vertex3 = float3Vertices[c];
		Vector3 cross = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1).normalized;
		float horizonAngle = Mathf.Abs(Vector3.Angle(new Vector3(cross.x, 0, cross.z), cross));

		//Fuck yo switch statement
		if (cross.x == 0 && cross.z == 0) return 0;
		else if (cross.x == 0 && horizonAngle > 45) return 1;
		else if (cross.z == 0 && horizonAngle > 45) return 2;
		else if (((cross.x > 0 && cross.z > 0) || (cross.x < 0 && cross.z < 0)) && horizonAngle >= 45) return 3;
		else if (((cross.x > 0 && cross.z < 0) || (cross.x > 0 && cross.z < 0)) && horizonAngle >= 45) return 4;
		else if (cross.x == 0) return 5;
		else if (cross.z == 0) return 6;
		else if ((cross.x > 0 && cross.z > 0) || (cross.x < 0 && cross.z < 0)) return 7;
		else if ((cross.x > 0 && cross.z < 0) || (cross.x < 0 && cross.z > 0)) return 8;
		else return 0;
	}

	static int GetTotalTriangles(List<int[]>[] triangleList) {
		int total = 0;
		foreach (List<int[]> triangles in triangleList) {
			total += triangles.Count * 3;
		}
		return total;
	}
}