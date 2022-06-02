using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FellTree : MonoBehaviour
{
    public GameObject log;
    public GameObject stump;

    public void Fell() {
        //These numbers are all arbitrary and subject to change.
        Vector3 pos = transform.position;
        //Send it way away first
        gameObject.transform.position = new Vector3(1000,10000, 1000);
        GameObject stumpInstance = Instantiate(stump, pos, transform.rotation);
        stumpInstance.transform.localScale = transform.localScale;

        Vector3 logPos = new Vector3(pos.x, pos.y + 0.01f, pos.z);
        float terrainY = EndlessTerrain.GetHeightFromMesh(new Vector2(logPos.x, logPos.z));
        if (terrainY > logPos.y + 0.5f) {
            logPos.y = terrainY + 0.05f;
            stumpInstance.transform.position = new Vector3(stumpInstance.transform.position.x, terrainY - 0.1f, stumpInstance.transform.position.z);
        }

        GameObject logInstance = Instantiate(log, logPos, transform.rotation);
        logInstance.transform.localScale = transform.localScale;
    }
}
