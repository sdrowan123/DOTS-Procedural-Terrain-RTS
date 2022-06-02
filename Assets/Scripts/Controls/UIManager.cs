using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class UIManager : MonoBehaviour
{
    //TODO CHANGE MOUSECURSOR SO ITS FOR EVERY MODE, NOT A SEPERATE THING
    GameObject[] modes;
    public int defaultMode;
    int currentMode;

    //Class
    private void Start() {
        currentMode = defaultMode;
        modes = GameObject.FindGameObjectsWithTag("UI Mode");
        foreach(GameObject mode in modes) {
            mode.SetActive(false);
        }
        modes[defaultMode].SetActive(true);
    }

    public void ChangeMode(int mode) {
        modes[currentMode].SetActive(false);
        modes[mode].SetActive(true);
        currentMode = mode;
    }

    public void offModes() {
        modes[currentMode].SetActive(false);
    }

    public void onModes() {
        modes[currentMode].SetActive(true);
    }
}
