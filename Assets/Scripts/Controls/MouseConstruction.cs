using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class MouseConstruction : MouseMode
{
    [Range(1, 9)]
    public int dim = 1;
    EndlessTerrain endlessTerrain;
    GameObject meshObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    GameObject shadowMeshObject;
    MeshRenderer shadowMeshRenderer;
    MeshFilter shadowMeshFilter;

    public Material material;
    public Material shadowMaterial;

    public float updateDistance;
    Vector2 lastPos;

    Vector3[] vertices;
    MeshData meshData;
    Mesh uiMesh;
    public float hoverHeight = 0.3F;

    //For animation
    public Texture2D[] frames;
    public int fps = 10;

    ClickAndDrag mouseDrag;

    public int cursorOverride;

    // Start is called before the first frame update
    void Start()
    {
        mouseDrag = new ClickAndDrag(15);
        meshObject = new GameObject("MouseConstructionUI");
        //meshObject.transform.parent = transform;
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer.material = material;
        meshObject.transform.position += new Vector3(0, hoverHeight, 0);

        shadowMeshObject = new GameObject("MouseConstructionUIShadow");
        shadowMeshObject.transform.position += new Vector3(0, 0.01F, 0);
        //meshObject.transform.parent = transform;
        shadowMeshRenderer = shadowMeshObject.AddComponent<MeshRenderer>();
        shadowMeshFilter = shadowMeshObject.AddComponent<MeshFilter>();
        shadowMeshRenderer.material = shadowMaterial;

        uiMesh = new Mesh();
        lastPos = new Vector2(0, 0);
        endlessTerrain = FindObjectOfType<EndlessTerrain>();

        UpdateMesh(new Vector2(0, 0));
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 point = MouseWorldPos();
        //test.transform.position = hit.point;

        float xDiff = Mathf.Abs(point.x - lastPos.x);
        float zDiff = Mathf.Abs(point.z - lastPos.y);
        if (xDiff > updateDistance || zDiff > updateDistance) {
            if (!Input.GetMouseButton(0)) {
                lastPos = new Vector2(point.x, point.z);
                UpdateMesh(new Vector2(point.x, point.z));
            }
        }

        if (Input.GetMouseButton(0)) {
            MouseCursor.ChangeCursor(cursorOverride);
        }
        else {
            MouseCursor.DefaultCursor();
        }

        mouseDrag.FrameUpdate();
        if (mouseDrag.PositiveY()) {
            UpMesh();
            SendMeshToChunk();
        }

        if (mouseDrag.NegativeY()) {
            DownMesh();
            SendMeshToChunk();
        }

        //Animation Stuff
        float index = Time.time * fps;
        index = index % frames.Length;
        meshRenderer.material.mainTexture = frames[(int)index];
    }

    void SendMeshToChunk() {
        //endlessTerrain.UpdateSingleChunkMesh(vertices, 0, MapGenerator.TerrainData.maxSteepness);
    }

    void ApplyMesh(Vector3[] vertices, Vector2[] uvs, int[] triangles) {
        uiMesh.vertices = vertices;
        uiMesh.uv = uvs;
        uiMesh.triangles = triangles;
        meshFilter.mesh = uiMesh;
        shadowMeshFilter.mesh = uiMesh;
        uiMesh.RecalculateNormals();
        uiMesh.RecalculateBounds();
    }

    void UpdateMesh(Vector2 pos) {

        Mesh mesh = EndlessTerrain.GetSubMeshFromMesh(pos, dim);
        vertices = mesh.vertices;
        ApplyMesh(mesh.vertices, mesh.uv, mesh.triangles);
    }

    void UpMesh() {
        float lowestHeight = uiMesh.vertices[0].y;
        List<int> lowestHeightIndices = new List<int>();
        //Puts lowest vertices of mesh up one and updates
        for(int i = 0; i < uiMesh.vertices.Length; i++) {
            if(uiMesh.vertices[i].y < lowestHeight) {
                lowestHeightIndices.Clear();
                lowestHeightIndices.Add(i);
                lowestHeight = uiMesh.vertices[i].y;
            }else if(uiMesh.vertices[i].y == lowestHeight) {
                lowestHeightIndices.Add(i);
            }
        }
        foreach(int index in lowestHeightIndices) {
            vertices[index].y += MapGenerator.TerrainData.heightRound * TerrainData.uniformScale;
        }
        ApplyMesh(vertices, uiMesh.uv, uiMesh.triangles);
    }

    void DownMesh() {
        float highestHeight = uiMesh.vertices[0].y;
        List<int> highestHeightIndices = new List<int>();
        //Puts lowest vertices of mesh up one and updates
        for (int i = 0; i < uiMesh.vertices.Length; i++) {
            if (uiMesh.vertices[i].y > highestHeight) {
                highestHeightIndices.Clear();
                highestHeightIndices.Add(i);
                highestHeight = uiMesh.vertices[i].y;
            }
            else if (uiMesh.vertices[i].y == highestHeight) {
                highestHeightIndices.Add(i);
            }
        }
        foreach (int index in highestHeightIndices) {
            vertices[index].y -= MapGenerator.TerrainData.heightRound * TerrainData.uniformScale;
        }
        ApplyMesh(vertices, uiMesh.uv, uiMesh.triangles);
    }
}
