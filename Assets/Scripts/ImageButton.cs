using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;

/// <summary>
/// Used to dynamically create image buttons. Only square buttons are supported at the moment.
/// ► OUTLINE IS NOT SUPPORTED, I GIVE UP. Maybe Image sprite could be used instead of material.
/// Outline material is shared among all outlined imageButtons, thickness is individual.
/// Outline can be performance consuming, since it creates individual GameObjects without GPU instancing support.
/// Once outline is added, it can't be removed using this class - since Destroy() can be called just from MonoBehaviours.
/// </summary>

public class ImageButton
{
    /*static readonly Material OutlineMaterial;
    public GameObject outline;*/
    public readonly GameObject gameObject;
    public readonly RectTransform rectTransform;
    public readonly Button button;
    public readonly Image image;

    /*static ImageButton()
    {
        OutlineMaterial = new Material(Shader.Find("GUI/Text Shader"));
        OutlineMaterial.color = Color.black;
    }*/

    public ImageButton(string buttonName, GameObject parent, float xPos, float yPos, float size, UnityAction callback/*, float outlineThickness = 0*/)
    {
        /*if (outlineThickness < 0)  // Zero is ok, means no outline
            Debug.LogWarning("ImageButton: Outline thickness must be greater than 0. No outline was added.");
        
        if (outlineThickness > 0)
        {
            outline = new GameObject($"outline_{buttonName}", typeof(Image));
            // outline.GetComponent<Renderer>().material = OutlineMaterial;
            // SetOutlineThickness(outlineThickness);
            // outline.transform.SetParent(parent.transform);
        }*/

        gameObject = new (buttonName, typeof(Button), typeof(Image));
        gameObject.transform.SetParent(parent.transform);

        rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.transform.position = new (xPos, yPos, 0);
        rectTransform.sizeDelta = new (size, size);

Debug.Log("size: " + size);  // TODO: Dořešit neviditelné buttony

        button = gameObject.GetComponent<Button>();
        image = gameObject.GetComponent<Image>();
        button.onClick.AddListener(callback);

        button.transition = Selectable.Transition.None;
        var nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
    }

    /*public static void SetOutlineColor(Color color)
    {
        OutlineMaterial.color = color;
    }

    /// <summary>
    /// Sets outline thickness.
    /// </summary>
    /// <param name="thickness">Value is added to scale = 1 and must be greater than 0.</param>
    public void SetOutlineThickness(float thickness)
    {
        if (thickness <= 0)
        {
            Debug.LogWarning("ImageButton: Outline thickness must be greater than 0. No outline was added.");
            return;
        }

        outline.transform.localScale = new(1 + thickness, 1 + thickness, 1);
    }*/
}
