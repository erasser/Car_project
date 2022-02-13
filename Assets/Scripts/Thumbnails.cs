using UnityEngine;
using static TrackEditor;

public class Thumbnails : MonoBehaviour
{
    public static void GenerateThumbnails()  // Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html
    {
        const byte thumbSize = 120;   // TODO: Should be relative to screen size
        const float thumbSpacing = 3.5f;  // Total space between two thumbnails

        var partsInstance = Instantiate(trackEditor.partsPrefab);

        // Create a render texture for the camera
        var renderTexture = new RenderTexture(thumbSize, thumbSize, 16)
        {
            antiAliasing = 8
        };

        // Create a camera for shooting partsPrefab
        var cameraThumb = new GameObject("cameraThumbnail", typeof(Camera));
        var cameraThumbCamera = cameraThumb.GetComponent<Camera>();
        cameraThumbCamera.targetTexture = renderTexture;

        byte categoryIndex = 0;
        byte partIndex = 0;
        foreach (Transform category in partsInstance.transform)  // Iterate over part categories
        {
            if (categoryIndex > 2) continue;  // hotfix to ignore additional object in parts prefab

            // PartCategories.Add(category);

            var categoryUiWrapper = new GameObject($"category_{category.name}");
            categoryUiWrapper.transform.SetParent(uiTrackEditor.transform);

            byte partInCategoryIndex = 0;  // TODO: Change to FOR loop
            foreach (Transform partTransform in category)  // Iterate over parts in a category
            {
                Parts.Add(partTransform);

                // Set camera position & look at the part
                cameraThumb.transform.position = partTransform.position + new Vector3(-4, 8, -15);
                cameraThumb.transform.LookAt(partTransform.position);

                partTransform.gameObject.SetActive(true);  // Show the part for render shot
                RenderTexture.active = cameraThumbCamera.targetTexture;
                cameraThumbCamera.Render();

                // Create a new texture and read the active Render Texture into it.
                var texture = new Texture2D(thumbSize, thumbSize);
                texture.ReadPixels(new Rect(0, 0, thumbSize, thumbSize), 0, 0);
                texture.Apply();

                partTransform.gameObject.SetActive(false);  // Hide the part after a shot is taken

                // Create a UI thumbnail (button with image) for each part
                var sprite = Sprite.Create(texture, new Rect(0, 0, thumbSize, thumbSize), Vector2.zero);
                byte index = partIndex;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093                

                new ImageButton(
                    $"buttonPartThumb_{partIndex}",
                    categoryUiWrapper,
                    partInCategoryIndex * (thumbSize + thumbSpacing) + thumbSize * .5f,
                    thumbSize * .5f + categoryIndex * (thumbSize + thumbSpacing),
                    thumbSize,
                    delegate {AddPart(new PartSaveData(index, 0, Coord.Null, 0));},
                    true)
                .image.sprite = sprite;

                ++partInCategoryIndex;
                ++partIndex;
            }
            ++categoryIndex;
        }

        cameraThumbCamera.targetTexture = null;
        partsInstance.SetActive(false);
        Destroy(cameraThumb);

        GameStateManager.Init();    // Initialization process continues here
    }

    public static void GenerateSurfaceMaterialsThumbnails()
    {
        const byte thumbSize = 120;
        const float thumbSpacing = 3.5f;

        for (byte i = 0; i < trackEditor.surfaceMaterials.Count; ++i)
        {
            byte index = i;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093

            var buttonThumb = new ImageButton(
                $"buttonSurfaceThumb_{i}",
                uiTrackEditor,
                i * (thumbSize + thumbSpacing) + thumbSize * .5f,
                thumbSize * 1.5f + 2 * thumbSpacing,
                thumbSize,
                delegate {ApplySurface(index);},
                true);

            if (i == 0)
                buttonThumb.image.color = Color.gray;
            else if (i == 1)
                buttonThumb.image.color = new (.43f, .19f, .06f);
        }
    }
}
