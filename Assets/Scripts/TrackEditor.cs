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

    // void FixedUpdate()
    // {}

    void GenerateThumbnails()
    {
        var thumbSize = 256;  // TODO: Should be relative to screen size
        var thumbSpacing = 10;  // Total space between two thumbnails
        
        // Create a render texture for the camera
        var renderTexture = new RenderTexture(thumbSize, thumbSize, 16)
        {
            antiAliasing = 2,
        };

        // Create a camera for shooting parts
        var cameraThumb = new GameObject("cameraThumbnail");
        cameraThumb.AddComponent<Camera>();
        var cameraThumbCamera = cameraThumb.GetComponent<Camera>();
        cameraThumbCamera.targetTexture = renderTexture;

        // Hide all parts, so just rendered object is visible at the moment
        foreach (Transform part in _parts)
        {
            part.gameObject.SetActive(false);
        }

        var i = 0;
        foreach (Transform part in _parts)
        {
            // Set camera position & look at the part
            cameraThumb.transform.position = part.position + new Vector3(-10, 10, -20);
            cameraThumb.transform.LookAt(part.position);

            part.gameObject.SetActive(true);
            var texture = RenderTextureImage(cameraThumbCamera);
            part.gameObject.SetActive(false);

            // Create a UI thumbnail image for each part
            var imageThumb = new GameObject("imageThumb");
            imageThumb.transform.SetParent(GameObject.Find("Canvas").transform);
            imageThumb.AddComponent<Image>();
            var imageThumbImage = imageThumb.GetComponent<Image>();
            var sprite = Sprite.Create(texture,
                new Rect(0, 0, thumbSize, thumbSize), new Vector2());
            imageThumbImage.sprite = sprite;

            ++i;

            // imageThumbImage.transform.position = new Vector3(i * thumbSize + 10, thumbSize + 10, 0);  // also works
            var rectTransform = imageThumb.GetComponent<RectTransform>();
            rectTransform.transform.position = new Vector3(i * (thumbSize + thumbSpacing) - thumbSize * .5f, thumbSize * .5f + thumbSpacing, 0);
            rectTransform.sizeDelta = new Vector2(thumbSize, thumbSize);
        }

        GameObject.Find("Parts").SetActive(false);

        /*  Take a "screenshot" of a camera's Render Texture.  */
        Texture2D RenderTextureImage(Camera cam)  //  https://docs.unity3d.com/ScriptReference/Camera.Render.html
        {
            // The Render Texture in RenderTexture.active is the one that will be read by ReadPixels.
            // var currentRenderTexture = RenderTexture.active;

            RenderTexture.active = cam.targetTexture;

            // Render the camera's view.
            cam.Render();

            // Make a new texture and read the active Render Texture into it.
            Texture2D texture = new Texture2D(thumbSize, thumbSize);
            texture.ReadPixels(new Rect(0, 0, thumbSize, thumbSize), 0, 0);
            texture.Apply();

            // Replace the original active Render Texture.
            // RenderTexture.active = currentRenderTexture;
            return texture;
        }
    }
}
