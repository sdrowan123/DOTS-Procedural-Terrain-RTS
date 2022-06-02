using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavMeshDisplay : MonoBehaviour
{
    public bool displayNavMesh;
    public Material cliffMaterial;
    public Material normalMaterial;
    public Material steepMaterial;
    public Material pathMaterial;

    public void DisplayUpdate(Vector2 pos) {
        if(displayNavMesh){
            Debug.Log("Displaying NavMesh for " + pos);
            NavMesh navMesh = EndlessTerrain.GetNavMeshFromDict(pos);

            for(int x = 0; x < navMesh.meshSize; x ++) {
                for(int y = 0; y < navMesh.meshSize; y++) {
                    float worldX = pos.x * TerrainData.uniformScale * 96 - (96 * TerrainData.uniformScale) / 2 + x * TerrainData.uniformScale;
                    float worldZ = pos.y * TerrainData.uniformScale * 96 + (96 * TerrainData.uniformScale) / 2 - y * TerrainData.uniformScale;
                    float worldY = EndlessTerrain.GetHeightFromMesh(new Vector2(worldX, worldZ));

                    Vector3 westPos = new Vector3(worldX, worldY, worldZ - TerrainData.uniformScale / 2);
                    Vector3 northPos = new Vector3(worldX + TerrainData.uniformScale / 2, worldY, worldZ);

                    NavQuad quad = navMesh.GetQuad(x, y);

                    Material northMat;
                    float northWeight = quad.NorthWeight();
                    if(northWeight == 0) {
                        northMat = cliffMaterial;
                    }
                    else if(northWeight == 16) {
                        northMat = normalMaterial;
                    }
                    else {
                        northMat = steepMaterial;
                    }
                    quad.DisplayNorth(northPos, northMat);

                    Material westMat;
                    float westWeight = quad.WestWeight();
                    if (westWeight == 0) {
                        westMat = cliffMaterial;
                    }
                    else if (westWeight == 16) {
                        westMat = normalMaterial;
                    }
                    else {
                        westMat = steepMaterial;
                    }
                    quad.DisplayWest(westPos, westMat);
                }
            }
        }
    }
}
