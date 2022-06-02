using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
//using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics;

//TerrainChunk is basically now just a collection of LODMeshes.
public class TerrainChunk {
	public Vector2 coord;
	Vector2 position;
	Bounds bounds;

	bool visible;
	public bool needsUpdate = false;

	bool useForCollider;
	bool useForNav;
	bool useForFoliage;

	public bool hasRequestedFoliage = false;
	bool hasFoliage = false;

	public bool hasRequestedMesh = false;
	public bool hasMesh;

	public MeshData meshData;
	UnityEngine.Material material;

	public bool hasCollisionMesh = false;
	public float[] heightMap;
	public bool mapDataReceived = false;

	//Entities
	EntityManager entityManager;
	Entity meshEntity;

	public TerrainChunk(Vector2 coord, int size, UnityEngine.Material material) {
		this.coord = coord;
		this.material = material;
		position = coord * size;
		bounds = new Bounds(position, Vector2.one * size);
		meshData = new MeshData(size);

		//SubMesh Entities
		entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		EntityArchetype terrainSubMeshArchetype = entityManager.CreateArchetype(
				typeof(Translation),
				typeof(LocalToWorld),
				typeof(RenderMesh),
				typeof(PhysicsCollider),
				typeof(Rotation),
				typeof(Scale),
				typeof(RenderBounds)
			);
		meshEntity = entityManager.CreateEntity(terrainSubMeshArchetype);
		entityManager.SetComponentData(meshEntity, new Translation { Value = new float3(coord.x * (MapGenerator.mapChunkSize-1) * TerrainData.uniformScale, 0, coord.y * (MapGenerator.mapChunkSize-1) * TerrainData.uniformScale) });
		entityManager.SetComponentData(meshEntity, new Rotation { Value = Quaternion.Euler(0, 0, 0)});
		entityManager.SetComponentData(meshEntity, new RenderBounds { Value = new AABB { Center = new float3(position.x, 0, position.y), Extents = new float3(size * TerrainData.uniformScale, 1000, size * TerrainData.uniformScale) } });
		entityManager.SetComponentData(meshEntity, new Scale { Value = TerrainData.uniformScale });

		entityManager.SetName(meshEntity, "terrain_chunk");

		//Debug.Log("Requesting Map Data Instance: " + coord);
		MapGenerator.RequestMapData(position, this);
	}


	//===================================
	//		Update Terrain Chunk
	//		Molto importante
	//===================================

	/// <summary>ONLY USE AFTER TO UPDATE AFTER CHUNK HAS BEEN CREATED</summary>
	/*public void UpdateTerrainChunk(Vector3[] newVertices, int levelOfDetail, float maxSteepness) {
		lodMesh.UpdateMesh(newVertices, levelOfDetail, maxSteepness, new Vector2(meshObject.transform.position.x, meshObject.transform.position.z));
	}*/

	/// <summary>Default Terrain Chunk Update</summary>
	public void UpdateTerrainChunk() {
		if (mapDataReceived) {
			if (hasMesh && needsUpdate) {
				//Apply mesh
				entityManager.SetSharedComponentData(meshEntity, new RenderMesh {
					mesh = meshData.GetMesh(),
					material = this.material
				});
				entityManager.SetComponentData(meshEntity, new RenderBounds { Value = meshData.GetMesh().bounds.ToAABB()});

				//Generate Mesh Collider
				MeshColliderWrapper meshColliderWrapper = new MeshColliderWrapper(meshData, entityManager, meshEntity);
				CreateMeshColliderJob createMeshColliderJob = new CreateMeshColliderJob {
					float3Vertices = meshColliderWrapper.float3Vertices,
					int3Triangles = meshColliderWrapper.int3Triangles,
					output = meshColliderWrapper.output,
					complete = meshColliderWrapper.returnComplete
				};

				JobHandler.AddJob(createMeshColliderJob, meshColliderWrapper);

				needsUpdate = false;
				
				//Debug.Log("Terrain Chunk Mesh updated: " + coord);
			}
			else if (!hasRequestedMesh) {
				hasRequestedMesh = true;
				MapGenerator.RequestMeshData(meshData, heightMap, position, this);
				needsUpdate = true;
			}
		}
	}

    /// <summary>Returns vertexHeightMap of lodMeshes[0]</summary>
    void OnMapDataReceived(float[] heightMap) {
		this.heightMap = heightMap;
		mapDataReceived = true;

		UpdateTerrainChunk();
	}
}