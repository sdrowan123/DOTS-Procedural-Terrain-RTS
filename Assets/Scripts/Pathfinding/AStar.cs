using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;
using System;
using System.Threading;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

public static class AStar {

    public struct GeneratePathJobInfo {
        public float2 startPosWorld;
        public float2 endPosWorld;
        public int2 startChunkCoord;
        public int2 endChunkCoord;
        public int2 startPosLocal;
        public int2 endPosLocal;
        public NativeHashMap<int4, int2> worldNavMeshWeightsMap;
        public NativeList<int4> returnPath;
        public NativeArray<bool> complete;
    }

    public static GeneratePathJobInfo CreatePathJobInfo(Vector2 startPosWorld, Vector2 endPosWorld, NativeHashMap<int4, int2> worldNavMeshWeightsMap, NativeList<int4> returnPath, NativeArray<bool> complete) {
        float scale = TerrainData.uniformScale;
        startPosWorld = new float2((int)Math.Floor(startPosWorld.x / scale) * scale + scale / 2, (int)Math.Floor(startPosWorld.y / scale) * scale + scale / 2);
        endPosWorld = new float2((int)Math.Floor(endPosWorld.x / scale) * scale + scale / 2, (int)Math.Floor(endPosWorld.y / scale) * scale + scale / 2);
        int2 startChunkCoord = EndlessTerrain.WorldCoordToChunkCoordInt2(startPosWorld);
        float2 startPos = EndlessTerrain.WorldCoordToLocalCoord(startPosWorld);

        int2 endChunkCoord = EndlessTerrain.WorldCoordToChunkCoordInt2(endPosWorld);
        float2 endPos = EndlessTerrain.WorldCoordToLocalCoord(endPosWorld);

        GeneratePathJobInfo returnInfo = new GeneratePathJobInfo() {
            startPosWorld = startPosWorld,
            endPosWorld = endPosWorld,
            startPosLocal = (int2)startPos,
            endPosLocal = (int2)endPos,
            startChunkCoord = startChunkCoord,
            endChunkCoord = endChunkCoord,
            worldNavMeshWeightsMap = worldNavMeshWeightsMap,
            returnPath = returnPath,
            complete = complete
        };
        return returnInfo;
    }
    /// <summary>Finds the best path from start point to end point</summary>
    /// <param name="startPos">World Position of Start Point</param>
    /// <param name="endPos">World Position of End Point</param>
    /// <param name="navMesh">NavMesh</param>
    /// <returns>Priority Queue of fastest path to destination.</returns>
    public static void GeneratePath(GeneratePathJobInfo info) {
        float2 startPosWorld = info.startPosWorld;
        float2 endPosWorld = info.endPosWorld;
        int2 startPos = info.startPosLocal;
        int2 endPos = info.endPosLocal;
        int2 startChunkCoord = info.startChunkCoord;
        int2 endChunkCoord = info.endChunkCoord;
        NativeHashMap<int4, int2> worldNavMeshWeightsMap = info.worldNavMeshWeightsMap;
        NativeList<int4> returnPath = info.returnPath;

        int4 end = new int4(endChunkCoord, endPos);
        int4 start = new int4(startChunkCoord, startPos);

        float a = 1;
        float maxIterationsPerFrame = 1000;
        int maxDictSize = 100000;
        float distance = Heuristic(startPosWorld, endPosWorld);
        if (distance > 1000) {
            a = 3;
        }
        else if (distance > 750) {
            a = 2;
        }

        FastPriorityQueue<Node> frontier = new FastPriorityQueue<Node>(100000);

        Node startNode = new Node(start);
        frontier.Enqueue(startNode, 0);

        NativeHashMap<int4, int4> cameFrom = new NativeHashMap<int4, int4>();
        NativeHashMap<int4, float> costSoFar = new NativeHashMap<int4, float>();
        cameFrom.Add(start, new int4(-1, -1, -1, -1));
        costSoFar.Add(start, 0);

        bool foundPath = false;
        float iterationsThisFrame = 0;

        while (frontier.Count > 0) {
            int4 current = frontier.Dequeue().reference;
            if (current.Equals(end)) {
                foundPath = true;
                break;
            }

            if (cameFrom.Count() > maxDictSize) {
                Debug.LogWarning("Max path size reached: Destination not reached.");
                break;
            }

            //sooo... when one is the same distance from another and same weight (diagonal)
            //It seems to randomly pick which way to go, causing odd diagonals
            //To fix this I just add a teeny tiny bit each time, so it will always favor north (a teeny tiny bit).
            float teeny = 0.1f;
            NativeArray<int4> neighborCoords = new NativeArray<int4>(4, Allocator.Persistent);
            neighborCoords[0] = NavMesh.NorthPos(current, worldNavMeshWeightsMap);
            neighborCoords[1] = NavMesh.EastPos(current, worldNavMeshWeightsMap);
            neighborCoords[2] = NavMesh.SouthPos(current, worldNavMeshWeightsMap);
            neighborCoords[3] = NavMesh.WestPos(current, worldNavMeshWeightsMap);

            foreach (int4 neighborCoord in neighborCoords) {

                Node nextNode = new Node(neighborCoord);
                float costBetween = NavMesh.CostBetween(current, neighborCoord, worldNavMeshWeightsMap);
                float2 nextWorldCoord = EndlessTerrain.LocalCoordToWorldCoord(new Vector2(neighborCoord.z, neighborCoord.w), new Vector2(neighborCoord.x, neighborCoord.y));
                float newCost = costSoFar[current] + costBetween + a * Heuristic(nextWorldCoord, endPosWorld) + teeny;
                teeny += 0.1f;

                if (!costSoFar.ContainsKey(neighborCoord) && costBetween != 0) {
                    costSoFar[neighborCoord] = newCost;
                    frontier.Enqueue(nextNode, newCost);
                    cameFrom[neighborCoord] = current;
                    iterationsThisFrame += 1;
                }
            }
            if (iterationsThisFrame > maxIterationsPerFrame) {
                iterationsThisFrame = 0;
            }
            neighborCoords.Dispose();
        }

        if (foundPath) {
            int4 currQuad = end;
            while (!currQuad.Equals(start)) {
                returnPath.Add(currQuad);
                currQuad = cameFrom[currQuad];
            }
        }
        else {
            returnPath.Add(start);
        }
        info.complete[0] = true;
    }

    public static float Heuristic(float2 startPos, float2 endPos) {
        return math.distance(startPos, endPos);
    }
}


//This is to ENSURE that one node will never be in two queues at once. Not sure if this will work
//Hopefully garbage collection does the rest for us *shrug*
public class Node : FastPriorityQueueNode {
    public int4 reference;
    public Node(int4 pos) {
        this.reference = pos;
    }
}

public static class Path  {

    /*public void Initialize(Vector3 startPos, Vector3 endPos, Material testMat, bool debugPath) {
        lineDict = new Dictionary<NavQuad, Vector2>();
        next = new Dictionary<NavQuad, NavQuad>();
        hasPath = false;
        Vector2 startPos2D = new Vector2(startPos.x, startPos.z);
        Vector2 endPos2D = new Vector2(endPos.x, endPos.z);

        this.testMat = testMat;
        this.debugPath = debugPath;

        //When we move to jobs, make this thread close if a new path needs to be initialized while it's running.
        ThreadStart threadStart = delegate {
            AStar.GeneratePath(startPos2D, endPos2D, this);
        };
        new Thread(threadStart).Start();
    }*/

    //Get methods for line
    public static Vector2 GetLinePos(int x, int y, Dictionary<int2, Vector2> lineDict) {
        int2 pos = new int2(x, y);
        return lineDict[pos];
    }

    public static bool LineContains(int x, int y, Dictionary<int2, Vector2> lineDict) {
        int2 pos = new int2(x, y);
        return (lineDict.ContainsKey(pos));
    }

    /*void DebugLine(Line.LineSegment line) {
        
        LineRenderer lineRenderer = new GameObject("Line Renderer").AddComponent<LineRenderer>();
        lineRenderer.material = testMat;
        float height1 = EndlessTerrain.GetHeightFromMesh(line.start);
        float height2 = EndlessTerrain.GetHeightFromMesh(line.end);
        lineRenderer.widthMultiplier = 0.25f;
        lineRenderer.SetPosition(0, new Vector3(line.start.x, height1 + 0.5f, line.start.y));
        lineRenderer.SetPosition(1, new Vector3(line.end.x, height2 + 0.5f, line.end.y));
    }*/

    public static void GeneratePathLine(int4 end, Vector3 destination, NativeList<int4> quadPath, NativeHashMap<int4, int2> worldNavMeshWeightMap, NativeList<Line.LineSegment> returnLineList, LocalToWorld localToWorld) {

        //The goal is to go straight as long as possible.
        returnLineList.Add(GenerateIntersectLine(quadPath[0], quadPath[1], worldNavMeshWeightMap));
        int index = 0;
        //Generate Line as far as we can
        if (!quadPath[index + 1].Equals(end)) {
            LineGenRecursion(returnLineList, end, worldNavMeshWeightMap, quadPath, index, localToWorld);
        }
        //Debug.Log("Number of lines " + intersectLines.Count);

        //IF pathing is much better with both recursion methods, we will use both, but for speed right now we are just using method one which I prefer
        /*
        Line.LineSegment blankLine;
        blankLine.start = Vector2.zero;
        blankLine.end = Vector2.zero;
        if (LineGenRecursion2(0, x, y, end, lineDict, next, navMesh, northMesh, southMesh, eastMesh, westMesh, chunkCoord, blankLine, localToWorld) < intersectLines.Length) {
            //Debug.Log("Method2");
            lineDict.Clear();
            //Go in and set intersect position for each line in the list
            Line.LineSegment unitLine;
            float2 unitPos2D = new float2(localToWorld.Position.x, localToWorld.Position.z);
            unitLine.start = unitPos2D;
            int2 nextMostQuad = currQuad;
            //Make work for last case
            for (int i = 0; i < intersectLines.Length; i++) {
                nextMostQuad = next[nextMostQuad];
            }

            if (!nextMostQuad.Equals(end)) {
                //Makes unitLine point to closest Point on next line
                Line.LineSegment nextIntersectLine = GenerateIntersectLine(nextMostQuad.x, nextMostQuad.y, next, navMesh, northMesh, southMesh, eastMesh, westMesh, chunkCoord);
                Vector2 midPoint = (nextIntersectLine.start + nextIntersectLine.end) / 2;
                unitLine.end = Line.ClosestPoint(intersectLines[intersectLines.Length - 1], midPoint);
            }
            else { unitLine.end = new Vector2(destination.x, destination.z); }
            //if(debugPath) DebugLine(unitLine);

            int2 tickQuad = currQuad;
            foreach (Line.LineSegment intersectLine in intersectLines) {
                //if(debugPath) DebugLine(intersectLine);
                Vector2 point = Vector2.zero;
                if (Line.IsIntersecting(intersectLine, unitLine)) {
                    point = Line.GetIntersection(intersectLine, unitLine);
                }
                else {
                    point = Line.ClosestPoint(intersectLine, unitPos2D);
                }

                lineDict[tickQuad] = point;
                tickQuad = next[tickQuad];
            }
        }
        */
    }

    static void LineGenRecursion(NativeList<Line.LineSegment> returnList, int4 end, NativeHashMap<int4, int2> worldNavMeshWeightMap, NativeList<int4> returnPath, int index, LocalToWorld localToWorld) {

        //Method 1: Go to closest point at each next line, see if it can still draw a line.
        Line.LineSegment nextIntersectLine = GenerateIntersectLine(returnPath[index], returnPath[index + 1], worldNavMeshWeightMap);

        Line.LineSegment unitLine;
        float2 unitPos2D = new float2(localToWorld.Position.x, localToWorld.Position.z);
        unitLine.start = unitPos2D;
        unitLine.end = Line.ClosestPoint(nextIntersectLine, unitPos2D);

        Line.LineSegment lastIntersectLine = returnList[returnList.Length - 1];

        if (Line.IsIntersecting(unitLine, lastIntersectLine)) {
            returnList.Add(nextIntersectLine);
            if (!returnPath[index + 1].Equals(end)) {
                LineGenRecursion(returnList, end, worldNavMeshWeightMap, returnPath, index + 1, localToWorld);
            }
        }
    }

    static int LineGenRecursion2(int4 end, NativeHashMap<int4, int2> worldNavMeshWeightMap, Line.LineSegment initialLine, NativeList<float2> lineList, NativeList<int4> returnPath, LocalToWorld localToWorld, int index=0, int count = 0) {

        //Method 2: Follow angle from first move to nearest
        Line.LineSegment nextIntersectLine = GenerateIntersectLine(returnPath[index], returnPath[index + 1], worldNavMeshWeightMap);

        if (initialLine.start.x == 0 && initialLine.start.y == 0) {
            float2 unitPos2D = new float2(localToWorld.Position.x, localToWorld.Position.z);
            initialLine.start = unitPos2D;
            float3 endPos3D = localToWorld.Forward * 100;
            initialLine.end = new Vector2(endPos3D.x, endPos3D.z);
        }

        if (Line.IsIntersecting(nextIntersectLine, initialLine)) {
            lineList.Add(Line.GetIntersection(nextIntersectLine, initialLine));
            if (!returnPath[index + 1].Equals(end)) {
                return LineGenRecursion2(end, worldNavMeshWeightMap, initialLine, lineList, returnPath, localToWorld, index + 1, count + 1);
            }
        }
        return count;
    }

    static Line.LineSegment GenerateIntersectLine(int4 pos1, int4 pos2, NativeHashMap<int4, int2> worldNavMeshWeightMap) {
        Vector2 worldCoord = EndlessTerrain.LocalCoordToWorldCoord(new Vector2(pos1.z, pos1.w), new Vector2(pos1.x, pos1.y));

        Line.LineSegment intersectLine;

        if (pos2.Equals(NavMesh.NorthPos(pos1, worldNavMeshWeightMap))) {
            intersectLine.start = new Vector2(worldCoord.x + 0.5f, worldCoord.y);
            intersectLine.end = new Vector2(worldCoord.x + TerrainData.uniformScale - 0.5f, worldCoord.y);
        }
        else if (pos2.Equals(NavMesh.EastPos(pos1, worldNavMeshWeightMap))) {
            intersectLine.start = new Vector2(worldCoord.x + TerrainData.uniformScale, worldCoord.y - 0.5f);
            intersectLine.end = new Vector2(worldCoord.x + TerrainData.uniformScale, worldCoord.y - TerrainData.uniformScale + 0.5f);
        }
        else if (pos2.Equals(NavMesh.SouthPos(pos1, worldNavMeshWeightMap))) {
            intersectLine.start = new Vector2(worldCoord.x + 0.5f, worldCoord.y - TerrainData.uniformScale);
            intersectLine.end = new Vector2(worldCoord.x + TerrainData.uniformScale - 0.5f, worldCoord.y - TerrainData.uniformScale);
        }
        else if (pos2.Equals(NavMesh.WestPos(pos1, worldNavMeshWeightMap))) {
            intersectLine.start = new Vector2(worldCoord.x, worldCoord.y - 0.5f);
            intersectLine.end = new Vector2(worldCoord.x, worldCoord.y - TerrainData.uniformScale + 0.5f);
        }
        else {
            Debug.LogError("Pathing Error, next Quad is not N, S, E, or W");
            intersectLine.start = Vector2.zero;
            intersectLine.end = Vector2.zero;
        }
        return intersectLine;
    }

    /*
    public void DebugPath() {
        NavQuad currQuad = start;
        while (currQuad != end) {
            //FOR TESTING PURPOSES
            Vector2 chunkCoord = currQuad.GetParentNavMesh().coord;
            Vector2 worldCoord = EndlessTerrain.LocalCoordToWorldCoord(currQuad.pos, chunkCoord);

            if (next[currQuad] == currQuad.NorthQuad()) {
                Vector2 newCoord = new Vector2(worldCoord.x + 2.5f, worldCoord.y);
                float height = EndlessTerrain.GetHeightFromMesh(newCoord);
                Vector3 displayCoord = new Vector3(newCoord.x, height, newCoord.y);

                currQuad.DisplayNorth(displayCoord, testMat);
            }
            else if (next[currQuad] == currQuad.EastQuad()) {
                Vector2 newCoord = new Vector2(worldCoord.x + 5f, worldCoord.y - 2.5f);
                float height = EndlessTerrain.GetHeightFromMesh(newCoord);
                Vector3 displayCoord = new Vector3(newCoord.x, height, newCoord.y);

                currQuad.DisplayEast(displayCoord, testMat);
            }
            else if (next[currQuad] == currQuad.SouthQuad()) {
                Vector2 newCoord = new Vector2(worldCoord.x + 2.5f, worldCoord.y - 5f);
                float height = EndlessTerrain.GetHeightFromMesh(newCoord);
                Vector3 displayCoord = new Vector3(newCoord.x, height, newCoord.y);

                currQuad.DisplaySouth(displayCoord, testMat);
            }
            else if (next[currQuad] == currQuad.WestQuad()) {
                Vector2 newCoord = new Vector2(worldCoord.x, worldCoord.y - 2.5f);
                float height = EndlessTerrain.GetHeightFromMesh(newCoord);
                Vector3 displayCoord = new Vector3(newCoord.x, height, newCoord.y);

                currQuad.DisplayWest(displayCoord, testMat);
            }
            else {
                Debug.LogError("Pathing Error, next Quad is not N, S, E, or W");
            }
            currQuad = next[currQuad];
        }
    }*/
}