using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  // Leave it here for debug

/// <summary>
///     It's added dynamically from GameStateManager.
/// </summary>

// TODO: Make it relative to screen dimensions, so the pan and orbit speed is always constant

public class TouchControllerTouchScreen : MonoBehaviour
{
    [SerializeField]    [Range(.01f, 5)]      [Tooltip("Interval in seconds, in which long touch event is fired (without touch dragging).")]
    float longTouchDuration = 2;

    static Vector2 _lastTouchPosition;  // https://docs.unity3d.com/ScriptReference/Input-mousePosition.html, https://docs.unity3d.com/ScriptReference/Touch-position.html
    static Vector2 _lastTouchDiff;
    static float _lastTouchDiffVsActualTouchMagnitude;  // Difference between actual and last two simultaneous touches
    static TouchState _touchState = TouchState.NoTouch;
    static ControllerState _controllerState = ControllerState.NoAction;
    static int _scrollValue;
    static Vector3 _touchDownPosition;
    static Vector3 _touchUpPosition;
    static bool _wasUpOnUI;
    float _touchDuration;

    enum TouchState         // All must be contained in _touchStates
    {
        NoTouch,
        TouchedDown,
        TouchedUp,
        DoubleTouch,  // two fingers
        HeldTouch
    }

    enum ControllerState    // All must be contained in _controllerStates
    {
        NoAction,
        Panning,     // dragging with double touch
        Orbiting,    // dragging with single touch
        PartDeleted
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

        /*  Zoom  */  // TODO: Implement for mobile
        var magnitudeDiff = _lastTouchDiffVsActualTouchMagnitude - GetTouchDiff().magnitude;
        if (magnitudeDiff != 0)
            OrbitCamera.Zoom((int)(magnitudeDiff));

        if (_touchState == TouchState.TouchedDown)
        {
            _touchDuration += Time.deltaTime;  // Used to detect held touch

            /*  delete a part  */  // Now holding touch
            if (_touchDuration > longTouchDuration && _controllerState == ControllerState.NoAction)
            {
                _touchState = TouchState.HeldTouch;

                if (TrackEditor.trackEditor.ProcessHeldTouch())  // This approach deletes also unselected parts
                    _controllerState = ControllerState.PartDeleted;
                
                // if (TrackEditor.selectedPart)  // This approach needs a part to be selected
                //     TrackEditor.selectedPart.GetComponent<Part>().Delete();
            }

            Vector2 touchPositionDiff = GetTouchDiff();

            /*  orbit camera */  // Now dragging with touch
            if (touchPositionDiff.sqrMagnitude > 0)  // TODO: Add some value for small difference?
            {
                _controllerState = ControllerState.Orbiting;
                OrbitCamera.Orbit(touchPositionDiff);
            }
        }

        /*  pan camera  */  // Now double touching  // TODO: Implement for touch screen
        if (_touchState == TouchState.DoubleTouch)
        {
            Vector2 touchPositionDiff = GetTouchDiff();
            // Now moving with double touch
            if (touchPositionDiff.sqrMagnitude > 0)  // TODO: Add some value for small difference?
            {
                _controllerState = ControllerState.Panning;
                OrbitCamera.Pan(touchPositionDiff);
            }
        }

        /*  Raycast to select part  */  // Finished touch without camera pan or rotation or use of UI (common or 3D)
        if (_touchState == TouchState.TouchedUp /*&& _controllerState == ControllerState.NoAction*/)
        {
            if (!_wasUpOnUI && _controllerState == ControllerState.NoAction)
            {
                TrackEditor.trackEditor.ProcessSimpleTouch();
            }
            else
            {
                /*  Reset states after orbit, pan or use of 3D UI  */
                _controllerState = ControllerState.NoAction;
            }
            _touchState = TouchState.NoTouch;
        }

        _lastTouchDiffVsActualTouchMagnitude = (GetTouchDiff() - _lastTouchDiff).magnitude;
        _lastTouchDiff = GetTouchDiff();

        // TODO: Maybe it's not necessary if the state is no touch
        _lastTouchPosition = GetTouchPosition();
    }

    Vector2 GetTouchPosition()
    {
        // if (Input.touchCount == 0 || Input.touchCount > 2) return Vector2.zero;  // TODO: Vyřešit tento stav
        if (Input.touchCount != 1) return Vector2.zero;  // TODO: Vyřešit tento stav

        return Input.GetTouch(0).position;
    }

    Vector2 GetTouchDiff()
    {
        // UNITY_EDITOR: return GetTouchPosition() - _lastTouchPosition;

        if (Input.touchCount == 0 || Input.touchCount > 2) return Vector2.zero;  // TODO: Vyřešit tento stav

        if (Input.touchCount == 1)
            return Input.GetTouch(0).position - _lastTouchPosition;
        else  // touchCount = 2  // TODO: WHAT?
            return Input.GetTouch(0).position - Input.GetTouch(1).position;
    }

    // Is reset each frame in ProcessTouch() when processed
    void CheckMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // #if UNITY_EDITOR
        //     if (Input.GetMouseButtonDown(0))
        //         _touchState = TouchState.TouchedDown;
        //
        //     if (Input.GetMouseButtonDown(1) && _touchState == TouchState.TouchedDown)
        //         _touchState = TouchState.DoubleTouch; // Need to press LMB, then RMB

        // No touch: Ended
        // Touch:    Began
        // Hold:     Stationary
        // Move:     Moved
        // End:      Ended

        if (Input.touchCount > 2) return;

        if (Input.touchCount == 1)
        {
            if (_touchState == TouchState.NoTouch)
                _lastTouchPosition = GetTouchPosition();

            _touchState = TouchState.TouchedDown;
        }

        else if (Input.touchCount == 2)
            _touchState = TouchState.DoubleTouch;

            // string tmp;
            // if (Input.touchCount > 0 /*&& Input.GetTouch(0).phase == TouchPhase.Began*/)
            //     tmp = "touch 0";
            // else if (Input.touchCount > 1 /*&& Input.GetTouch(1).phase == TouchPhase.Began*/)
            //     tmp = "touch 1";
            // else
            //     tmp = "other touch";
            
            // if (Input.touchCount == 1)
            //     GameObject.Find("debugText").GetComponent<Text>().text = Input.GetTouch(0).position.ToString();
            // if (Input.touchCount == 2)
            //     GameObject.Find("debugText").GetComponent<Text>().text = Input.GetTouch(0).position + ", " + Input.GetTouch(1).position;

            // if (Input.touchCount == 1)
            //     GameObject.Find("debugText").GetComponent<Text>().text = Input.GetTouch(0).phase.ToString();
            // if (Input.touchCount == 2)
            //     GameObject.Find("debugText").GetComponent<Text>().text = Input.GetTouch(0).phase + ", " + Input.GetTouch(1).phase;

            // GameObject.Find("debugText").GetComponent<Text>().text = _touchState.ToString();
            // GameObject.Find("debugText").GetComponent<Text>().text = ((int)(_lastTouchDiffVsActualTouchMagnitude - GetTouchDiff().magnitude)).ToString();
    }

    // Is reset each frame in ProcessTouch() when processed
    void CheckMouseUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _touchState = TouchState.TouchedUp;
            _wasUpOnUI = EventSystem.current.IsPointerOverGameObject();
            _touchDuration = 0;
        }
    }

    // TODO: Implement touchscreen control (like e.g. picture zoom works?)
    void CheckScroll()
    {
        _scrollValue = (int)Input.mouseScrollDelta.y;
    }
}
