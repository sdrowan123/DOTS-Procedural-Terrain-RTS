using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class CursorOrbAnimation : MonoBehaviour
{
    //Public variables
    public GameObject orb;
    public GameObject orbShadow;
    public float maxDistance;
    public float speed;
    public float rotationSpeed;

    //Local Vars
    float currDistance;
    bool goingUp;
    RectTransform orbTransform;
    RectTransform shadowTransform;

    void Start()
    {
        orbTransform = orb.GetComponent<RectTransform>();
        shadowTransform = orbShadow.GetComponent<RectTransform>();

        goingUp = true;
        currDistance = 0;
    }

    private void OnEnable() {
        Update();
    }

    void Update()
    {
        if(Math.Abs(currDistance) > maxDistance) {
            goingUp = !goingUp;
        }

        if (goingUp) {
            currDistance += speed * Time.deltaTime;
            orbTransform.position = new Vector2(orbTransform.position.x - speed * Time.deltaTime, orbTransform.position.y + speed * Time.deltaTime * 2.5f);
            shadowTransform.position = new Vector2(orbTransform.position.x - speed * Time.deltaTime, orbTransform.position.y + speed * Time.deltaTime * 2.5f);
        }
        else {
            currDistance -= speed * Time.deltaTime;
            orbTransform.position = new Vector2(orbTransform.position.x + speed * Time.deltaTime, orbTransform.position.y - speed * Time.deltaTime * 2.5f);
            shadowTransform.position = new Vector2(orbTransform.position.x + speed * Time.deltaTime, orbTransform.position.y - speed * Time.deltaTime * 2.5f);
        }

        orbTransform.Rotate(0.0f, 0.0f, Time.deltaTime * rotationSpeed, Space.Self);
    }
}
