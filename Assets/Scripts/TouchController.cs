using UnityEngine;
using UnityEngine.EventSystems;
// See UFO to implement touch
// TODO: ► Process only if screen is touched? Přece musí jít ty eventy pověsit nějak na celý okno hry

public class TouchController : MonoBehaviour
{
    private static Vector3 _touchPosition;  // screen coordinates for touch  https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
    private static Transform _dummyTransform;
    private static TouchState _touchState = TouchState.NoTouch;
    private static ControllerState _controllerState = ControllerState.NoAction;
    private static Vector3 _touchDownPosition;
    private static Vector3 _touchUpPosition;
    // private static bool _wasDownOnUI;
    private static bool _wasUpOnUI;

    enum TouchState
    {
        NoTouch,
        TouchedDown,
        TouchedUp,
        DoubleTouch
    }

    enum ControllerState
    {
        NoAction,
        Panning,
        Rotating
    }

    void Start()
    {
        _dummyTransform = GameObject.Find("dummy").transform;
    }

    void Update()
    {
        ProcessTouch();
    }
    
    void ProcessTouch()
    {

        MouseDown();
        MouseUp();

        // Now touching
        if (_touchState == TouchState.TouchedDown)
        {
            var diff = Input.mousePosition - _touchPosition;
            // Now moving with touch
            if (diff.sqrMagnitude > 0)  // TODO: Add some value for small difference
            {
                _controllerState = ControllerState.Rotating;

                var translationV3 = diff * Time.deltaTime * 4;
                _dummyTransform.Translate(new Vector3(translationV3.x, translationV3.z, 0));
                // _dummyTransform.Rotate(new Vector3(translationV3.y, translationV3.x, 0));
            }
        }
        // Now double (or more) touching
        // else if (_touchState == TouchState.TouchedDown)
        // {
        //     var diff = Input.mousePosition - _touchPosition;
        //     // Now moving with touch
        //     if (diff.sqrMagnitude > 0)  // TODO: Add some value for small difference
        //     {
        //         _controllerState = ControllerState.Rotating;
        //
        //         var translationV3 = diff * Time.deltaTime * 4;
        //         _dummyTransform.Translate(new Vector3(translationV3.x, translationV3.z, 0));
        //         // _dummyTransform.Rotate(new Vector3(translationV3.y, translationV3.x, 0));
        //     }
        // }

        if (_touchState == TouchState.TouchedUp)
        {
            // Finished touch without camera pan or rotation => raycast
            if (_controllerState == ControllerState.NoAction && !_wasUpOnUI)
            {
                TrackEditor.Instance.ProcessSimpleTouch();
            }
            else
            {
                _controllerState = ControllerState.NoAction;
            }
            _touchState = TouchState.NoTouch;
        }
        _touchPosition = Input.mousePosition;
    }

    // Is reset each frame
    private void MouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject())  // UI
            return;

        if (Input.GetMouseButtonDown(0))
        {
            _touchState = TouchState.TouchedDown;
            
            if (Input.GetMouseButtonDown(1))
                _touchState = TouchState.DoubleTouch;
        }
    }

    // Is reset each frame
    private void MouseUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _touchState = TouchState.TouchedUp;
            _wasUpOnUI = EventSystem.current.IsPointerOverGameObject();
        }
    }

    // private Vector3 ScreenToScenePosition(Vector3 screenPosition)
    // {
    //     return new Vector3(diff.x, diff.z, 0);
    // }
}
