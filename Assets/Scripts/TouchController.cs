using UnityEngine;
using UnityEngine.EventSystems;

// See UFO to implement touch
// TODO: ► Process only if screen is touched? Přece musí jít ty eventy pověsit nějak na celý okno hry

public class TouchController : MonoBehaviour
{
    private static Vector3 _lastMousePosition;  // screen coordinates for touch  https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
    private static TouchState _touchState = TouchState.NoTouch;
    private static ControllerState _controllerState = ControllerState.NoAction;
    private static int _scrollValue;
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

    void Update()
    {
        ProcessTouch();
    }
    
    void ProcessTouch()
    {

        CheckMouseDown();
        CheckMouseUp();
        CheckScroll();

        // Now touching => orbit camera
        if (_touchState == TouchState.TouchedDown)
        {
            var touchPositionDiff = Input.mousePosition - _lastMousePosition;
            // Now moving with touch
            if (touchPositionDiff.sqrMagnitude > 0)  // TODO: Add some value for small difference?
            {
                _controllerState = ControllerState.Rotating;
                OrbitCamera.Orbit(touchPositionDiff);
            }
        }
        // Now double (or more) touching => pan camera      // TODO: Limit camera target position to grid bounds
        else if (_touchState == TouchState.DoubleTouch)
        {
            var touchPositionDiff = Input.mousePosition - _lastMousePosition;
            // Now moving with double touch
            if (touchPositionDiff.sqrMagnitude > 0)  // TODO: Add some value for small difference?
            {
                _controllerState = ControllerState.Panning;
                OrbitCamera.Pan(touchPositionDiff);
            }
        }

        if (_scrollValue != 0)
        {
            OrbitCamera.Zoom(_scrollValue);
        }

        if (_touchState == TouchState.TouchedUp)
        {
            // Finished touch without camera pan or rotation => raycast
            if (_controllerState == ControllerState.NoAction && !_wasUpOnUI)
            {
                TrackEditor.instance.ProcessSimpleTouch();
            }
            else
            {
                _controllerState = ControllerState.NoAction;
            }
            _touchState = TouchState.NoTouch;
        }
        _lastMousePosition = Input.mousePosition;
    }

    // Is reset each frame
    private void CheckMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return; // UI

        if (Input.GetMouseButtonDown(0))
        {
            _touchState = TouchState.TouchedDown;
        }
        if (Input.GetMouseButtonDown(1) && _touchState == TouchState.TouchedDown)
            _touchState = TouchState.DoubleTouch;                                   // Need to press LMB, then RMB
    }

    // Is reset each frame
    private void CheckMouseUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _touchState = TouchState.TouchedUp;
            _wasUpOnUI = EventSystem.current.IsPointerOverGameObject();
        }
    }

    // TODO: Implement touchscreen control (like e.g. picture zoom works?)
    private void CheckScroll()
    {
        _scrollValue = (int)Input.mouseScrollDelta.y;
    }
}
