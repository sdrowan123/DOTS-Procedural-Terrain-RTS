using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.WSA.Input;

public class MouseMovement : MouseMode {
    MouseHighlight mouseHighlight;
    public Material testMat;
    public int distanceBetweenUnits;

    void Start() {
        mouseHighlight = FindObjectOfType<MouseHighlight>();
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetMouseButtonDown(1)) {
            if (mouseHighlight.selectedPlayables.Count > 0) {
                Formationable.GroupGoTo(mouseHighlight.selectedPlayables, MouseWorldPos());
            }
        }
    }
}







//OLD CODE:
/*foreach (GameObject unit in new List<GameObject>(mouseHighlight.selectedUnits)) {
        if (!CheckAndResendUnits(unit)) {
            unit.GetComponent<PlayerMovable>().moving = false;

            if (unit != middleUnit) {
                unit.GetComponent<PlayerMovable>().posDestination = Vector3.zero;
                if (unit.GetComponent<PlayerMovable>().unitDestination != null) {
                    unit.GetComponent<PlayerMovable>().unitDestination.GetComponent<PlayerMovable>().followers.Remove(unit);
                }
                unit.GetComponent<PlayerMovable>().unitDestination = middleUnit;
            }
        }
    }*/

//CHecks if unit in group has followers and sends them to a different leader
/*bool CheckAndResendUnits(GameObject potLeader) {
    if (potLeader.GetComponent<PlayerMovable>().followers.Count > 0) {
        GameObject unit = potLeader.GetComponent<PlayerMovable>().followers[0];
        unit.GetComponent<PlayerMovable>().posDestination = potLeader.GetComponent<PlayerMovable>().posDestination;
        unit.GetComponent<PlayerMovable>().moving = false;
        unit.GetComponent<PlayerMovable>().unitDestination.GetComponent<PlayerMovable>().followers.Remove(unit);
        unit.GetComponent<PlayerMovable>().unitDestination = null;

        GameObject replacementMidUnit = unit;

        for (int i = 1; i < potLeader.GetComponent<PlayerMovable>().followers.Count; i++) {
            unit = potLeader.GetComponent<PlayerMovable>().followers[i];

            unit.GetComponent<PlayerMovable>().unitDestination = replacementMidUnit;
            unit.GetComponent<PlayerMovable>().posDestination = Vector3.zero;
            unit.GetComponent<PlayerMovable>().moving = false;
        }

        potLeader.GetComponent<PlayerMovable>().moving = false;
        potLeader.GetComponent<PlayerMovable>().followers.Clear();
        return true;
    }
    return false;
}*/
