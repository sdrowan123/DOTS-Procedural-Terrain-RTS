using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGrid
{
    public GameObject leadObject;
    int width;
    int height;

    List<Vector2> localPositions;
    List<Vector2> backRow;
    List<Vector2> backRow2;
    Dictionary<GameObject, Vector2> dictionary;

    /// <summary>DO NOT USE, instead use Static GenerateUnitGrid()</summary>
    public UnitGrid(GameObject leadObject, int poolSize, float unitSpacing) {
        this.leadObject = leadObject;
        dictionary = new Dictionary<GameObject, Vector2>();
        localPositions = new List<Vector2>();
        //I use seperate back rows that fill last to ensure a nice shape.
        backRow = new List<Vector2>();
        backRow2 = new List<Vector2>();

        float gridSizeSqrt = Mathf.Sqrt(poolSize);
        width = (int)(gridSizeSqrt);
        height = (int)(gridSizeSqrt);

        //Find Relative distance between middle object and middle position of grid:
        Vector2 midGridPos = new Vector2((int)(-width * unitSpacing / 2) + (int)width / 2 * unitSpacing, (int)(-height * unitSpacing / 2) + (int)height / 2 * unitSpacing);
        float xDiff = -midGridPos.x;
        float yDiff = -midGridPos.y;

        //Set Up Grid
        for (int y = height - 1; y > -3; y--) {
            for (int x = 0; x < width; x++) {
                //CHeck for back row which is technically front row since pathing always seems to start backwards...
                if (y >= 0)
                    localPositions.Add(new Vector2((int)(-width * unitSpacing / 2) + x * unitSpacing + xDiff, (int)(-height * unitSpacing / 2) + y * unitSpacing + yDiff));
                else if (y >= -1)
                    backRow.Add(new Vector2((int)(-width * unitSpacing / 2) + x * unitSpacing + xDiff, (int)(-height * unitSpacing / 2) + y * unitSpacing + yDiff));
                else
                    backRow2.Add(new Vector2((int)(-width * unitSpacing / 2) + x * unitSpacing + xDiff, (int)(-height * unitSpacing / 2) + y * unitSpacing + yDiff));
            }
        }

        SearchAndPlace(leadObject);
    }

    /// <summary>Place one unit in closest position in grid</summary>
    public void SearchAndPlace(GameObject unit) {
        //Fill normal grid positions first
        if (localPositions.Count > 0) {
            Vector2 unitPos = new Vector2(unit.transform.position.x, unit.transform.position.z);
            Vector3 pos3 = leadObject.transform.position + leadObject.transform.TransformDirection(new Vector3(localPositions[0].x, 0, localPositions[0].y));
            Vector2 pos2 = new Vector2(pos3.x, pos3.z);
            Vector2 closest = localPositions[0];
            float distance = Vector2.Distance(unitPos, pos2);
            foreach (Vector2 pos in new List<Vector2>(localPositions)) {
                pos3 = leadObject.transform.position + leadObject.transform.TransformDirection(new Vector3(pos.x, 0, pos.y));
                pos2 = new Vector2(pos3.x, pos3.z);
                float newDistance = Vector2.Distance(unitPos, pos2);
                if (newDistance < distance) {
                    distance = newDistance;
                    closest = pos;
                }
            }
            localPositions.Remove(closest);
            dictionary[unit] = closest;
        }
        //Then Fill backrow1
        else if (backRow.Count > 0) {
            Vector2 unitPos = new Vector2(unit.transform.position.x, unit.transform.position.z);
            Vector3 pos3 = leadObject.transform.position + leadObject.transform.TransformDirection(new Vector3(backRow[0].x, 0, backRow[0].y));
            Vector2 pos2 = new Vector2(pos3.x, pos3.z);
            Vector2 closest = backRow[0];
            float distance = Vector2.Distance(unitPos, pos2);
            foreach (Vector2 pos in new List<Vector2>(backRow)) {
                pos3 = leadObject.transform.position + leadObject.transform.TransformDirection(new Vector3(pos.x, 0, pos.y));
                pos2 = new Vector2(pos3.x, pos3.z);
                float newDistance = Vector2.Distance(unitPos, pos2);
                if (newDistance < distance) {
                    distance = newDistance;
                    closest = pos;
                }
            }
            backRow.Remove(closest);
            dictionary[unit] = closest;
        }
        //Then Fill backrow2
        else if (backRow2.Count > 0) {
            Vector2 unitPos = new Vector2(unit.transform.position.x, unit.transform.position.z);
            Vector3 pos3 = leadObject.transform.position + leadObject.transform.TransformDirection(new Vector3(backRow2[0].x, 0, backRow2[0].y));
            Vector2 pos2 = new Vector2(pos3.x, pos3.z);
            Vector2 closest = backRow2[0];
            float distance = Vector2.Distance(unitPos, pos2);
            foreach (Vector2 pos in new List<Vector2>(backRow2)) {
                pos3 = leadObject.transform.position + leadObject.transform.TransformDirection(new Vector3(pos.x, 0, pos.y));
                pos2 = new Vector2(pos3.x, pos3.z);
                float newDistance = Vector2.Distance(unitPos, pos2);
                if (newDistance < distance) {
                    distance = newDistance;
                    closest = pos;
                }
            }
            backRow2.Remove(closest);
            dictionary[unit] = closest;
        }
        else {
            Debug.LogWarning("All Grid Positions Filled");
        }
    }

    public Vector2 GetRelativePos(GameObject unit) {
        if (dictionary.ContainsKey(unit)) {
            return dictionary[unit];
        }
        else {
            Debug.LogWarning("Unit has no position in the grid");
            return Vector2.zero;
        }
    }

    public Vector2 GetWorldPos(GameObject unit) {
        if (dictionary.ContainsKey(unit)) {
            Vector2 leadPos2D = new Vector2(leadObject.transform.position.x, leadObject.transform.position.z);
            return leadPos2D + GetRelativePos(unit);
        }
        else {
            Debug.LogWarning("Unit has no position in the grid");
            return Vector2.zero;
        }
    }


    /// <summary>Generates and holds data for moving objects relative to eachother in a grid formation, returns UnitGrid.</summary>
    public static UnitGrid GenerateUnitGrid(List<GameObject> units) {
        //Unit spacing should be dynamic but for now we'll make it static here
        float unitSpacing = 2f;
        //First step is to find middle unit, not including stray units
        //So we will first find the average position, and take the unit closest
        float xSum = 0;
        float zSum = 0;
        foreach (GameObject unit in units) {
            xSum += unit.transform.position.x;
            zSum += unit.transform.position.z;
        }

        float xAverage = xSum / units.Count;
        float zAverage = zSum / units.Count;
        float avg = xAverage + zAverage / 2;

        float currAvg = units[0].transform.position.x + units[0].transform.position.z / 2;
        float minAvgDistance = Mathf.Abs(currAvg - avg);
        GameObject middleUnit = units[0];

        foreach (GameObject unit in units) {
            currAvg = unit.transform.position.x + unit.transform.position.z / 2;
            if (minAvgDistance > Mathf.Abs(currAvg - avg)) {
                minAvgDistance = Mathf.Abs(currAvg - avg);
                middleUnit = unit;
            }
        }
        UnitGrid grid = new UnitGrid(middleUnit, units.Count, unitSpacing);
        //Debug
        Debug.Log("Units = " + units.Count);
        return grid;
    }
}
