using UnityEngine;
using System.Collections;
using Valve.VR;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;



public class TrackedCameraScript : MonoBehaviour {

	public uint index;
	public Material mat;
	public GameObject leftConnectedText;
	public GameObject rightConnectedText;
	public GameObject leftTrackingText;
	public GameObject rightTrackingText;

	CVRTrackedCamera trcam_instance = null;
	ulong pTrackedCamera = 0;
	IntPtr pBuffer = (IntPtr)null;
	byte[] buffer = null;
	uint buffsize = 0;
	Texture2D texture = null;
	CameraVideoStreamFrameHeader_t pFrameHeader;
	EVRTrackedCameraError camerror = EVRTrackedCameraError.None;
	uint prevFrameSequence = 0;
	StreamReader reader = null;
	StreamWriter writer = null;
	bool connected = false;
	bool connectedIn = false;
	NamedPipeClientStream server = null;
	NamedPipeClientStream client = null;
	Thread newThread = null;
	bool finished = false;
	string positions = "";
	int count = 0;
	bool running = false;
	int[] intPos = { 0 };
	uint width = 0;
	uint height = 0;
	bool closed = false;
	//int[] intPos = new int[200/ 4];

	void Start () {
		bool pHasCamera = false;

		trcam_instance = OpenVR.TrackedCamera;

		if(trcam_instance == null) {
			Debug.LogError("Error getting TrackedCamera");
		} else {			
			camerror = trcam_instance.HasCamera ((uint)index, ref pHasCamera);
			if(camerror != EVRTrackedCameraError.None) {
				Debug.LogError("HasCamera: EVRTrackedCameraError="+camerror);
				return;
			}
			if (pHasCamera) {
				camerror = trcam_instance.GetCameraFrameSize ((uint)index, EVRTrackedCameraFrameType.Undistorted, ref width, ref height, ref buffsize);
				if (camerror != EVRTrackedCameraError.None) {
					Debug.LogError("GetCameraFrameSize: EVRTrackedCameraError=" + camerror);
				} else {
					Debug.Log("width=" + width + " height=" + height + " buffsize=" + buffsize);
					texture = new Texture2D ((int)width, (int)height, TextureFormat.RGBA32, false);

					buffer = new byte[buffsize];
					pBuffer = Marshal.AllocHGlobal ((int)buffsize);

					camerror = trcam_instance.AcquireVideoStreamingService ((uint)index, ref pTrackedCamera);
					if (camerror != EVRTrackedCameraError.None) {
						Debug.LogError("AcquireVideoStreamingService: EVRTrackedCameraError=" + camerror);
					}
				}
			} else {
				Debug.Log("no camera found, only Vive Pre and later supported");
			}
		}

		//Client
		client = new NamedPipeClientStream("OutgoingFrame");
		//var server = new NamedPipeClientStream (".", "IncdmingData", PipeDirection.InOut, PipeOptions.Asynchronous);
		server = new NamedPipeClientStream ("IncomingData");


		try {
			client.Connect();
			//reader = new StreamReader(client);
			writer = new StreamWriter(client,Encoding.UTF8);
			connected = true;
			Debug.Log("Connected Out");

		} catch (Exception to) {
			Debug.Log("Cannot connect out!");
		}

		try {
			server.Connect();
			reader = new StreamReader(server);
			connectedIn = true;
			Debug.Log("Connected In");
		} catch (Exception to) {
			Debug.Log("Cannot connect in!");
		}

		newThread = new Thread(doLoop);
		newThread.Start ();

		if (connected & connectedIn) {
			Debug.Log ("Both are connected");
			leftConnectedText.GetComponent<TextMesh> ().text = "IPC pipe connection to python established.";
			rightConnectedText.GetComponent<TextMesh> ().text = "IPC pipe connection to python established.";
		} else {
			Debug.Log ("Connection Failed");
			leftConnectedText.GetComponent<TextMesh> ().text = "Connection Failed... no pipes found.";
			rightConnectedText.GetComponent<TextMesh> ().text = "Connection Failed... no pipes found.";
		}
			


	}

	public void doLoop() {
		var buf= new byte[200];
		int num = 0;
		intPos = new int[buf.Length / 4];
		while (finished == false) {
			//Debug.Log (finished);
			try {
			var line = reader.BaseStream.Read(buf,0,200);
			}
			catch (Exception ex) {
				Debug.Log ("Error Reading");
			}
			if (checkEnd (buf)) {
				Debug.Log ("Return teardown heard");
				finished = true;
			} else {
				//Debug.Log(BitConverter.ToString(buf));
				//Debug.Log (buf [0]);
				num = 0;
				for (int i = 0; i < buf.Length; i = i + 4) {
					num = buf [i] + (buf [i + 1] << 8) + (buf [i + 2] << 16) + (buf [i + 3] << 24);
					//+ buf [i + 1] << 8 + buf [i + 2] << 16 + buf [i + 3] << 24;
					intPos [i / 4] = num;
					//Debug.Log (num);
				}
				string[] stringArray = intPos.Select (i => i.ToString ()).ToArray ();
				//Debug.Log(string.Join(",",stringArray));
				//positions = System.Text.Encoding.ASCII.GetString (buf);
				Array.Clear (buf, 0, buf.Length);
			}

				
		}
		Debug.Log ("Closing reader and server");
		try {
			reader.Close ();
			Debug.Log("Now closing server");
			server.Close ();
		} catch (Exception) {
			Debug.Log ("Failed closing reader and server");
		}
		
		Debug.Log ("Reader and server closed");

		//UnityEditor.EditorApplication.isPlaying = false;
		newThread.Abort ();
		Debug.Log ("Thread aborted");

	}

	public void readIn() {
		var buf= new byte[200];
		int num = 0;

		try {
			Debug.Log("Attempting to read");
			var line = reader.BaseStream.Read(buf,0,200);
			Debug.Log("Successful read");

			if (checkEnd (buf)) {
				Debug.Log ("Return teardown heard");
				finished = true;
			} else {
				//Debug.Log(BitConverter.ToString(buf));
				//Debug.Log (buf [0]);
				num = 0;
				for (int i = 0; i < buf.Length; i = i + 4) {
					num = buf [i] + (buf [i + 1] << 8) + (buf [i + 2] << 16) + (buf [i + 3] << 24);
					//+ buf [i + 1] << 8 + buf [i + 2] << 16 + buf [i + 3] << 24;
					intPos [i / 4] = num;
					//Debug.Log (num);
				}
				string[] stringArray = intPos.Select (i => i.ToString ()).ToArray ();
				Debug.Log(string.Join(",",stringArray));
				//positions = System.Text.Encoding.ASCII.GetString (buf);
				Array.Clear (buf, 0, buf.Length);
			}
				

		}
		catch (Exception ex) {
			Debug.Log ("Skipped read");
		}




		

	}

	public bool checkEnd (byte[] buf) {
		var returnEndMessage = Encoding.ASCII.GetBytes("Return tear down");
		//Debug.Log (returnEndMessage [0]);
		bool correct = true;
		for (int i = 0; i < 16; i++) {
			
			if (buf [i*4] != returnEndMessage [i]) {
				correct = false;
			}
		}
		return correct;
	}


	public void tearDown() {
		Debug.Log ("Teardown Connection");

		var endMessage = Encoding.ASCII.GetBytes("Tear Down Connection");

		writer.BaseStream.Write(endMessage, 0, endMessage.Length);
		writer.Flush ();
		Debug.Log ("Sent");
		//finished = true;


	}

	public void toggleOn() {
		running = !running;
	}


	public void Save () {
		// first get header only
		//Debug.Log("Triggered!");
		camerror = trcam_instance.GetVideoStreamFrameBuffer(pTrackedCamera,  EVRTrackedCameraFrameType.Undistorted, (IntPtr)null, 0, ref pFrameHeader, (uint)Marshal.SizeOf(typeof(CameraVideoStreamFrameHeader_t)));
		if(camerror != EVRTrackedCameraError.None) {
//			Debug.LogError("GetVideoStreamFrameBuffer: EVRTrackedCameraError="+camerror);
			return;
		}
		//if frame hasn't changed don't copy buffer
		if (pFrameHeader.nFrameSequence == prevFrameSequence) {
			return;
		}
		// now get header and buffer
		camerror = trcam_instance.GetVideoStreamFrameBuffer(pTrackedCamera,  EVRTrackedCameraFrameType.Undistorted, pBuffer, buffsize, ref pFrameHeader, (uint)Marshal.SizeOf(typeof(CameraVideoStreamFrameHeader_t)));
		if(camerror != EVRTrackedCameraError.None) {
			Debug.LogError("GetVideoStreamFrameBuffer: EVRTrackedCameraError="+camerror);
			return;
		}
		prevFrameSequence = pFrameHeader.nFrameSequence;

		Marshal.Copy(pBuffer, buffer, 0, (int)buffsize);
		texture.LoadRawTextureData(buffer);
		texture.Apply();
		var bytes = texture.EncodeToPNG();
		//File.WriteAllBytes( "D:/Documents/University/CompSci/test1.png",bytes);



		if (connected && finished == false) {
			


			/*Encoding unicode = Encoding.Unicode;
			char[] arrayChar = new char[unicode.GetCharCount (bytes, 0, bytes.Length)];
			unicode.GetChars (bytes, 0, bytes.Length, arrayChar, 0);
			string frameString = new string (arrayChar);
			writer.WriteLine (frameString);*/

			//writer.WriteLine (bytes);
			try {
				writer.BaseStream.Write (bytes, 0, bytes.Length);
				writer.Flush ();
			}
			catch (Exception ex) {
				Debug.Log ("Error Writing");
			}

			//Console.WriteLine (reader.ReadLine ());
		} /*else if (connected && finished == true && closed == false) {
			Debug.Log ("Closing writer and client");
			writer.Close ();
			client.Close ();
			closed = true;
			Debug.Log ("Closed, aborting...");
			UnityEditor.EditorApplication.isPlaying = false;
			//REPLACE WITH THIS IN BUILD
			//Application.Quit();
		}*/
		//GetComponent<MeshRenderer>().material.mainTexture = texture;
		//mat.mainTexture = texture;
	}

	void onDestroy() {
		if (pTrackedCamera != 0) {
			trcam_instance.ReleaseVideoStreamingService(pTrackedCamera);
		}
	}

	void OnApplicationQuit() {
		Debug.Log ("Closing writer and client");
		writer.Close ();
		client.Close ();
		Debug.Log ("Closed, aborting...");
		Debug.Log ("End prog");
		finished = true;
	}

	void updatePositions(){
		//readIn ();
		int[] intPosClone = (int[])intPos.Clone ();
		float[] floatPos = new float[20];

		/*if (intPosClone [0] == 0) {
			floatPos [0] = 0f;
			floatPos [1] = 0f;
			floatPos [2] = 0f;
			floatPos [3] = 0f;
		}
		if (intPosClone [0] == 1) {
			floatPos [2] = 0f;
			floatPos [3] = 0f;
		}*/

		for (int i = 0; i < (intPosClone [0]); i++) {
			/*int x = intPosClone [(4 * i) + 1];
			int y = intPosClone [(4 * i) + 2];
			int w = intPosClone [(4 * i) + 3];
			int h = intPosClone [(4 * i) + 4];
			float xf = x + (w / 2);
			float yf = y + (h / 2);
			xf = (xf - (width / 2)) / width;
			yf = (yf - (height / 2)) /height;
			//Debug.Log (xf);
			//Debug.Log (yf);
			if (i <5) {
				floatPos [(2*i)] = xf;
				floatPos [(2 * i) + 1] = yf;
			}*/


			float xf = Convert.ToSingle(intPosClone [(4 * i) + 1]);
			float yf = Convert.ToSingle(intPosClone [(4 * i) + 2]);
			float d = Convert.ToSingle(intPosClone [(4 * i) + 3])/100f;
			float s = Convert.ToSingle(intPosClone [(4 * i) + 4])/100f;

			xf = (xf - (width / 2)) / width;
			yf = (yf - (height / 2)) /height;
			if (i <5) {
				floatPos [(4*i)] = xf;
				floatPos [(4 * i) + 1] = yf;
				floatPos [(4 * i) + 2] = d;
				floatPos [(4 * i) + 3] = s;

			}




		}
		Transform sphere1 = GameObject.Find ("Sphere").GetComponent<Transform> ();
		Transform sphere2 = GameObject.Find ("Sphere (1)").GetComponent<Transform> ();
		Transform sphere3 = GameObject.Find ("Sphere (2)").GetComponent<Transform> ();
		Transform sphere4 = GameObject.Find ("Sphere (3)").GetComponent<Transform> ();
		Transform sphere5 = GameObject.Find ("Sphere (4)").GetComponent<Transform> ();

		Transform cap1 = GameObject.Find ("Capsule").GetComponent<Transform> ();
		Transform cap2 = GameObject.Find ("Capsule (1)").GetComponent<Transform> ();
		Transform cap3 = GameObject.Find ("Capsule (2)").GetComponent<Transform> ();
		Transform cap4 = GameObject.Find ("Capsule (3)").GetComponent<Transform> ();
		Transform cap5 = GameObject.Find ("Capsule (4)").GetComponent<Transform> ();

		Transform camTrans = GameObject.Find ("Camera (eye)").GetComponent<Transform> ();


		float distance = 2f;
		float size = 1f;


		float scale = 1.5f;


		Transform[] sphereArray = {sphere1,sphere2,sphere3,sphere4,sphere5};

		Transform[] capArray = {cap1,cap2,cap3,cap4,cap5};


		for (int f = 0; f < 5; f++) {

			if (floatPos [f*4] == 0) {
				sphereArray[f].localPosition = new Vector3 (0, 0, -1.0f);
			} else {
				sphereArray[f].localPosition = new Vector3 (floatPos [f*4]*scale, -floatPos [(f*4)+1]*scale, 1.0f);
			}

			placeFace (sphereArray[f], capArray[f], camTrans, floatPos [(f*4)+2], floatPos [(f*4)+3]);

		}

		
		/*


		if (floatPos [0] == 0) {
			sphere1.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere1.localPosition = new Vector3 (floatPos [0]*scale, -floatPos [1]*scale, 1.0f);
		}
			
		placeFace (sphere1, cap1, camTrans, floatPos [2], floatPos [3]);

		

		if (floatPos [4] == 0) {
			sphere2.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere2.localPosition = new Vector3 (floatPos [2]*scale, -floatPos [3]*scale, 1.0f);
		}

		placeFace (sphere2, cap2, camTrans, distance, size);



		if (floatPos [4] == 0) {
			sphere3.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere3.localPosition = new Vector3 (floatPos [4]*scale, -floatPos [5]*scale, 1.0f);
		}

		placeFace (sphere3, cap3, camTrans, distance, size);



		if (floatPos [6] == 0) {
			sphere4.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere4.localPosition = new Vector3 (floatPos [6]*scale, -floatPos [7]*scale, 1.0f);
		}

		placeFace (sphere4, cap4, camTrans, distance, size);



		if (floatPos [8] == 0) {
			sphere5.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere5.localPosition = new Vector3 (floatPos [8]*scale, -floatPos [9]*scale, 1.0f);
		}

		placeFace (sphere5, cap5, camTrans, distance, size);*/

		/* Debug.Log("String: [" + string.Join(",",stringPos)+"]");
		//for (int i = 0; i < stringPos.Length; i++) {
			//Debug.Log ("String Pos: " + stringPos [i]);
			//if (stringPos [i] == "") {
				//floatPos [i] = 0;
			//} else {
			//	floatPos [i] = float.Parse (stringPos [i]);
		//	}
		


		//}*/
		//Debug.Log("Floats: [" +string.Join(",",floatPos)+"]");


	}

	void placeFace(Transform sphere, Transform cap, Transform cam,float distance,float size){
		Vector3 dir1 = (sphere.position - cam.position);
		dir1.Normalize ();
		dir1.Scale (new Vector3 (distance, distance, distance));
		Vector3 facePos = cam.position + (dir1);
		cap.position = facePos;
		cap.localScale = new Vector3 (size * 0.2f, size * 0.15f, size * 0.2f);

	}

	void resetPositions() {
		Transform sphere1 = GameObject.Find ("Sphere").GetComponent<Transform> ();
		Transform sphere2 = GameObject.Find ("Sphere (1)").GetComponent<Transform> ();
		Transform sphere3 = GameObject.Find ("Sphere (2)").GetComponent<Transform> ();
		Transform sphere4 = GameObject.Find ("Sphere (3)").GetComponent<Transform> ();
		Transform sphere5 = GameObject.Find ("Sphere (4)").GetComponent<Transform> ();

		Transform cap1 = GameObject.Find ("Capsule").GetComponent<Transform> ();
		Transform cap2 = GameObject.Find ("Capsule (1)").GetComponent<Transform> ();
		Transform cap3 = GameObject.Find ("Capsule (2)").GetComponent<Transform> ();
		Transform cap4 = GameObject.Find ("Capsule (3)").GetComponent<Transform> ();
		Transform cap5 = GameObject.Find ("Capsule (4)").GetComponent<Transform> ();

		Transform camTrans = GameObject.Find ("Camera (eye)").GetComponent<Transform> ();

		Transform[] sphereArray = {sphere1,sphere2,sphere3,sphere4,sphere5};
		Transform[] capArray = {cap1,cap2,cap3,cap4,cap5};

		for (int f = 0; f < 5; f++) {
			sphereArray [f].localPosition = new Vector3 (0f, 0f, -1f);
			placeFace (sphereArray [f], capArray [f], camTrans, 1f, 1f);
		}
		



	}
		

	void Update()
	{
		if (finished) {
			//UnityEditor.EditorApplication.isPlaying = false;
			//REPLACE WITH THIS IN BUILD
			Application.Quit();
		}

		if (running) {
			if (count == 0) {
			
				Save ();
				count = 10;
			} else {
				count--;
			}

			//TextMesh textObject = GameObject.Find ("FacePosition").GetComponent<TextMesh> ();
			updatePositions();
			//textObject.text = positions;
		} else {
			resetPositions ();
		}
		

		if (connected & connectedIn) {
			if (running) {
				leftTrackingText.GetComponent<TextMesh> ().text = "Face Tracking: ON   (Toggle with trigger)";
				rightTrackingText.GetComponent<TextMesh> ().text = "Face Tracking: ON   (Toggle with trigger)";
			} else {
				leftTrackingText.GetComponent<TextMesh> ().text = "Face Tracking: OFF   (Toggle with trigger)";
				rightTrackingText.GetComponent<TextMesh> ().text = "Face Tracking: OFF   (Toggle with trigger)";
			}

		} else {
			leftTrackingText.GetComponent<TextMesh> ().text = "Cannot track faces without python connection.";
			rightTrackingText.GetComponent<TextMesh> ().text = "Cannot track faces without python connection.";


		}

		//Debug.Log ("Started Update");
		//var buf= new byte[20];
		//reader.BaseStream.BeginRead(buf,0,20,null,null);
		/*if (connectedIn) {
			if (!reader.EndOfStream) {
				Debug.Log ("not ended");
			}
		} else {
			Debug.Log ("Not connected");
		}*/

		//reader.BaseStream.Read(buf,0,20);
		//Debug.Log (buf);
		//var line = reader.ReadToEnd();
		//if (reader.Peek () > -1) {
			//var line = reader.ReadLine ();
		
		//Debug.Log ("not empty");
		//}

	}
		
}
