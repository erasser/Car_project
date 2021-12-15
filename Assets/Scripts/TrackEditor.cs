using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TrackEditor : MonoBehaviour
{
    // private List<GameObject> _parts = new();
    // private List<Transform> _parts = new();
    private Transform _parts;

    void Start()
    {
        _parts = GameObject.Find("Parts").transform.Find("Category0").transform;
        GenerateThumbnails();
    }

    void FixedUpdate()
    {
        
    }

    void GenerateThumbnails()
    {
        // Create a render texture for the camera
        var renderTexture = new RenderTexture(256, 256, 16)
        {
            // antiAliasing = 2,
            enableRandomWrite = true
        };

        // Create a camera for shooting parts
        var cameraThumbnail = new GameObject("cameraThumbnail");
        cameraThumbnail.AddComponent<Camera>();
        var cameraThumbnailCamera = cameraThumbnail.GetComponent<Camera>();
        cameraThumbnailCamera.targetTexture = renderTexture;

        var i = 0;
        foreach (Transform part in _parts)
        {
            // Set camera position & look at the part
            cameraThumbnail.transform.position = part.position + new Vector3(-10, 10, -20);
            cameraThumbnail.transform.LookAt(part.position);

            var texture = RenderTextureImage(cameraThumbnailCamera);

            // Create a UI thumbnail image for each part
            var imageThumbnail = new GameObject("imageThumbnail");
            imageThumbnail.transform.SetParent(GameObject.Find("Canvas").transform);
            imageThumbnail.AddComponent<Image>();
            var imageThumbnailImage = imageThumbnail.GetComponent<Image>();
            var sprite = Sprite.Create(texture,
                new Rect(0, 0, cameraThumbnailCamera.targetTexture.width, cameraThumbnailCamera.targetTexture.height), new Vector2());
            imageThumbnailImage.sprite = sprite;

            imageThumbnail.transform.position = new Vector3(i * 110 + 60, 110);

            ++i;

            //button.GetComponent<RectTransform>().transform.position = new Vector3(childI++ * 50 + 50, catI * 50 + 200, 0);
        }
    }
    
    /** Take a "screenshot" of a camera's Render Texture. */
    Texture2D RenderTextureImage(Camera cam)  //  https://docs.unity3d.com/ScriptReference/Camera.Render.html
    {
        // The Render Texture in RenderTexture.active is the one that will be read by ReadPixels.
        // var currentRenderTexture = RenderTexture.active;

        RenderTexture.active = cam.targetTexture;

        // Render the camera's view.
        cam.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D texture = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        texture.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        texture.Apply();

        // Replace the original active Render Texture.
        // RenderTexture.active = currentRenderTexture;
        return texture;
    }
}
