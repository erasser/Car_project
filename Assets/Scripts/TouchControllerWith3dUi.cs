using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Can be attached to anything (e.g. GameController).
/// </summary>

// TODO: Make it relative to screen dimensions, so the pan and orbit speed is always constant
// See UFO to implement touch
// TODO: ► Process only if screen is touched? Přece musí jít ty eventy pověsit nějak na celý okno hry

public class TouchControllerWith3dUi : MonoBehaviour
{
    [SerializeField]    [Range(.01f, 5)]      [Tooltip("Interval in seconds, in which long touch event is fired (without touch dragging).")]
    float longTouchDuration = 2;
    [SerializeField]
    bool use3DUiToo = true;
    [SerializeField]
    GameObject cameraUi;                // TODO: Show only if use3DUiToo = true
    [SerializeField]
    LayerMask selectable3dUiObjectsLayer; // TODO: Show only if use3DUiToo = true  https://answers.unity.com/questions/1284988/custom-inspector-2.html

    public static Camera cameraUiComponent;     // TODO: Show only if use3DUiToo = true
    static Vector3 _lastMousePosition;  // screen coordinates for touch  https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
    static TouchState _touchState = TouchState.NoTouch;
    static ControllerState _controllerState = ControllerState.NoAction;
    static int _scrollValue;
    static Vector3 _touchDownPosition;
    static Vector3 _touchUpPosition;
    // private static bool _wasDownOnUI;
    static bool _wasUpOnUI;
    float _touchDuration;

#if UNITY_EDITOR    // for debug
    [Space]
    public string debugTouchState;
    public string debugControllerState;
    readonly Dictionary<TouchState, string> _touchStates = new() {
        { TouchState.NoTouch, "no touch" },
        { TouchState.TouchedDown, "touched down" },
        { TouchState.TouchedUp, "touched up" },
        { TouchState.DoubleTouch, "double touch" },
        { TouchState.HeldTouch, "held touch" }};
    readonly Dictionary<ControllerState, string> _controllerStates = new() {
        { ControllerState.NoAction, "no action" },
        { ControllerState.Panning, "panning" },
        { ControllerState.Orbiting, "orbiting" },
        { ControllerState.Ui3DUsed, "UI 3D used" },
        { ControllerState.PartDeleted, "part deleted" }};
#endif

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
        Ui3DUsed,
        PartDeleted
    }

    void Start()
    {
        cameraUiComponent = cameraUi.GetComponent<Camera>();
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

#if UNITY_EDITOR
        DebugShowStates();
#endif

        /*  Zoom  */  // TODO: Implement for mobile
        if (_scrollValue != 0)
        {
            OrbitCamera.Zoom(_scrollValue);
            return;
        }

        /*  3D UI touch  */
        if (use3DUiToo && _touchState == TouchState.TouchedDown && _controllerState == ControllerState.NoAction)
        {
            /*  process 3D UI touch  */
            // if (TrackEditor.trackEditor.Process3dUiTouch(selectable3dUiObjectsLayer))
            // {
            //     _touchState = TouchState.NoTouch;
            //     _controllerState = ControllerState.Ui3DUsed;
            // }
        }

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

            var touchPositionDiff = Input.mousePosition - _lastMousePosition;

            /*  orbit camera */  // Now dragging with touch
            if (touchPositionDiff.sqrMagnitude > 0)  // TODO: Add some value for small difference?
            {
                _controllerState = ControllerState.Orbiting;
                OrbitCamera.Orbit(touchPositionDiff);
            }
        }

        /*  pan camera  */  // Now double (or more) touching
        if (_touchState == TouchState.DoubleTouch)
        {
            var touchPositionDiff = Input.mousePosition - _lastMousePosition;
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

        _lastMousePosition = Input.mousePosition;
    }

    // Is reset each frame in ProcessTouch() when processed
    void CheckMouseDown()
    {
        // UI - Při přechodu na nové 3D UI udělat na to metodu. Toto použít v metodě tady, komplexnější logiku v UI.cs
        // UI elementům se dá vypnout Raycast target...
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0))
        {
            _touchState = TouchState.TouchedDown;
        }

        if (Input.GetMouseButtonDown(1) && _touchState == TouchState.TouchedDown)
        {
            _touchState = TouchState.DoubleTouch; // Need to press LMB, then RMB
        }
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

#if UNITY_EDITOR
    void DebugShowStates()
    {
        debugTouchState = _touchStates[_touchState];
        debugControllerState = _controllerStates[_controllerState];
    }
#endif
}
