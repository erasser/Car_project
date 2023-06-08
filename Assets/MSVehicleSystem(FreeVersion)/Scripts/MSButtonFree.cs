using UnityEngine.EventSystems;
using UnityEngine;

public class MSButtonFree : MonoBehaviour, /*IPointerDownHandler, IPointerUpHandler,*/ IPointerEnterHandler, IPointerExitHandler {

	[HideInInspector]
	public float buttonInput;
	[Tooltip("The sensitivity perceived by the script when pressing the button.")]
	public float sensibility = 3;
	bool pressing;

	// public void OnPointerDown(PointerEventData eventData){
	// 	pressing = true;
	// }

	// public void OnPointerUp(PointerEventData eventData){
	// 	pressing = false;
	// }

	public void OnPointerEnter(PointerEventData eventData)
	{
		pressing = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pressing = false;
	}

	void Update () {
		if (pressing) {
			buttonInput += Time.deltaTime * sensibility;
		} else {
			buttonInput -= Time.deltaTime * sensibility;
		}
		buttonInput = Mathf.Clamp (buttonInput, 0, 1);
	}
}