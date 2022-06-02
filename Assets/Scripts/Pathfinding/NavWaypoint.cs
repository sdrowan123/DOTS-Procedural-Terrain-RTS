using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class NavMesh {

    //====================================
    //      Calculation Methods
    //====================================

    /// <summary>Returns cost between this navquad and another</summary>
    public static float CostBetween(int4 pos1, int4 pos2, NativeHashMap<int4, int2> worldNavMeshWeightsMap) {
        if (pos2.Equals(NorthPos(pos1, worldNavMeshWeightsMap))) return worldNavMeshWeightsMap[pos1].x;
        else if (pos2.Equals(EastPos(pos1, worldNavMeshWeightsMap))) return worldNavMeshWeightsMap[pos2].y;
        else if (pos2.Equals(SouthPos(pos1, worldNavMeshWeightsMap))) return worldNavMeshWeightsMap[pos2].x;
        else if (pos2.Equals(WestPos(pos1, worldNavMeshWeightsMap))) return worldNavMeshWeightsMap[pos1].x;
        else {
            Debug.LogWarning("CostBetween Error: Can't find neighbor quad");
            return 0;
        }
    }


    //================================
    //         Getter Methods
    //================================

    public static int4 NorthPos(int4 pos, NativeHashMap<int4, int2> worldNavMeshWeightMap) {
        if (pos.w > 0)
            return new int4(pos.x, pos.y, pos.z, pos.w - 1);
        else {
            int4 northPos = new int4(pos.x, pos.y + 1, pos.z, MapGenerator.mapChunkSize - 2);
            if (worldNavMeshWeightMap.ContainsKey(northPos))
                return northPos;
            else {
                Debug.LogWarning("Tried to get north pos from uninstantiated navmesh.");
                return pos;
            };
        }
    }

    public static int4 WestPos(int4 pos, NativeHashMap<int4, int2> worldNavMeshWeightMap) {
        if (pos.z > 0)
            return new int4(pos.x, pos.y, pos.z - 1, pos.w);
        else {
            int4 westPos = new int4(pos.x - 1, pos.y, MapGenerator.mapChunkSize - 2, pos.w);
            if (worldNavMeshWeightMap.ContainsKey(westPos))
                return westPos;
            else {
                Debug.LogWarning("Tried to get west pos from uninstantiated navmesh.");
                return pos;
            };
        }
    }

    public static int4 EastPos(int4 pos, NativeHashMap<int4, int2> worldNavMeshWeightMap) {
        if (pos.z < MapGenerator.mapChunkSize - 2)
            return new int4(pos.x, pos.y, pos.z + 1, pos.w);
        else {
            int4 eastPos = new int4(pos.x + 1, pos.y, 0, pos.w);
            if (worldNavMeshWeightMap.ContainsKey(eastPos))
                return eastPos;
            else {
                Debug.LogWarning("Tried to get east pos from uninstantiated navmesh.");
                return pos;
            };
        }
    }

    public static int4 SouthPos(int4 pos, NativeHashMap<int4, int2> worldNavMeshWeightMap) {
        if (pos.w < MapGenerator.mapChunkSize - 2)
            return new int4(pos.x, pos.y, pos.z, pos.w + 1);
        else {
            int4 southPos = new int4(pos.x, pos.y - 1, pos.z, 0);
            if (worldNavMeshWeightMap.ContainsKey(southPos))
                return southPos;
            else {
                Debug.LogWarning("Tried to get south pos from uninstantiated navmesh.");
                return pos;
            };
        }
    }

    //=======================================
    //         Debug/Display Methods
    //=======================================
    /*
    public void DisplayNorth(Vector3 coord, Material mat) {
        if (!northInstantiated) {
            northDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northDisplay.GetComponent<BoxCollider>().enabled = false;
            northInstantiated = true;
        }
        northDisplay.transform.position = coord;
        northDisplay.GetComponent<MeshRenderer>().material = mat;
    }

    public void DisplayWest(Vector3 coord, Material mat) {
        if (!westInstantiated) {
            westDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westDisplay.GetComponent<BoxCollider>().enabled = false;
            westInstantiated = true;
        }
        westDisplay.transform.position = coord;
        westDisplay.GetComponent<MeshRenderer>().material = mat;
    }

    public void DisplaySouth(Vector3 coord, Material mat) {
        SouthQuad().DisplayNorth(coord, mat);
    }

    public void DisplayEast(Vector3 coord, Material mat) {
        EastQuad().DisplayWest(coord, mat);
    }

    public void UpdateMatNorth(Material mat) {
        northDisplay.GetComponent<MeshRenderer>().material = mat;
    }

    public void UpdateMatWest(Material mat) {
        westDisplay.GetComponent<MeshRenderer>().material = mat;
    }

    public void UpdateMatSouth(Material mat) {
        SouthQuad().northDisplay.GetComponent<MeshRenderer>().material = mat;
    }

    public void UpdateMatEast(Material mat) {
        EastQuad().westDisplay.GetComponent<MeshRenderer>().material = mat;
    }*/

}
