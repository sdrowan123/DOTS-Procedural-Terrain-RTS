using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPlacementTest : MouseMode
{
    //Placeholder script
    public GameObject toPlace;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(1)) {
            Vector3 pos = MouseWorldPos();
            pos.y += 1;
            Instantiate(toPlace, pos, Quaternion.identity);
        }
    }
}
