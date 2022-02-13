using UnityEngine;
using static TrackEditor;

public class Thumbnails : MonoBehaviour
{
    static GameObject _partsInstance;

    public static void GenerateThumbnails()  // Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html
    {
        const byte thumbSize = 120;   // TODO: Should be relative to screen size
        const float thumbSpacing = 3.5f;  // Total space between two thumbnails

        _partsInstance = Instantiate(trackEditor.partsPrefab);

        // Create a render texture for the camera
        var renderTexture = new RenderTexture(thumbSize, thumbSize, 16)
        {
            antiAliasing = 2,
        };

        // Create a camera for shooting partsPrefab
        var cameraThumb = new GameObject("cameraThumbnail", typeof(Camera));
        var cameraThumbCamera = cameraThumb.GetComponent<Camera>();
        cameraThumbCamera.targetTexture = renderTexture;

        byte categoryIndex = 0;
        byte partIndex = 0;
        foreach (Transform category in _partsInstance.transform)  // Iterate over part categories
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

        _partsInstance.SetActive(false);
        cameraThumb.SetActive(false);
        Grid3D.ToggleGridHelper();  // Shows grid
        Grid3D.SetBoundingBox();
        trackEditor.ground.transform.position = new Vector3(0, Grid3D.Bounds["min"].y + Grid3D.CubeSize - .05f, 0);
        trackEditor.ground.SetActive(true);
        selectionCube.SetActive(true);
        SetSelectionCoords(new Coord(1, 1, 1));
        OrbitCamera.Set(selectionCube.transform.position, 50, -30, 200);
        cameraEditor.SetActive(false);cameraEditor.SetActive(true);  // Something is fucked up, this is a hotfix
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
