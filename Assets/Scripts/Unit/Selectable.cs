using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EPOOutline;


//DEPRECATED UPDATE WITH FASTER OUTLINE SHADER
public class Selectable : MonoBehaviour
{
    [HideInInspector]
    public bool highlighted;
    [HideInInspector]
    public bool selected;

    public bool playableUnit;
    public bool movableUnit;
    public int cursorHighlightOverride = 0;
    public Color outlineColor;

    bool initialized = false;
    Outlinable outlineable;

    float minAlbedo = 0f;
    float maxAlbedo = 1f;
    float pulseSpeed = 1f;
    bool pulsing = false;

    //This script needs special treatment since it will be on a LOT of entities
    //So we will initialize only when we need to in order to reduce loading times.
    void Initialize() {
        initialized = true;
        /*outlineable = gameObject.AddComponent<Outlinable>();
        outlineable.RenderStyle = RenderStyle.FrontBack;
        outlineable.FrontParameters.Color = outlineColor;
        outlineable.BackParameters.Enabled = false;
        outlineable.AddAllChildRenderersToRenderingList();
        ToggleOutlines(false);*/
    }

    void ToggleOutlines(bool on) {
        outlineable.enabled = on;
    }

    IEnumerator Pulse() {
        float nextAlbedo;
        Color newColor;
        while (pulsing) {
            newColor = outlineable.FrontParameters.Color;
            nextAlbedo = newColor.a + pulseSpeed * Time.deltaTime;
            if(nextAlbedo < minAlbedo || nextAlbedo > maxAlbedo) {
                pulseSpeed = -pulseSpeed;
                nextAlbedo = newColor.a + pulseSpeed * Time.deltaTime;
            }
            newColor.a = nextAlbedo;
            outlineable.FrontParameters.Color = newColor;
            yield return null;
        }
    }

    public void OutlineOn() {
        if (!initialized) Initialize();
        outlineable.FrontParameters.Color = outlineColor;
        ToggleOutlines(true);
    }

    public void OutlineOff() {
        if (!initialized) Initialize();
        ToggleOutlines(false);
    }

    public void OutlinePulseOn() {
        if (!initialized) Initialize();
        if (!pulsing) {
            pulsing = true;
            StartCoroutine(Pulse());
            ToggleOutlines(true);
        }
    }

    public void OutlinePulseOff() {
        if (!initialized) Initialize();
        StopCoroutine(Pulse());
        pulsing = false;
        ToggleOutlines(false);
    }
}
