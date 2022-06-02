using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MouseHighlight : MouseMode
{
    //TO FIX BUG WITH VERTICALITY:
    //ADD MORE POINTS ON SQUARE AND MORE TRIANGLES
    //PUT ALL TRIANGLES IN A LIST FOR NICE EASY FOREACH TO TEST IF THEIR IN ANY OF THE TRIANGLES
    //CONSIDER USING TRIANLGE OBJECT

    //Consider adding methods to (de)select or (un)highlight all
    public Material testMat1;
    public Material testMat2;
    public Material testMat3;
    public float delay;
    public string playerTag;

    float startTime;
    Vector3 startPosScreen;
    bool dragging = false;

    List<GameObject> highlightedUnits;
    GameObject singleHighlightedUnit = null;
    public List<GameObject> selectedPlayables;
    public List<GameObject> selectedMovables;
    public List<GameObject> selectedObjects;

    RectTransform selectionSquareTransform;
    GameObject selectionSquare;

    //FOr testing:
    GameObject cube1;
    GameObject cube2;
    GameObject cube3;
    GameObject cube4;

    Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        selectedPlayables = new List<GameObject>();
        highlightedUnits = new List<GameObject>();

        selectionSquare = GameObject.FindGameObjectWithTag("Selection Square");
        selectionSquare.SetActive(false);
        selectionSquareTransform = selectionSquare.GetComponent<RectTransform>();
        mainCamera = Camera.main;

        //For Testing:
        cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
    }

    // Update is called once per frame
    void Update()
    {

        //On mouse click
        if (Input.GetMouseButtonDown(0)) {
            dragging = true;
            startTime = Time.time;
            startPosScreen = Input.mousePosition;

            foreach(GameObject unit in selectedPlayables) {
                DeselectUnit(unit, false);
            }
            foreach (GameObject unit in selectedMovables) {
                DeselectUnit(unit, false);
            }
            foreach (GameObject unit in selectedObjects) {
                DeselectUnit(unit, false);
            }
            selectedPlayables.Clear();
            selectedMovables.Clear();
            selectedObjects.Clear();
        }

        //On Mouse release
        if (Input.GetMouseButtonUp(0)) {
            selectionSquare.SetActive(false);
            dragging = false;
            //Debug.Log("Highlighted Units = " + highlightedUnits.Count);
            foreach(GameObject unit in new List<GameObject>(highlightedUnits)) {
                UnhighlightUnit(unit);
                SelectUnit(unit);
            }

            //Single select:
            if (singleHighlightedUnit != null) {
                singleHighlightedUnit = null;
            }

            highlightedUnits.Clear();
        }

        //While dragging
        if(dragging == true && startTime - Time.time <= delay) {
            //Debug.Log("Dragging");
            selectionSquare.SetActive(true);
            DrawSquare(startPosScreen, Input.mousePosition);
            //Testing making a big ass square and then going thru each one in that
            Vector3[] squareEdges = GetSquareEdgesWorld(startPosScreen, Input.mousePosition);
            Vector3 avgPos = (squareEdges[0] + squareEdges[1] + squareEdges[2] + squareEdges[3]) / 4;
            float maxX = 0;
            float maxZ = 0;
            foreach (Vector3 squareEdge in squareEdges) {
                float currX = Mathf.Abs(squareEdge.x - avgPos.x);
                float currZ = Mathf.Abs(squareEdge.z - avgPos.z);

                if (currX > maxX) maxX = currX;
                if (currZ > maxZ) maxZ = currZ;
            }

            Vector3 extents = new Vector3(maxX + 10, 200, maxZ + 10);
            Collider[] colliders = Physics.OverlapBox(avgPos, extents);
            foreach(Collider collider in colliders) {
                GameObject unit = collider.gameObject;
                if (unit.GetComponent<Selectable>()) {
                    if (IsWithinPolygon(unit.transform.position, squareEdges[0], squareEdges[1], squareEdges[2], squareEdges[3])) {
                        if (!highlightedUnits.Contains(unit)) {
                            HighlightUnit(unit);
                        }
                    }
                    else {
                        UnhighlightUnit(unit);
                    }
                }
            }

            //Make single unit stay highlighted:
            if(singleHighlightedUnit != null) {
                singleHighlightedUnit = null;
            }
        }

        //Hover Highlight
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask defaultMask = LayerMask.GetMask("Default");
        if (Physics.Raycast(ray, out hit, 100000f, defaultMask)) {
            GameObject unit = hit.transform.gameObject;
            Selectable script = unit.GetComponent<Selectable>();
            if (script != null) {
                if (!selectedPlayables.Contains(unit) && !selectedMovables.Contains(unit) && !selectedObjects.Contains(unit) && !highlightedUnits.Contains(unit)) {
                    if (singleHighlightedUnit == null) {
                        singleHighlightedUnit = unit;
                        HighlightUnit(unit);
                    }
                    else {
                        UnhighlightUnit(singleHighlightedUnit);
                        singleHighlightedUnit = unit;
                        HighlightUnit(unit);
                    }
                }
            }
            else if (singleHighlightedUnit != null) {
                UnhighlightUnit(singleHighlightedUnit);
                singleHighlightedUnit = null;
            }
        }
        else if (singleHighlightedUnit != null) {
            UnhighlightUnit(singleHighlightedUnit);
            singleHighlightedUnit = null;
        }
    }

    public void HighlightUnit(GameObject unit) {
        Selectable script = unit.GetComponent<Selectable>();
        highlightedUnits.Add(unit);
        script.highlighted = true;

        if(script.cursorHighlightOverride != 0) {
            MouseCursor.ChangeCursor(script.cursorHighlightOverride);
        }
    }

    public void UnhighlightUnit(GameObject unit) {
        Selectable script = unit.GetComponent<Selectable>();
        if (script.highlighted == true) {
            script.highlighted = false;
            highlightedUnits.Remove(unit);

            if (script.cursorHighlightOverride != 0) {
                MouseCursor.DefaultCursor();
            }
        }
    }

    public void SelectUnit(GameObject unit) {
        Selectable script = unit.GetComponent<Selectable>();
        if (script.playableUnit) {
            selectedPlayables.Add(unit);
        }else if (script.movableUnit) {
            selectedMovables.Add(unit);
        }
        else {
            selectedObjects.Add(unit);
        }
        script.selected = true;
    }

    public void DeselectUnit(GameObject unit, bool clear = true) {
        Selectable script = unit.GetComponent<Selectable>();
        if (script.selected == true) {
            script.selected = false;
            if (clear) {
                if (script.playableUnit) {
                    selectedPlayables.Remove(unit);
                }
                else if (script.movableUnit) {
                    selectedMovables.Remove(unit);
                }
                else {
                    selectedObjects.Remove(unit);
                }
            }
        }
    }

    void DrawSquare(Vector3 screenPos1, Vector3 screenPos2) {
        Vector3 middle = (screenPos1 + screenPos2) / 2f;
        selectionSquareTransform.position = middle;

        float sizeX = Mathf.Abs(screenPos1.x - screenPos2.x);
        float sizeY = Mathf.Abs(screenPos1.y - screenPos2.y);
        selectionSquareTransform.sizeDelta = new Vector2(sizeX, sizeY);
    }

    Vector3[] GetSquareEdgesWorld(Vector3 screenPos1, Vector3 screenPos2) {
        Vector3 screenPos3 = new Vector3(screenPos1.x, screenPos2.y, 0);
        Vector3 screenPos4 = new Vector3(screenPos2.x, screenPos1.y, 0);

        Vector3 worldPos1 = ScreenToWorld.Collision(screenPos1);
        Vector3 worldPos2 = ScreenToWorld.Collision(screenPos2);
        Vector3 worldPos3 = ScreenToWorld.Collision(screenPos3);
        Vector3 worldPos4 = ScreenToWorld.Collision(screenPos4);

        //DEBUG SQUARES:
        //cube1.transform.position = worldPos1;
        //cube2.transform.position = worldPos2;
        //cube3.transform.position = worldPos3;
        //cube4.transform.position = worldPos4;

        return new Vector3[] { worldPos1, worldPos3, worldPos4, worldPos2 };
    }

    bool IsWithinTriangle(Vector3 p, Vector3 p1, Vector3 p2, Vector3 p3) {
        bool isWithinTriangle = false;

        //Need to set z -> y because of other coordinate system
        float denominator = ((p2.z - p3.z) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.z - p3.z));

        float a = ((p2.z - p3.z) * (p.x - p3.x) + (p3.x - p2.x) * (p.z - p3.z)) / denominator;
        float b = ((p3.z - p1.z) * (p.x - p3.x) + (p1.x - p3.x) * (p.z - p3.z)) / denominator;
        float c = 1 - a - b;

        //The point is within the triangle if 0 <= a <= 1 and 0 <= b <= 1 and 0 <= c <= 1
        if (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f) {
            isWithinTriangle = true;
        }

        return isWithinTriangle;
    }

    bool IsWithinPolygon(Vector3 unitPos, Vector3 TL, Vector3 BL, Vector3 TR, Vector3 BR) {
        bool isWithinPolygon = false;

        //The polygon forms 2 triangles, so we need to check if a point is within any of the triangles
        //Triangle 1: TL - BL - TR
        if (IsWithinTriangle(unitPos, TL, BL, TR)) {
            return true;
        }

        //Triangle 2: TR - BL - BR
        if (IsWithinTriangle(unitPos, TR, BL, BR)) {
            return true;
        }

        return isWithinPolygon;
    }
}
