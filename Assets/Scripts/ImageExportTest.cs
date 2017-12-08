using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ImageExportTest : MonoBehaviour {

	public Material material;
	public Transform target;
	public bool undistorted = true;
	public bool cropped = true;




	void SaveTextureToFile(Texture2D tex,string fileName)
	{
		var bytes = tex.EncodeToPNG();

		//var file = Application.OpenURL(Application.dataPath + "/" +fileName);
		//var binary = new BinaryWriter(file);
		//binary.Write(bytes);
		File.WriteAllBytes( "D:/Documents/University/CompSci/" +fileName,bytes);


	}

	void OnEnable()
	{
		var source = SteamVR_TrackedCamera.Source(undistorted);
		source.Acquire();

		if (!source.hasCamera)
			enabled = false;
	}

	void OnDisable()
	{
		material.mainTexture = null;

		var source = SteamVR_TrackedCamera.Source (undistorted);
		source.Release ();

	}
	public void Released()
	{
		Debug.Log ("Release him!!");
		//var source = SteamVR_TrackedCamera.Source(undistorted);
		//var tex = material.GetTexture("_MainTex");
		var colBytes = SteamVR_TrackedCamera.Source(undistorted).texture.GetPixels32();
		var bytes = new byte[] {colBytes [0].a, colBytes [0].r, colBytes [0].g, colBytes [0].g};
		//tex.EncodeToJPG()
		//var bytes = tex.EncodeToPNG();
		File.WriteAllBytes( "D:/Documents/University/CompSci/test1.txt",bytes);
	}
	// Update is called once per frame
	void Update () {
		var source = SteamVR_TrackedCamera.Source(undistorted);

		//SaveTextureToFile (source.texture, "test1.png");
		//Debug.Log("Here: " + source.texture);
	}
}

