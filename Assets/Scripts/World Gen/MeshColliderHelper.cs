using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using PCollider = Unity.Entities.BlobAssetReference<Unity.Physics.Collider>;


[BurstCompile(FloatMode = FloatMode.Fast)]
public struct CreateMeshColliderJob : IJob {

    [ReadOnly]public NativeArray<float3> float3Vertices;
    [ReadOnly]public NativeArray<int3> int3Triangles;
    [WriteOnly]public NativeArray<bool> complete;
    [WriteOnly] public NativeArray<PCollider> output;

    public void Execute() {

        output[0] = Unity.Physics.MeshCollider.Create(float3Vertices, int3Triangles);
        complete[0] = true;
    }
}

