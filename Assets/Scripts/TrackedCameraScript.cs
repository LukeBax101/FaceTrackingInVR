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
	Thread newThread = null;
	bool finished = false;
	string positions = "";
	int count = 0;
	bool running = false;
	int[] intPos = { 0 };
	uint width = 0;
	uint height = 0;

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
		var client = new NamedPipeClientStream("OutgoingFrame");
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


	}

	public void doLoop() {
		var buf= new byte[200];
		int num = 0;
		intPos = new int[buf.Length / 4];
		while (finished == false) {
			//Debug.Log (finished);
			var line = reader.BaseStream.Read(buf,0,200);
			//Debug.Log(BitConverter.ToString(buf));
			//Debug.Log (buf [0]);
			num = 0;
			for (int i = 0; i < buf.Length; i = i + 4) {
				num = buf [i] + (buf [i + 1] << 8) + (buf [i + 2] << 16) + (buf [i + 3] << 24);
					//+ buf [i + 1] << 8 + buf [i + 2] << 16 + buf [i + 3] << 24;
				intPos[i/4] = num;
				//Debug.Log (num);
			}
			string[] stringArray = intPos.Select (i => i.ToString ()).ToArray ();
			//Debug.Log(string.Join(",",stringArray));
			//positions = System.Text.Encoding.ASCII.GetString (buf);
			Array.Clear(buf, 0, buf.Length);

				
		}
		newThread.Abort ();

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
		File.WriteAllBytes( "D:/Documents/University/CompSci/test1.png",bytes);



		if (connected) {
			/*Debug.Log (buffer.Length);
			Debug.Log (bytes [0]);
			Debug.Log (bytes [1]);
			Debug.Log (bytes [2]);
			Debug.Log (bytes [3]);
			Debug.Log (bytes [4]);
			Debug.Log (bytes [5]);
			Debug.Log (bytes [6]);
			Debug.Log (bytes [7]);
			Debug.Log (bytes [8]);
			Debug.Log (bytes [9]);
			Debug.Log (bytes [10]);
			Debug.Log (bytes [11]);
			Debug.Log (bytes [12]);
			Debug.Log (bytes [13]);
			Debug.Log (bytes [14]);
			Debug.Log (bytes [15]);
			Debug.Log (bytes [16]);
			Debug.Log (bytes [17]);
			Debug.Log (bytes [18]);
			Debug.Log (bytes [19]);
			Debug.Log (bytes [20]);
			Debug.Log (bytes [21]);
			Debug.Log (bytes [22]);
			Debug.Log (bytes [23]);
			Debug.Log (bytes [24]);
			Debug.Log (bytes [25]);
			Debug.Log (bytes [26]);
			Debug.Log (bytes [27]);*/


			/*Encoding unicode = Encoding.Unicode;
			char[] arrayChar = new char[unicode.GetCharCount (bytes, 0, bytes.Length)];
			unicode.GetChars (bytes, 0, bytes.Length, arrayChar, 0);
			string frameString = new string (arrayChar);
			writer.WriteLine (frameString);*/

			//writer.WriteLine (bytes);
			writer.BaseStream.Write(bytes, 0, bytes.Length);
			writer.Flush ();
			//Console.WriteLine (reader.ReadLine ());
		}
		//GetComponent<MeshRenderer>().material.mainTexture = texture;
		//mat.mainTexture = texture;
	}

	void onDestroy() {
		if (pTrackedCamera != 0) {
			trcam_instance.ReleaseVideoStreamingService(pTrackedCamera);
		}
	}

	void OnApplicationQuit() {
		Debug.Log ("End prog");
		finished = true;
	}

	void updatePositions(){
		int[] intPosClone = (int[])intPos.Clone ();
		float[] floatPos = new float[10];

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

		for (int i = 0; i < intPosClone [0]; i++) {
			//Debug.Log ("i: " +i);
			//Debug.Log ("First: " +intPosClone [0]);
			//Debug.Log ("Length:" + intPosClone.Length);
			int x = intPosClone [(4 * i) + 1];
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
			}




		}
		Transform sphere1 = GameObject.Find ("Sphere").GetComponent<Transform> ();
		Transform sphere2 = GameObject.Find ("Sphere (1)").GetComponent<Transform> ();
		Transform sphere3 = GameObject.Find ("Sphere (2)").GetComponent<Transform> ();
		Transform sphere4 = GameObject.Find ("Sphere (3)").GetComponent<Transform> ();
		Transform sphere5 = GameObject.Find ("Sphere (4)").GetComponent<Transform> ();

		if (floatPos [0] == 0) {
			sphere1.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere1.localPosition = new Vector3 (floatPos [0], -floatPos [1], 1.0f);
		}

		if (floatPos [2] == 0) {
			sphere2.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere2.localPosition = new Vector3 (floatPos [2], -floatPos [3], 1.0f);
		}

		if (floatPos [4] == 0) {
			sphere2.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere2.localPosition = new Vector3 (floatPos [4], -floatPos [5], 1.0f);
		}
		if (floatPos [6] == 0) {
			sphere2.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere2.localPosition = new Vector3 (floatPos [6], -floatPos [7], 1.0f);
		}
		if (floatPos [8] == 0) {
			sphere2.localPosition = new Vector3 (0, 0, -1.0f);
		} else {
			sphere2.localPosition = new Vector3 (floatPos [8], -floatPos [9], 1.0f);
		}

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


	void Update()
	{
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
