using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
///     To be attached to anything (e.g. GameController)
/// </summary>

// See UFO to implement touch
// TODO: ► Process only if screen is touched? Přece musí jít ty eventy pověsit nějak na celý okno hry

public class TouchController : MonoBehaviour
{
    [SerializeField]
    private bool use3DUiToo = true;
    [SerializeField]
    private GameObject cameraUi;
    public static Camera cameraUiComponent;
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
        Rotating
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

        var was3DUiIntersected = false;

        if (use3DUiToo && _touchState == TouchState.TouchedDown) 
        {
            if (TrackEditor.instance.ProcessUiTouch(selectableUiObjectsLayer))
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
    private void CheckMouseUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _touchState = TouchState.TouchedUp;
            _wasUpOnUI = EventSystem.current.IsPointerOverGameObject();
            _touchDuration = 0;
        }
    }

    // TODO: Implement touchscreen control (like e.g. picture zoom works?)
    private void CheckScroll()
    {
        _scrollValue = (int)Input.mouseScrollDelta.y;
    }
}
