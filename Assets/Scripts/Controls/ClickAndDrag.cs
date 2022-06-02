using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickAndDrag
{
    //!!! COnsider adding RESET INSTEAD OF RESETTING EACH TIMe. CONSIDER IT

    private readonly int mouseButton;
    private readonly float targetDistance;
    Vector2 currPosition = new Vector2(0, 0);
    Vector2 initialPosition;
    
    /// <summary>Click And Drag via screen space. FrameUpdate() must be called every frame.</summary>
    /// <param name="distance">Desired distance of drag</param>
    /// <param name="rightClick">Use right click instead of left click?</param>
    public ClickAndDrag(float distance, bool rightClick=false) {
        mouseButton = (rightClick) ? 1 : 0;
        targetDistance = distance;
    }

    /// <summary>Updates the object. Must be called every frame.</summary>
    public void FrameUpdate() {
        if (Input.GetMouseButton(mouseButton)){
            if(currPosition == new Vector2(0,0)) initialPosition = Input.mousePosition;
            currPosition = Input.mousePosition;
        }
        else currPosition = new Vector2(0,0);
    }

    /// <summary>Checks if Distance has been met in the +y direction.</summary>
    public bool PositiveY() {
        if (Input.GetMouseButton(mouseButton)) { 
            if(currPosition.y - initialPosition.y >= targetDistance) {
                initialPosition = currPosition;
                return true;
            }
        }
        return false;
    }

    /// <summary>Checks if Distance has been met in the -y direction.</summary>
    public bool NegativeY() {
        if (Input.GetMouseButton(mouseButton)) {
            if (initialPosition.y - currPosition.y >= targetDistance) {
                initialPosition = currPosition;
                return true;
            }
        }
        return false;
    }

    /// <summary>Checks if Distance has been met in the +x direction.</summary>
    public bool PositiveX() {
        if (Input.GetMouseButton(mouseButton)) {
            if (currPosition.x - initialPosition.x >= targetDistance) {
                initialPosition = currPosition;
                return true;
            }
        }
        return false;
    }

    /// <summary>Checks if Distance has been met in the -x direction.</summary>
    public bool NegativeX() {
        if (Input.GetMouseButton(mouseButton)) {
            if (initialPosition.x - currPosition.x >= targetDistance) {
                initialPosition = currPosition;
                return true;
            }
        }
        return false;
    }
}
