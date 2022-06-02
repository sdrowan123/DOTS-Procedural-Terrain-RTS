using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragYArrowAnimation : MonoBehaviour
{

    public GameObject upArrow;
    public GameObject downArrow;
    public float speed;
    public float maxDistance;
    float currDistance;

    Transform upTransform;
    Transform downTransform;

    void Start() {
        currDistance = 0;

        upTransform = upArrow.GetComponent<RectTransform>();
        downTransform = downArrow.GetComponent<RectTransform>();
    }

    private void OnEnable() {
        Update();
    }

    // Update is called once per frame
    void Update() {
        if (currDistance > maxDistance) {
            currDistance = 0;
            speed = -speed;
        }

        currDistance += Mathf.Abs(speed) * Time.deltaTime;
        upTransform.position = new Vector2(upTransform.position.x, upTransform.position.y + speed * Time.deltaTime);
        downTransform.position = new Vector2(downTransform.position.x, downTransform.position.y - speed * Time.deltaTime);
    }
}
