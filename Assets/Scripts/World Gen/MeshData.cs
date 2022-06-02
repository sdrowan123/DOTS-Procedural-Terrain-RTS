using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;

//This used to be a struct which is why all are public, should fix this at some point
public class MeshData{

	public bool creatingMesh;
	public bool hasMesh;
	public int meshSize;

	public int verticesPerLine;

	public NativeArray<float> vertexHeightMap; //Easy way to keep track of height of vertices
	public NativeArray<int> vertexIndexMap; //Since we are removing redundant vertices, we can keep track of where everything is here.
	public NativeArray<bool> flipMap; //Used to determine if triangles are normal or inverted

	public NativeList<float3> float3Vertices; //NOT INITIALIZED, MUST BE INITIALIZED BEFORE ASSIGNEMNT
	public NativeList<int3> int3Triangles;    // ^^^
	public NativeList<float2> float2UVs;      // ^^^
	public NativeList<Vector3> vertices;		// ^^^
	public NativeList<uint> triangles;			// ^^^
	public NativeList<Vector2> uvs;         // ^^^

	public NativeArray<int2> navMesh;

	Mesh mesh;

	//positive z is north

	//			   North:1
	//	 NorthWest:4	NorthEast:3
	//West:2		Up:0			East:2
	//	 SouthWest:3    SouthEast:4
	//			   South:1

	//Slopes +4, light then regular slopes

	public MeshData(int meshSize) {
		creatingMesh = false;
		hasMesh = false;
		verticesPerLine = 0;
		this.meshSize = meshSize;
		mesh = new Mesh();
	}

	public void CreateMesh() {
		var dataArray = Mesh.AllocateWritableMeshData(1);
		var data = dataArray[0];
		data.SetVertexBufferParams(vertices.Length, 
			new VertexAttributeDescriptor(VertexAttribute.Position),
			new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1));
		data.GetVertexData<Vector3>().CopyFrom(vertices);

		data.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
		data.GetIndexData<uint>().CopyFrom(triangles);

		data.subMeshCount = 1;
		data.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length));
		Mesh.ApplyAndDisposeWritableMeshData(dataArray, mesh);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	//=========================================
	//			Nav Methods
	//========================================


	//==================================
	//			Getter Methods
	//===================================

	public Mesh GetMesh() {
		return mesh;
	}

	public NativeArray<int2> GetNavMesh() {
		return navMesh;
	}

	public NativeArray<float> GetVertexHeightMap() {
		return vertexHeightMap;
	}

	public NativeArray<bool> GetFlipMap() {
		return flipMap;
	}
}
