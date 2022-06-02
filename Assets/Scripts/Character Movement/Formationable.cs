using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Formationable : Movable
{
    //Constants
    static float leaderSpeedCoeff = 0.5f;
    const float minDistanceForUpdate = 0.25f;
    const int minFrameUpdate = 6;
    

    public bool debugGrid;
    [HideInInspector]
    public List<GameObject> followerList = null;
    [HideInInspector]
    public Vector2 relativePos = Vector3.zero;
    [HideInInspector]
    public GameObject leadUnit = null;

    int currFrame = 0;
    //Debug
    GameObject test;
    bool debugThing = false;

    void Start()
    {
        base.Start();

        //Debug
        if (debugGrid) {
            test = GameObject.CreatePrimitive(PrimitiveType.Cube);
            test.GetComponent<BoxCollider>().enabled = false;
        }
    }

    void FixedUpdate() {
        base.FixedUpdate();

        if (leadUnit != null) {
            GoToRelative();
        }
    }

    void Update()
    {
        if (followerList != null && !base.hasPath && !base.generatingPath ) {
            foreach(GameObject unit in followerList) {
                Formationable script = unit.GetComponent<Formationable>();
                script.relativePos = Vector2.zero;
                script.leadUnit = null;
            }
            followerList = null;
        }
    }

    /// <summary>GoTo for moving to position relative to leader</summary>
    void GoToRelative() {
        //TODO: Make sure when leader is changed, a new destination is immediately set.

        //Get Relative Position:
        Vector3 newPos = leadUnit.transform.position + leadUnit.transform.TransformDirection(new Vector3(relativePos.x, 0, relativePos.y));
        Vector2 newPos2D = new Vector2(newPos.x, newPos.z);
        newPos = new Vector3(newPos.x, EndlessTerrain.GetHeightFromMesh(newPos2D), newPos.z);
        
        //Break if transform.position hasn't moved too much and we have a path
        if(hasPath && Vector2.Distance(new Vector2(transform.position.x, transform.position.z), newPos2D) < minDistanceForUpdate){
            return;
        }

        //Okay so we don't want to generate a new path every frame, so we'll apply some restrictions
        //This needs testing but the algorithm should compare remaining distance to destination and difference between destinations.
        //I also added an angle difference test, curr----x----new doesn't make sense to go to curr, angle would be 180
        Vector3 testDest = destination;
        if (base.pathInstantiated) {
            Vector2 dest2D = GetNextDestination();
            if(dest2D != Vector2.zero) testDest = new Vector3(dest2D.x, EndlessTerrain.GetHeightFromMesh(dest2D), dest2D.y);
        }
        float destinationDifference1 = Vector2.Distance(new Vector2(destination.x, destination.z), newPos2D);
        if (destinationDifference1 < 2) destinationDifference1 = 2;
        float destinationDifference2 = Vector2.Distance(new Vector2(testDest.x, testDest.z), newPos2D);
        if (destinationDifference2 < 2) destinationDifference2 = 2;
        float destinationDistance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), newPos2D);

        //Angle stuff
        Vector3 v1 = newPos - transform.position;
        Vector3 v2 = testDest - transform.position;
        float angle = Mathf.Abs(Vector3.Angle(new Vector3(v1.x, 0, v1.z), new Vector3(v2.x, 0, v2.z)));
        float angleRelative = (angle * (40 - 1) / 180) + 1;

        //Finally we test if we pass our algorithm
        if (destinationDistance < TerrainData.uniformScale || (!hasPath && !generatingPath) || (destinationDistance <= destinationDifference2 * angleRelative * destinationDifference1 && currFrame > minFrameUpdate)) {
            GoTo(newPos);
            debugThing = false;
            currFrame = 0;
        }
        //Debug
        else if (!debugThing && currFrame >= minFrameUpdate) { debugThing = true; }
        if (debugGrid) test.transform.position = newPos;
        if (debugThing) {
            Debug.Log("Not making path for efficiency, angle = " + angle + ":" + angleRelative);
        }

        currFrame++;
    }

    /// <summary>Send list of units to position</summary>
    /// <param name="units">List of units</param>
    /// <param name="destination">World Vector3 destination</param>
    public static void GroupGoTo(List<GameObject> units, Vector3 destination) {
        UnitGrid grid = UnitGrid.GenerateUnitGrid(units);
        foreach(GameObject unit in units) {
            if (unit == grid.leadObject) {
                Movable movable = unit.GetComponent<Movable>();
                //Set speed to 1/2 speed, but this should eventually be changed.
                movable.speed = leaderSpeedCoeff * movable.maxSpeed;
                movable.GoTo(destination);
                List<GameObject> dupeList = new List<GameObject>(units);
                dupeList.Remove(unit);
                unit.GetComponent<Formationable>().followerList = dupeList;
                
                
                //TESTING PURPOSES ONLY
                unit.GetComponent<MeshRenderer>().material = unit.GetComponent<Formationable>().testMat;
            }
            else {
                grid.SearchAndPlace(unit);
                Formationable script = unit.GetComponent<Formationable>();
                script.relativePos = grid.GetRelativePos(unit);
                script.leadUnit = grid.leadObject;
            }
        }
    }
}
