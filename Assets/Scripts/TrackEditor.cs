using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackEditor : MonoBehaviour
{
    private GameObject _camera;

    void Start()
    {
        _camera = GameObject.Find("Camera0");
        _camera.SetActive(false);
        print(_camera);

        var image = RTImage(_camera.GetComponent<Camera>());
print(image);
        GameObject.Find("RawImage").GetComponent<RawImage>().texture = image;
    }

    void FixedUpdate()
    {
        
    }
    
    // Take a "screenshot" of a camera's Render Texture.
    Texture2D RTImage(Camera cam)
    {
        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        // Render the camera's view.
        cam.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;
        return image;
    }
}
