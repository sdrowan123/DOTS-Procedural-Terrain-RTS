using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Log : MonoBehaviour
{
    //It's Log!
    bool hasLimbs = true;
    Rigidbody physics;
    void Start()
    {
        physics = GetComponent<Rigidbody>();
        Vector3 fellDirection = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        fellDirection = fellDirection.normalized;
        float torqueHeight = GetComponent<BoxCollider>().size.y * transform.localScale.y;
        physics.AddForceAtPosition(fellDirection * 10000, new Vector3(transform.position.x, transform.position.y + torqueHeight, transform.position.z));
    }

    void Update()
    {
        if (hasLimbs && physics.velocity.magnitude < 0.05f) StartCoroutine(DeLimb());
    }

    IEnumerator DeLimb() {
        yield return new WaitForSeconds(0.5f);
        foreach(Transform child in transform) {
            child.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.025f);
        }
        hasLimbs = false;
    }
}
