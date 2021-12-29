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
    public string debugTouchState;
    public string debugControllerState;

    enum TouchState
    {
        NoTouch,
        TouchedDown,
        TouchedUp,
        DoubleTouch,  // two fingers
        HeldTouch
    }

    enum ControllerState
    {
        NoAction,
        Panning,
        Rotating,
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

        /*  PO ORBIT A PAN NEFUNGUJE 3D UI  */
        
        DebugShowStates();

        // 3D UI touch
        if (use3DUiToo && _touchState == TouchState.TouchedDown && _controllerState == ControllerState.NoAction) 
        {
            if (TrackEditor.instance.ProcessUiTouch(selectableUiObjectsLayer))
            {
                _touchState = TouchState.NoTouch;
                _controllerState = ControllerState.Ui3DUsed;
            }
        }

        // Reset states after using 3D UI (related to above condition)
        if (use3DUiToo && _touchState == TouchState.TouchedUp && _controllerState == ControllerState.Ui3DUsed)
        {
            _controllerState = ControllerState.NoAction;
            _touchState = TouchState.NoTouch;
        }

        // Now touching => orbit camera
        if (_touchState == TouchState.TouchedDown)
        {
            _touchDuration += Time.deltaTime;  // Used to detect held touch

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
                _controllerState = ControllerState.Rotating;
                OrbitCamera.Orbit(touchPositionDiff);
            }
        }

        // Now double (or more) touching => pan camera      // TODO: Limit camera target position to grid bounds
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

        // Reset states after orbit and pan  // TODO: Can be merged with "reset states after using 3D UI" (use3DUiToo is redundant there)
        if (_touchState == TouchState.TouchedUp && (_controllerState == ControllerState.Rotating || _controllerState == ControllerState.Panning))
        {
            _controllerState = ControllerState.NoAction;
            _touchState = TouchState.NoTouch;
        }
        
        if (_scrollValue != 0)  // TODO: Put this at the end of this block
        {
            OrbitCamera.Zoom(_scrollValue);
        }

        if (_touchState == TouchState.TouchedUp && _controllerState == ControllerState.NoAction)
        {
            // Finished touch without camera pan or rotation => raycast
            if (_controllerState == ControllerState.NoAction && !_wasUpOnUI)  // TODO: Nedalo by se to spojit s předešlou podmínkou?
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

    void DebugShowStates()
    {
        if (_controllerState == ControllerState.NoAction)
            debugControllerState = "no action";
        else if (_controllerState == ControllerState.Panning)
            debugControllerState = "panning";
        else if (_controllerState == ControllerState.Rotating)
            debugControllerState = "rotating";
        else if (_controllerState == ControllerState.Ui3DUsed)
            debugControllerState = "UI 3D used";
        else debugControllerState = "";

        if (_touchState == TouchState.NoTouch)
            debugTouchState = "no touch";
        else if (_touchState == TouchState.TouchedDown)
            debugTouchState = "touch down";
        else if (_touchState == TouchState.TouchedUp)
            debugTouchState = "touch up";
        else if (_touchState == TouchState.HeldTouch)
            debugTouchState = "held touch";
        else if (_touchState == TouchState.DoubleTouch)
            debugTouchState = "double touch";
        else debugTouchState = "";
    }
}
