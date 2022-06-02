using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using UnityEditor;
using System.Xml.Schema;
using System;
using UnityEngine.UI;

public class CameraMovement : MonoBehaviour {

	//Camera Inspector Vars
	public float cameraSpeed;
	public float rotateVelocity;
	public int maxZoom;
	public int minZoom;
	public int zoomInterval;
	public float zoomVelocity;
	public float zoomDistance = 1000;
	public float startAngle;
	public AnimationCurve distanceCurve;
	public GameObject gameCamera;
	public GameObject test;

	float camAngle;
	Vector3 zoomPos;
	bool coroutineRunning = false;
	int rotateAxisDirection = 0;

	void Start(){camAngle = startAngle;}

	void Update () {
		//Move camera
		transform.Translate (new Vector3(1, 0, 0) * Input.GetAxis ("Horizontal") * Time.deltaTime * cameraSpeed * distanceCurve.Evaluate(GetCameraDistance ()/maxZoom));
		transform.Translate (new Vector3(0, 0, 1) * Input.GetAxis ("Vertical") * Time.deltaTime * cameraSpeed * distanceCurve.Evaluate(GetCameraDistance ()/maxZoom));

		//Zoom Out
		if(Input.GetAxis ("ScrollWheel") > 0){
			float curve = distanceCurve.Evaluate (GetCameraDistance () / maxZoom);
			Vector3 potentialZoomPos = (transform.position + -transform.forward * (zoomDistance - zoomInterval * curve)*Mathf.Sqrt(3)/2) + transform.up * (zoomDistance - zoomInterval * curve)/2;
			if (GetCameraDistance (potentialZoomPos) >= minZoom) zoomDistance = Mathf.RoundToInt(zoomDistance - zoomInterval * curve);
		}
		//Zoom in
		if(Input.GetAxis ("ScrollWheel") < 0){
			float curve = distanceCurve.Evaluate (GetCameraDistance () / maxZoom);
			Vector3 potentialZoomPos = (transform.position + -transform.forward * (zoomDistance + zoomInterval * curve)*Mathf.Sqrt(3)/2) + transform.up * (zoomDistance + zoomInterval * curve)/2;
			if (GetCameraDistance (potentialZoomPos) <= maxZoom) zoomDistance = Mathf.RoundToInt(zoomDistance + zoomInterval * curve);
		}

		//Rotate
		if(Input.GetAxis ("Rotate") < 0 && rotateAxisDirection == 0) rotateAxisDirection = -1;
		if(Input.GetAxis ("Rotate") > 0 && rotateAxisDirection == 0) rotateAxisDirection = 1;
		if(Input.GetAxisRaw("Rotate") == 0 && rotateAxisDirection != 0){
			StartCoroutine (QueueRoutine (Rotate (rotateAxisDirection)));
			rotateAxisDirection = 0;
		}

		//Define zoom distance to adjust camera position, keeps it same distance from ground as we move along terrain
		float meshY = EndlessTerrain.GetHeightFromMesh(new Vector2(transform.position.x, transform.position.z));
		float deltaY = meshY - transform.position.y;
		transform.position = new Vector3(transform.position.x, meshY, transform.position.z);
		gameCamera.transform.position = new Vector3 (gameCamera.transform.position.x, gameCamera.transform.position.y - deltaY, gameCamera.transform.position.z);
		zoomDistance = (zoomDistance - deltaY/Mathf.Sin(Mathf.Deg2Rad * camAngle));

		//Changes camera angle when close up:
		float currCamAngle = gameCamera.transform.eulerAngles.x;
		if (zoomDistance < 85) camAngle = 20;
		else if (zoomDistance < 125) {
			float newRange = startAngle - 20;
			float oldRange = 125 - 85;
			camAngle = (((zoomDistance - 85) * newRange) / oldRange) + 20;
		}else camAngle = startAngle;

		zoomPos = (transform.position + -transform.forward * zoomDistance*Mathf.Cos(Mathf.Deg2Rad * camAngle)) + transform.up * zoomDistance*Mathf.Sin(Mathf.Deg2Rad * camAngle);

		//Finally, Adjust
		Quaternion toRotation = gameCamera.transform.rotation;
		toRotation *= Quaternion.AngleAxis(camAngle - currCamAngle, Vector3.right);
		gameCamera.transform.rotation = Quaternion.Lerp(gameCamera.transform.rotation, toRotation, Time.deltaTime * zoomVelocity * 1F);
		gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, zoomPos, Time.deltaTime * zoomVelocity);
		test.transform.position = new Vector3(transform.position.x, meshY, transform.position.z);
	}


	//==============================
	//		Coroutines
	//===============================

	IEnumerator QueueRoutine(IEnumerator coroutine){
		if (coroutineRunning) yield return new WaitForSeconds (0.001f);
		else StartCoroutine (coroutine);
	}

	IEnumerator Rotate(int direction){
		coroutineRunning = true;
		Quaternion toRotationValue = transform.rotation;
		float fraction = 0;

		if(direction > 0) toRotationValue *= Quaternion.AngleAxis(45, Vector3.up);
		else if(direction < 0) toRotationValue *= Quaternion.AngleAxis(-45, Vector3.up);

		while (fraction < 1) {
			fraction += Time.deltaTime * rotateVelocity;
			transform.rotation = Quaternion.Lerp(transform.rotation, toRotationValue, fraction);
			yield return null;
		}
		coroutineRunning = false;
	}

	//Deprecated
	IEnumerator Zoom(){
		coroutineRunning = true;
		Transform cam = gameCamera.transform;
		float fraction = 0;
	
		while (fraction < 1) {
			fraction += Time.deltaTime * zoomVelocity;
			cam.position = Vector3.Lerp (cam.position, zoomPos, fraction);
			yield return null;
		}
		coroutineRunning = false;
	}


	//=======================================
	//			Helper Methods
	//=======================================
	float GetCameraDistance() {
		Transform cam = gameCamera.transform;
		Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, Mathf.Infinity, 1 << 8);
		return hit.distance;
	}

	float GetCameraDistance(Vector3 newPos) {
		Transform cam = gameCamera.transform;
		Physics.Raycast(newPos, cam.forward, out RaycastHit hit, Mathf.Infinity, 1 << 8);
		return hit.distance;
	}
}
