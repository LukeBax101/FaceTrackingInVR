using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViveControllerInput : MonoBehaviour {

	public TrackedCameraScript script1;
	public GameObject planeLeft;
	public GameObject planeRight;
	public GameObject camPlane;
	// 1
	private bool showPlanes = true;
	private bool testMode = false;
	private SteamVR_TrackedObject trackedObj;
	// 2
	private SteamVR_Controller.Device Controller
	{
		get { return SteamVR_Controller.Input((int)trackedObj.index); }
	}
	void Awake()
	{
		trackedObj = GetComponent<SteamVR_TrackedObject>();
	}
	// Update is called once per frame
	void Update () {
		// 1
		if (Controller.GetPressDown (SteamVR_Controller.ButtonMask.Touchpad)) {
			if (Controller.GetAxis ().y > 0) {
				Debug.Log ("Tear Down");
				script1.tearDown ();
			} else {
				//Debug.Log ("Start Test");
				if (testMode == false) {
					
					script1.testStart (trackedObj.gameObject.name);
					testMode = true;
				} else {
					script1.testStop ();
					testMode = false;
				}
			}

		}

		//if (Controller.GetAxis() != Vector2.zero)
		//{
			//Debug.Log(gameObject.name + Controller.GetAxis());
		//}

		// 2
		if (Controller.GetHairTriggerDown())
		{
			//Debug.Log(gameObject.name + " Trigger Press");
		}

		// 3
		if (Controller.GetHairTriggerUp())
		{
			script1.toggleOn();
		}

		// 4
		if (Controller.GetPressDown(SteamVR_Controller.ButtonMask.Grip))
		{
			//Debug.Log(gameObject.name + " Grip Press");
		}

		// 5
		if (Controller.GetPressUp(SteamVR_Controller.ButtonMask.Grip))
			
		{
			planeLeft.SetActive (!showPlanes);
			planeRight.SetActive (!showPlanes);
			camPlane.SetActive (!showPlanes);
			showPlanes = !showPlanes;
			Debug.Log(gameObject.name + " Grip Release");
		}

		if (Controller.GetPressDown (SteamVR_Controller.ButtonMask.ApplicationMenu)) {
			//Debug.Log ("Application Button Pressed");

		}

	}
}
