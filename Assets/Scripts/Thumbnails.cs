using UnityEngine;
using static TrackEditor;
using static UiController;

// TODO: ► Do automatic counting of categories instead of categoryIndex > 2 at /***/ mark

public class Thumbnails : MonoBehaviour
{
    static readonly Vector3 CameraOffset = new (-4, 8, -15);
    const float ThumbRelativeSize = .98f;
    static int _thumbSize;
    static int _thumbSpacing;
    static float _thumbHalfSize;

    public static void GeneratePartsThumbnails()  // Taking a screenshot of a camera's Render Texture: https://docs.unity3d.com/ScriptReference/Camera.Render.html
    {
        /*  Get parts  */
        var partsInstance = Instantiate(trackEditor.partsPrefab);

        /*  Calculate thumb size & thumb spacing  */
        float maxCount = 0;
        byte categoryIndex = 0;
        foreach (Transform category in partsInstance.transform) // Iterate over part categories
        {
            if (categoryIndex > 2) break;  /***/
            
            maxCount = Mathf.Max(maxCount, category.childCount);
            ++categoryIndex;
        }

        float thumbSizeWithSpacing = (Screen.width - 2) / maxCount;
        _thumbSize = (int)(thumbSizeWithSpacing * ThumbRelativeSize);
        _thumbSpacing = (int)(thumbSizeWithSpacing * (1 - ThumbRelativeSize));  // one spacing for each thumbnail
        _thumbHalfSize = (int)(_thumbSize / 2);

        /*  Create a render texture for the camera  */
        var renderTexture = new RenderTexture(_thumbSize, _thumbSize, 16)
        {
            antiAliasing = 8
        };

        /*  Create a camera for shooting partsPrefab  */
        var cameraThumb = new GameObject("cameraThumbnail", typeof(Camera));
        var cameraThumbCamera = cameraThumb.GetComponent<Camera>();
        cameraThumbCamera.targetTexture = renderTexture;

        categoryIndex = 0;
        byte partIndex = 0;
        foreach (Transform category in partsInstance.transform)  // Iterate over part categories
        {
            if (categoryIndex > 2) break;  // hotfix to ignore additional object in parts prefab /***/

            // PartCategories.Add(category);

            var categoryUiWrapper = new GameObject($"category_{category.name}");
            categoryUiWrapper.transform.SetParent(partThumbnailsWrapper.transform);

            byte partInCategoryIndex = 0;
            foreach (Transform partTransform in category)  // Iterate over parts in a category
            {
                Parts.Add(partTransform);

                /*  Set camera position & look at the part  */
                cameraThumb.transform.position = partTransform.position + CameraOffset;
                cameraThumb.transform.LookAt(partTransform.position);

                partTransform.gameObject.SetActive(true);  // Show the part for render shot
                RenderTexture.active = cameraThumbCamera.targetTexture;
                cameraThumbCamera.Render();

                /*  Create a new texture and read the active Render Texture into it.  */
                Texture2D texture = new (_thumbSize, _thumbSize);
                texture.ReadPixels(new Rect(0, 0, _thumbSize, _thumbSize), 0, 0);
                texture.Apply();

                partTransform.gameObject.SetActive(false);  // Hide the part after a shot is taken

                /*  Create a UI thumbnail (button with image) for each part  */
                var sprite = Sprite.Create(texture, new Rect(0, 0, _thumbSize, _thumbSize), Vector2.zero);
                byte index = partIndex;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093                

                new ImageButton(
                    $"buttonPartThumb_{partIndex}",
                    categoryUiWrapper,
                    5 + _thumbHalfSize + partInCategoryIndex * (_thumbSize + _thumbSpacing),
                    - (5 + _thumbHalfSize + categoryIndex * (_thumbSize + _thumbSpacing)) + Screen.height,
                    _thumbSize,
                    delegate {AddPart(new PartSaveData(index, 0, Coord.Null, 0));})
                .image.sprite = sprite;

                ++partInCategoryIndex;
                ++partIndex;
            }
            ++categoryIndex;
        }

        // UiController.partThumbnailsWrapper.GetComponent<RectTransform>().transform.localPosition = new(0, Screen.height - (3 * thumbSizeWithSpacing / 2)); /***/

        cameraThumbCamera.targetTexture = null;
        partsInstance.SetActive(false);
        Destroy(cameraThumb);
    }

    public static void GenerateSurfaceMaterialsThumbnails()
    {
        for (byte i = 0; i < trackEditor.surfaceMaterials.Count; ++i)
        {
            byte index = i;  // https://forum.unity.com/threads/addlistener-and-delegates-i-think-im-doing-it-wrong.413093
            print("_thumbSize: " + _thumbSize);
            var buttonThumb = new ImageButton(
                $"buttonSurfaceThumb_{i}",
                uiTrackEditor,
                5 + _thumbHalfSize + i * (_thumbSize + _thumbSpacing),
                _thumbSize * 1.5f + 2 * _thumbSpacing,  // TODO: This should be more simple, since it's just one line
                _thumbSize,
                delegate {ApplySurface(index);});

            if (i == 0)
                buttonThumb.image.color = Color.gray;
            else if (i == 1)
                buttonThumb.image.color = new (.43f, .19f, .06f);
        }
    }
}
