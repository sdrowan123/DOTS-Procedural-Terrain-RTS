using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Movable : MonoBehaviour {

    //ALRIGHT BITCH LISTEN UP
    // HERES WHAT YOURE DOING
    // 1. Make units entities
    // 2. Make entity system for those badboys with physics!
    // 3. redo all this shit and make job for generating path
    // 4. done, was that so hard you pu55y

    //Inspector variables
    public float maxSpeed;
    public float size;
    public bool displayPath;
    public bool debugPath;
    public Material testMat;

    //Constants
    const float accelerationCoeff = 8f;
    const float brakeDistance = 0.5f;
    const float stopDistance = 0.1f;
    const float rotationSpeed = 140f;
    const float keepUprightSpeed = 85f;
    const float slopeForceModifier = 5f;
    const float groundDrag = 0.05f;
    const float groundAngularDrag = 0.57f;
    const float airDrag = 0.00214f;
    const float airAngularDrag = 0.0143f;

    //Others
    float acceleration;
    int numTerrainTouching;
    [HideInInspector]
    public float speed;

    //Pathing variables
    Path path;
    Path newPath;
    NavQuad currQuad = null;
    [HideInInspector]
    public bool pathInstantiated = false;
    [HideInInspector]
    public bool hasPath = false;
    [HideInInspector]
    public bool generatingPath = false;
    [HideInInspector]
    public Vector3 destination;
    

    Rigidbody physics;
    CapsuleCollider collider;


    public void Start() {
        physics = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        
        physics.drag = airDrag * physics.mass;
        physics.angularDrag = airAngularDrag * physics.mass;
        speed = maxSpeed;
        acceleration = maxSpeed * accelerationCoeff;
        destination = transform.position;
    }

    public void FixedUpdate() {
        //Control for drag and gravity grounded vs ragdolling
        if (numTerrainTouching >= 1 && physics.velocity.magnitude <= maxSpeed * 1.5f) {
            physics.drag = groundDrag * physics.mass;
            physics.angularDrag = groundAngularDrag * physics.mass;
            physics.useGravity = false;
        }
        else {
            physics.drag = airDrag * physics.mass;
            physics.angularDrag = airAngularDrag * physics.mass;
            physics.useGravity = true;
        }

        //Our unit will try to stay upright whenever it is touching the ground
        if (numTerrainTouching >= 1 && physics.velocity.magnitude <= maxSpeed * 1.1f) {
            KeepUpright();
        }

        //Control for movement
        if (numTerrainTouching >= 1 && physics.velocity.magnitude <= maxSpeed * 1.5f) {
            if (generatingPath && newPath.hasPath) {
                path = newPath;
                if(debugPath) path.DebugPath();
                currQuad = path.start;
                hasPath = true;
                generatingPath = false;
            }
            //If we aren't close to our destination
            if (hasPath && currQuad != path.end) {
                MoveToNode(currQuad);
                if (NavQuadInRange(currQuad)) {
                    currQuad = path.next[currQuad];
                }
            }
            //If we are
            else if (Vector2.Distance (new Vector2(destination.x, destination.z), new Vector2(transform.position.x, transform.position.z)) > stopDistance){
                hasPath = false;
                MoveToDestination(destination, true);
            }
        }
    }


    //=================================================
    //       GoTos initialize class for movement
    //==================================================

    /// <summary>ONLY Method used for sending a unit to a destination</summary>
    /// <param name="destination">Vector3 in world coords</param>
    public void GoTo(Vector3 destination) {
        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(destination.x, destination.z)) > brakeDistance) {
            if (!pathInstantiated) {
                newPath = gameObject.AddComponent<Path>();
                path = gameObject.AddComponent<Path>();
                pathInstantiated = true;
            }
            if (Vector2.Distance(new Vector2(destination.x, destination.z), new Vector2(this.destination.x, this.destination.z)) > stopDistance) {
                //Debug.Log("Generating Path from: " + transform.position + " to " + destination);
                newPath.Initialize(transform.position, destination, testMat, debugPath);
                generatingPath = true;
            }
        }
        else {
            MoveToDestination(destination, true);
        }
        this.destination = destination;
    }

    /// <summary>Okay I lied, you can use this one too</summary>
    /// <param name="destination">Vector2 In world coords</param>
    public void GoTo(Vector2 destination) {
        float height = EndlessTerrain.GetHeightFromMesh(destination);
        GoTo(new Vector3(destination.x, height, destination.y));
    }


    //===============================================
    //          Local Movement Methods
    //===============================================

    static void MoveToNode(int x, int y, NativeArray<int2> navMesh) {
        //If route has been generated for this quad
        if (path.LineContains(quad)) {
            Vector2 dest2D = path.GetLinePos(quad);
            float height = EndlessTerrain.GetHeightFromMesh(dest2D);
            Vector3 localDestination = new Vector3(dest2D.x, height, dest2D.y);

            MoveToDestination(localDestination);
        }
        //else generate route
        else {
            path.GeneratePathLine(currQuad, destination);
        }
    }

    void MoveToDestination(Vector3 destination, bool final = false) {
        Vector3 position = transform.position;
        if (Vector3.Distance(position, destination) <= TerrainData.uniformScale * 2) {
            if (numTerrainTouching >= 1) {
                Vector3 lookVector = (destination - position).normalized;
                Quaternion lookRotation = Quaternion.FromToRotation(transform.forward, lookVector);
                float torque = rotationSpeed * physics.mass;
                physics.AddTorque(Vector3.up * lookRotation.y * torque * Time.fixedDeltaTime, ForceMode.VelocityChange);
                float force = physics.mass * acceleration;

                //Steepness modifier
                float steepness = EndlessTerrain.GetSteepnessFromMesh(new Vector2(transform.position.x, transform.position.z));
                force /= 1 + (steepness / slopeForceModifier);

                //Brake Modifier
                if (final) {
                    float distance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(destination.x, destination.z));
                    if (distance < brakeDistance) {
                        force /= brakeDistance / distance;
                    }
                }

                if (physics.velocity.magnitude < speed) {
                    physics.AddForceAtPosition(lookVector * force, new Vector3(transform.position.x, transform.position.y - collider.height / 16, transform.position.z));
                }
            }
        }
        else if (physics.velocity.sqrMagnitude < 0.05f) {
            Debug.Log("Unit too far from path");
            GoTo(this.destination);
        }
    }


    //==============================================
    //          Other local Physics methods
    //==============================================
    void KeepUpright() {
        Quaternion upQuat = Quaternion.FromToRotation(transform.up, Vector3.up).normalized;
        physics.AddTorque(new Vector3(upQuat.x, upQuat.y, upQuat.z) * keepUprightSpeed * physics.mass * Time.fixedDeltaTime, ForceMode.VelocityChange);
    }


    //==============================================
    //         Computatinal Helper Methods
    //==============================================

    bool NavQuadInRange(NavQuad quad) {
        float dist;
        if (path.LineContains(quad)) {
            Vector2 dest2D = path.GetLinePos(quad);
            float height = EndlessTerrain.GetHeightFromMesh(dest2D);
            dist = Vector3.Distance(transform.position, new Vector3(dest2D.x, height, dest2D.y));
            if (dist <= TerrainData.uniformScale / 10f + collider.height / 2) {
                return true;
            }
        }
        else {
            Vector3 quadPos = quad.ToWorldPos();
            dist = Vector3.Distance(transform.position, quadPos);
            if (dist <= TerrainData.uniformScale / 2f + collider.height / 2) {
                return true;
            }
            else { return false; }
        }
        return false;
    }

    /// <summary>Gets the Vector2 of where the unit is heading next. Must only be run once path is instantiated.</summary>
    public Vector2 GetNextDestination() {
        if (currQuad != path.end && path.next.ContainsKey(currQuad)) return path.next[currQuad].pos;
        return Vector2.zero;
    }


    //=============================================
    //          Terrain Touching Methods
    //=============================================

    private void OnCollisionEnter(Collision collision) {
        GameObject collisionObject = collision.gameObject;
        if (collisionObject.layer == 8) {
            numTerrainTouching += 1;
        }
    }

    private void OnCollisionExit(Collision collision) {
        GameObject collisionObject = collision.gameObject;
        if (collisionObject.layer == 8) {
            numTerrainTouching -= 1;
        }
    }
}
