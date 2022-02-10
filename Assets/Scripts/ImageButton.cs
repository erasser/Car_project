using UnityEngine;
using UnityEngine.Events;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;
using Outline = UnityEngine.UI.Outline;

/// <summary>
/// Used to dynamically create image buttons. Only square buttons are supported at the moment.
/// </summary>

// TODO: Try to support click listener?
// TODO: Support outline

public class ImageButton
{
    public readonly GameObject gameObject;
    public readonly RectTransform rectTransform;
    public readonly Button button;
    public readonly Image image;
    public Outline outline;

    public ImageButton(string buttonName, GameObject parent, float xPos, float yPos, float size, UnityAction callback, bool addOutline = false)
    {
        gameObject = new GameObject(buttonName, typeof(Button), typeof(Image)/*, typeof(Outline)*/);
        gameObject.transform.SetParent(parent.transform);

        rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.transform.position = new (xPos, yPos, 0);
        rectTransform.sizeDelta = new (size, size);

        button = gameObject.GetComponent<Button>();
        image = gameObject.GetComponent<Image>();
        button.onClick.AddListener(callback);
        
        if (addOutline)
        {
            gameObject.AddComponent<Outline>();
            outline = gameObject.GetComponent<Outline>();
        }
    }
}
