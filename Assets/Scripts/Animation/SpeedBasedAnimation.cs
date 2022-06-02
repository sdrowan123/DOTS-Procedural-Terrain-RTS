using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedBasedAnimation : MonoBehaviour
{
    //This is a placeholder for a better animation controller
    Animator animator;
    public float maxSpeed;
    public Rigidbody physics;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        animator.SetFloat("speedPercent", physics.velocity.magnitude / maxSpeed);
    }
}
