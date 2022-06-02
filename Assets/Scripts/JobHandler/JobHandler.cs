using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
//using Unity.Physics;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;

//This is the weirdest code I've ever written, I hate ECS
//Note: Removeat is very slow, might want to rethink that
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class JobHandler : MonoBehaviour
{
    public float maxChunkGenPerFrame;
    int currFrame = 0;
    int nextFrame = 0;

    private static JobHandler singletonInstance;
    Queue<MeshDataJobWrapper> meshDataJobQueue;
    Queue<HeightMapJobWrapper> heightMapJobQueue;
    Queue<MeshColliderJobWrapper> meshColliderJobQueue;

    List<MeshDataJobHandleWrapper> meshDataJobHandleList;
    List<HeightMapJobHandleWrapper> heightMapJobHandleList;
    List<MeshColliderJobHandleWrapper> meshColliderJobHandleList;

    NativeList<JobHandle> jobHandleList;

    static JobHandler Instance() {
        if (singletonInstance == null) {
            singletonInstance = (JobHandler)FindObjectOfType<JobHandler>();
        }
        return singletonInstance;
    }

    public static int MeshDataJobsQueued { get { return Instance().meshDataJobHandleList.Count; } }
    public static int HeightMapJobsQueued { get { return Instance().heightMapJobHandleList.Count; } }
    public static int MeshColliderJobsQueued { get { return Instance().meshColliderJobHandleList.Count; } }
    public static float MaxChunkGenPerFrame { get { return Instance().maxChunkGenPerFrame; } }
    public static void AddJob(GenerateTerrainMeshJob job, MeshDataWrapper meshDataWrapper) { Instance().AddJobInstance(job, meshDataWrapper); }
    public static void AddJob(GenerateHeightMapJob job, HeightMapWrapper heightMapWrapper) { Instance().AddJobInstance(job, heightMapWrapper); }
    public static void AddJob(CreateMeshColliderJob job, MeshColliderWrapper meshColliderWrapper) { Instance().AddJobInstance(job, meshColliderWrapper); }

    void Start() {
        meshDataJobHandleList = new List<MeshDataJobHandleWrapper>();
        heightMapJobHandleList = new List<HeightMapJobHandleWrapper>();
        meshColliderJobHandleList = new List<MeshColliderJobHandleWrapper>();
        meshDataJobQueue = new Queue<MeshDataJobWrapper>();
        heightMapJobQueue = new Queue<HeightMapJobWrapper>();
        meshColliderJobQueue = new Queue<MeshColliderJobWrapper>();
        jobHandleList = new NativeList<JobHandle>(Allocator.Persistent);

        if (maxChunkGenPerFrame < 1) nextFrame = (int)(1 / maxChunkGenPerFrame);

        Instance(); 
    }

    void LateUpdate() {
        //Complete HeightMap
        if (heightMapJobHandleList.Count > 0) {
            //Debug.Log("Heightmap jobs queued: " + heightMapJobHandleList.Count);
            for (int i = 0; i < heightMapJobHandleList.Count; i++) {
                jobHandleList.Add(heightMapJobHandleList[i].jobHandle);
            }
        }
        //Complete MeshData
        if (meshDataJobHandleList.Count > 0) {
            //Debug.Log("MeshData jobs queued: " + meshDataJobQueue.Count);
            for (int i = 0; i < meshDataJobHandleList.Count; i++) {
                jobHandleList.Add(meshDataJobHandleList[i].jobHandle);
            }
        }
        //Complete MeshColliders
        if (meshColliderJobHandleList.Count > 0) {
            //Debug.Log("MeshCollider jobs queued: " + meshColliderJobQueue.Count);
            for (int i = 0; i < meshColliderJobHandleList.Count; i++) {
                jobHandleList.Add(meshColliderJobHandleList[i].jobHandle);
            }
        }
        //JobHandle.CompleteAll(jobHandleList);
        //jobHandleList.Clear();

        //Callback assignment
        //HeightMap
        for (int i = 0; i < heightMapJobHandleList.Count; i++) {
            if (heightMapJobHandleList[i].jobHandle.IsCompleted) {
                Debug.Log("HeightMap Complete");
                heightMapJobHandleList[i].jobHandle.Complete();
                heightMapJobHandleList[i].heightMapWrapper.SetReturnValue();
                heightMapJobHandleList.RemoveAt(i);
            }
        }
        //heightMapJobHandleList.Clear();

        //MeshData
        for (int i = 0; i < meshDataJobHandleList.Count; i++) {
            if (meshDataJobHandleList[i].jobHandle.IsCompleted) {
                Debug.Log("MeshData Complete");
                meshDataJobHandleList[i].jobHandle.Complete();
                meshDataJobHandleList[i].meshDataWrapper.SetReturnValue();
                meshDataJobHandleList.RemoveAt(i);
            }
        }
        //meshDataJobHandleList.Clear();

        //MeshColliders
        for (int i = 0; i < meshColliderJobHandleList.Count; i++) {
            if (meshColliderJobHandleList[i].jobHandle.IsCompleted) {
                Debug.Log("MeshCollider Complete");
                meshColliderJobHandleList[i].jobHandle.Complete();
                meshColliderJobHandleList[i].meshColliderWrapper.SetReturnValue();
                meshColliderJobHandleList.RemoveAt(i);
            }
        }
        //meshColliderJobHandleList.Clear();
    }

    void Update() {
        //Debug.Log("Mesh Data Waiting: " + meshDataJobQueue.Count);
        //Debug.Log("HeightMap Waiting: " + heightMapJobQueue.Count);
        //Debug.Log("MeshCollider Waiting" + meshColliderJobQueue.Count);
        currFrame++;
        if (maxChunkGenPerFrame > 1 || currFrame > nextFrame) {
            float maxTemp = maxChunkGenPerFrame > 1 ? maxChunkGenPerFrame : 1;
            for (int i = 0; i < maxTemp; i++) {
                if (meshDataJobQueue.Count > 0) {
                    MeshDataJobWrapper current = meshDataJobQueue.Dequeue();
                    meshDataJobHandleList.Add(new MeshDataJobHandleWrapper { jobHandle = current.job.Schedule(), meshDataWrapper = current.meshDataWrapper });
                }

                if (heightMapJobQueue.Count > 0) {
                    HeightMapJobWrapper current = heightMapJobQueue.Dequeue();
                    heightMapJobHandleList.Add(new HeightMapJobHandleWrapper { jobHandle = current.job.Schedule(), heightMapWrapper = current.heightMapWrapper });
                }

                if (meshColliderJobQueue.Count > 0) {
                    MeshColliderJobWrapper current = meshColliderJobQueue.Dequeue();
                    meshColliderJobHandleList.Add(new MeshColliderJobHandleWrapper { jobHandle = current.job.Schedule(), meshColliderWrapper = current.meshColliderWrapper });
                }
            }
        }
        if (currFrame > nextFrame) currFrame = 0;
    }



    void AddJobInstance(GenerateHeightMapJob job, HeightMapWrapper heightMapWrapper) {
        if (heightMapJobHandleList.Count < maxChunkGenPerFrame) heightMapJobHandleList.Add(new HeightMapJobHandleWrapper { jobHandle = job.Schedule(), heightMapWrapper = heightMapWrapper });
        else heightMapJobQueue.Enqueue(new HeightMapJobWrapper { job = job, heightMapWrapper = heightMapWrapper });
    }

    void AddJobInstance(GenerateTerrainMeshJob job, MeshDataWrapper meshDataWrapper) {
        if (meshDataJobHandleList.Count < maxChunkGenPerFrame) meshDataJobHandleList.Add(new MeshDataJobHandleWrapper { jobHandle = job.Schedule(), meshDataWrapper = meshDataWrapper });
        else meshDataJobQueue.Enqueue(new MeshDataJobWrapper { job = job, meshDataWrapper = meshDataWrapper });
    }

    void AddJobInstance(CreateMeshColliderJob job, MeshColliderWrapper meshColliderWrapper) {
        if (meshColliderJobHandleList.Count < maxChunkGenPerFrame) meshColliderJobHandleList.Add(new MeshColliderJobHandleWrapper { jobHandle = job.Schedule(), meshColliderWrapper = meshColliderWrapper });
        else meshColliderJobQueue.Enqueue(new MeshColliderJobWrapper { job = job, meshColliderWrapper = meshColliderWrapper });
    }
}


/// Wrapper Classes for each type

public struct MeshDataJobWrapper {
    public MeshDataWrapper meshDataWrapper;
    public GenerateTerrainMeshJob job;
}

public struct MeshDataJobHandleWrapper {
    public MeshDataWrapper meshDataWrapper;
    public JobHandle jobHandle;
}

public class MeshDataWrapper {
    //Passed Values
    public NativeArray<int> returnVerticesPerLine; //Size 1

    public NativeArray<bool> returnFlipMap; //2D flattened
    public NativeArray<float> returnVertexHeightMap; //3D Flattened
    public NativeArray<int> returnVertexIndexMap;

    public NativeList<float3> returnFloat3Vertices;
    public NativeList<int3> returnInt3Triangles;
    public NativeList<float2> returnFloat2UVs;
    public NativeList<Vector3> returnVertices;
    public NativeList<uint> returnTriangles;
    public NativeList<Vector2> returnUVs;

    public NativeArray<float> nativeHeightMap;
    public NativeArray<float> nativeHeightCurve;

    public NativeArray<bool> returnComplete; //Size 1

    

    //Return values
    MeshData meshData;
    public bool complete;
    public MeshData returnValue;

    public MeshDataWrapper(MeshData meshData, float[] heightMap, float[] heightCurve) {
        this.meshData = meshData;
        complete = false;

        returnVerticesPerLine = new NativeArray<int>(1, Allocator.Persistent);
        returnFlipMap = new NativeArray<bool>(meshData.meshSize * meshData.meshSize, Allocator.Persistent);
        returnVertexHeightMap = new NativeArray<float>(meshData.meshSize * meshData.meshSize * 4, Allocator.Persistent);
        returnVertexIndexMap = new NativeArray<int>(meshData.meshSize * meshData.meshSize * 4, Allocator.Persistent);
        returnFloat3Vertices = new NativeList<float3>(Allocator.Persistent);
        returnInt3Triangles = new NativeList<int3>(Allocator.Persistent);
        returnFloat2UVs = new NativeList<float2>(Allocator.Persistent);
        returnVertices = new NativeList<Vector3>(Allocator.Persistent);
        returnTriangles = new NativeList<uint>(Allocator.Persistent);
        returnUVs = new NativeList<Vector2>(Allocator.Persistent);
        returnComplete = new NativeArray<bool>(1, Allocator.Persistent);
        nativeHeightMap = new NativeArray<float>(MapGenerator.mapChunkSize * MapGenerator.mapChunkSize, Allocator.Persistent);
        nativeHeightMap.CopyFrom(heightMap);
        nativeHeightCurve = new NativeArray<float>(1000, Allocator.Persistent);
        nativeHeightCurve.CopyFrom(heightCurve);
        returnComplete[0] = false;
    }

    public void SetReturnValue() {
        //Debug.Log("Setting Mesh Return Value");
        meshData.verticesPerLine = returnVerticesPerLine[0];

        meshData.float3Vertices = returnFloat3Vertices;
        meshData.int3Triangles = returnInt3Triangles;
        meshData.float2UVs = returnFloat2UVs;
        meshData.vertices = returnVertices;
        meshData.triangles = returnTriangles;
        meshData.uvs = returnUVs;
        meshData.flipMap = returnFlipMap;
        meshData.vertexHeightMap = returnVertexHeightMap;
        meshData.vertexIndexMap = returnVertexIndexMap;

        meshData.CreateMesh();

        returnVerticesPerLine.Dispose();
        nativeHeightCurve.Dispose();
        nativeHeightMap.Dispose();
        returnComplete.Dispose();

        returnValue = meshData;
        complete = true;
    }
}

public struct HeightMapJobWrapper {
    public HeightMapWrapper heightMapWrapper;
    public GenerateHeightMapJob job;
}

public struct HeightMapJobHandleWrapper {
    public HeightMapWrapper heightMapWrapper;
    public JobHandle jobHandle;
}

public class HeightMapWrapper {
    public NativeArray<float> returnArray;
    public NativeArray<bool> returnComplete;

    public bool complete;
    public float[] returnValue;
    public HeightMapWrapper() { 
        complete = false;
        returnValue = new float[MapGenerator.mapChunkSize * MapGenerator.mapChunkSize];

        returnComplete = new NativeArray<bool>(1, Allocator.Persistent);
        returnArray = new NativeArray<float>(MapGenerator.mapChunkSize * MapGenerator.mapChunkSize, Allocator.Persistent);
        returnComplete[0] = false;
    }

    public void SetReturnValue() {
        
        returnValue = returnArray.ToArray();
        returnComplete.Dispose();
        returnArray.Dispose();
        //Debug.Log("Setting Heightmap Return Value");
        complete = true;
    }
}

public struct MeshColliderJobWrapper {
    public MeshColliderWrapper meshColliderWrapper;
    public CreateMeshColliderJob job;
}
public struct MeshColliderJobHandleWrapper {
    public MeshColliderWrapper meshColliderWrapper;
    public JobHandle jobHandle;
}

public class MeshColliderWrapper {
    public NativeArray<float3> float3Vertices;
    public NativeArray<uint> triangles;
    public NativeArray<int3> int3Triangles;
    public NativeArray<bool> returnComplete;
    public NativeArray<BlobAssetReference<Unity.Physics.Collider>> output;
    public BlobBuilder builder;
    public bool complete;
    EntityManager entityManager;
    Entity meshEntity;

    public MeshColliderWrapper(MeshData meshData, EntityManager entityManager, Entity meshEntity) {
        float3Vertices = meshData.float3Vertices;
        int3Triangles = meshData.int3Triangles;
        triangles = meshData.triangles;
        output = new NativeArray<BlobAssetReference<Unity.Physics.Collider>>(1, Allocator.Persistent);

        complete = false;
        returnComplete = new NativeArray<bool>(1, Allocator.Persistent);
        this.entityManager = entityManager;
        this.meshEntity = meshEntity;
    }

    public void SetReturnValue() {
        //Debug.Log("Setting Collider Return Value");
        PhysicsCollider collider = new PhysicsCollider { Value = output[0] };
        EntityCommandBufferSystem ecbSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer buffer = ecbSystem.CreateCommandBuffer();
        buffer.SetComponent(meshEntity, collider);
        output.Dispose();
        returnComplete.Dispose();
        complete = true;
    }
}