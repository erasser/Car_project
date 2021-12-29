using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     Can be attached to anything (e.g. GameController).
/// </summary>

// See UFO to implement touch
// TODO: ► Process only if screen is touched? Přece musí jít ty eventy pověsit nějak na celý okno hry

public class TouchController : MonoBehaviour
{
    [SerializeField]
    private bool use3DUiToo = true;
    [SerializeField]
    private GameObject cameraUi;                // TODO: Show only if use3DUiToo = true
    public static Camera cameraUiComponent;     // TODO: Show only if use3DUiToo = true
    [SerializeField]
    private LayerMask selectableUiObjectsLayer; // TODO: Show only if use3DUiToo = true  https://answers.unity.com/questions/1284988/custom-inspector-2.html
    private static Vector3 _lastMousePosition;  // screen coordinates for touch  https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
    private static TouchState _touchState = TouchState.NoTouch;
    private static ControllerState _controllerState = ControllerState.NoAction;
    private static int _scrollValue;
    private static Vector3 _touchDownPosition;
    private static Vector3 _touchUpPosition;
    // private static bool _wasDownOnUI;
    private static bool _wasUpOnUI;
    private float _touchDuration;

#if UNITY_EDITOR    // for debug
    public string debugTouchState;
    public string debugControllerState;
    private readonly Dictionary<TouchState, string> _touchStates = new() {
        { TouchState.NoTouch, "no touch" },
        { TouchState.TouchedDown, "touched down" },
        { TouchState.TouchedUp, "touched up" },
        { TouchState.DoubleTouch, "double touch" },
        { TouchState.HeldTouch, "held touch" }};
    private readonly Dictionary<ControllerState, string> _controllerStates = new() {
        { ControllerState.NoAction, "no action" },
        { ControllerState.Panning, "panning" },
        { ControllerState.Orbiting, "orbiting" },
        { ControllerState.Ui3DUsed, "UI 3D used" }};
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
        Panning,
        Orbiting,
        Ui3DUsed
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

        /*  Zoom  */
        if (_scrollValue != 0)
        {
            OrbitCamera.Zoom(_scrollValue);
        }

        /*  3D UI touch  */
        if (use3DUiToo && _touchState == TouchState.TouchedDown && _controllerState == ControllerState.NoAction) 
        {
            if (TrackEditor.instance.ProcessUiTouch(selectableUiObjectsLayer))
            {
                _touchState = TouchState.NoTouch;
                _controllerState = ControllerState.Ui3DUsed;
            }
        }

        /*  orbit camera */  // Now touching
        if (_touchState == TouchState.TouchedDown)
        {
            _touchDuration += Time.deltaTime;  // Used to detect held touch

            /*  delete a part  */  // Now holding touch
            if (_touchDuration > 2 && _controllerState == ControllerState.NoAction)
            {
                _touchState = TouchState.HeldTouch;  // TODO: This may be redundant
                if (TrackEditor.selectedPart)
                    TrackEditor.selectedPart.GetComponent<Part>().Delete();
            }

            var touchPositionDiff = Input.mousePosition - _lastMousePosition;
            // Now moving with touch
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
                TrackEditor.instance.ProcessSimpleTouch();
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
    private void CheckMouseDown()
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
