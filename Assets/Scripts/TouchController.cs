using UnityEngine;
using UnityEngine.EventSystems;

public class TouchController : MonoBehaviour
{
    private Vector3 _touchPosition;  // screen coordinates for touch  https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
    private Transform _dummyTransform;
    private State _state;

    enum State
    {
        NoTouch,
        OneFingerTouch,
        TwoFingerTouch
    }

    void Start()
    {
        _dummyTransform = GameObject.Find("dummy").transform;
    }

    void Update()
    {
        // TODO: ► Process only if screen is touched?
        ProcessTouch();
    }
    
    void ProcessTouch()
    {
        if (_state == State.OneFingerTouch)
        {
            var diff = (Input.mousePosition - _touchPosition) / 20;
            var translationV3 = new Vector3(diff.x, diff.z, 0);
            _dummyTransform.Translate(translationV3);
            _touchPosition = Input.mousePosition;
            print("translating " + Time.time);
        }

        if (MouseDown())
        {
            _state = State.OneFingerTouch;
            _touchPosition = Input.mousePosition;
        }
        
        if (MouseUp())  // See UFO to implement touch
        {
            _state = State.NoTouch;
            
            // Finished touch, raycast      // TODO: Add some value for small difference
            if (_touchPosition == Input.mousePosition)
                TrackEditor.Instance.ProcessSimpleTouch();
        }
    }
    
    private bool MouseDown()
    {                                         // exclude UI from touch event ↓
        return Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject();
    }

    private bool MouseUp()
    {
        return Input.GetMouseButtonUp(0);
    }
}
