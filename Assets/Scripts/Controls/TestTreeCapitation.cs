using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTreeCapitation : MonoBehaviour
{
    List<GameObject> selectables;
    // Start is called before the first frame update
    void Start()
    {
        selectables = FindObjectOfType<MouseHighlight>().selectedObjects;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("f")) {
            foreach (GameObject tree in selectables) {
                tree.GetComponent<FellTree>().Fell();
            }
        }
    }
}
