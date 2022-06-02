using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseMode : MonoBehaviour
{
    public static Vector3 MouseWorldPos() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask terrainMask = LayerMask.GetMask("Terrain");
        Physics.Raycast(ray, out hit, 100000f, terrainMask);

        return new Vector3(hit.point.x, hit.point.y, hit.point.z);
    }
}
