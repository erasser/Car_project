using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html

public class TrackEditor : MonoBehaviour
{
    [SerializeField]
    private GameObject parts;

    void Start()
    {
        // parts = GameObject.Find("Parts").transform.Find("Category0").transform;  // Now it's a prefab
        GenerateThumbnails();
    }

    // void FixedUpdate()
    // {}

    void GenerateThumbnails()
    {
        var thumbSize = 256;  // TODO: Should be relative to screen size
        var thumbSpacing = 10;  // Total space between two thumbnails
        var rectSize = new Vector2(thumbSize, thumbSize);

        var partsInstance = Instantiate(parts);
        
        var partsCategory0 = partsInstance.transform.Find("Category0").transform;
        
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

        var i = 0;
        foreach (Transform part in partsCategory0)
        {
            // Set camera position & look at the part
            cameraThumb.transform.position = part.position + new Vector3(-10, 10, -20);
            cameraThumb.transform.LookAt(part.position);

            part.gameObject.SetActive(true);  // Show the part for render shot
            RenderTexture.active = cameraThumbCamera.targetTexture;
            cameraThumbCamera.Render();

            // Create a new texture and read the active Render Texture into it.
            var texture = new Texture2D(thumbSize, thumbSize);
            texture.ReadPixels(new Rect(0, 0, thumbSize, thumbSize), 0, 0);
            texture.Apply();

            part.gameObject.SetActive(false);  // Hide the part after shot is taken

            // Create a UI thumbnail image for each part
            var imageThumb = new GameObject("imageThumb");
            imageThumb.transform.SetParent(GameObject.Find("Canvas").transform);
            imageThumb.AddComponent<Image>();
            var imageThumbImage = imageThumb.GetComponent<Image>();
            var sprite = Sprite.Create(texture, new Rect(0, 0, thumbSize, thumbSize), Vector2.zero);
            imageThumbImage.sprite = sprite;

            // imageThumbImage.transform.position = new Vector3(i * thumbSize + 10, thumbSize + 10, 0);  // also works
            var rectTransform = imageThumb.GetComponent<RectTransform>();
            rectTransform.transform.position = new Vector3(i * (thumbSize + thumbSpacing) + thumbSize * .5f, thumbSize * .5f + thumbSpacing, 0);
            rectTransform.sizeDelta = rectSize;

            ++i;
        }

        partsInstance.SetActive(false);
    }
}
