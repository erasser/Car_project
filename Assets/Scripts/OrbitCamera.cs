using UnityEngine;
using UnityEngine.Animations;
using Vector3 = UnityEngine.Vector3;

/// <summary>
///     Creates the camera target, positions it to the same height, attaches the camera to the target, sets the minimal pitch.
/// </summary>

public class OrbitCamera : MonoBehaviour
{
    public static OrbitCamera instance;
    [SerializeField][Tooltip("Minimal pitch in degrees")]
    private int minPitch = 5;  // Beware: Euler angles are clamped to [0, 360]
    [SerializeField][Tooltip("Maximal pitch in degrees")]
    private int maxPitch = 85;
    [SerializeField]
    private int minZoom = 5;
    [SerializeField]
    private int maxZoom = 200;
    public static Camera cameraComponent;
    private static Transform _cameraTargetTransform;
    private static GameObject _watchedObject;

    void Start()
    {
        instance = this;
        _cameraTargetTransform = new GameObject("cameraTarget").transform;
        _cameraTargetTransform.Translate(0, transform.position.y, 0);
        transform.SetParent(_cameraTargetTransform, true);
        Orbit(Vector3.zero);  // To set initial pitch
        cameraComponent = GetComponent<Camera>();
        gameObject.AddComponent<LookAtConstraint>().constraintActive = true;  // It works without source object, strange...
    }

    private void Update()
    {
        if (_watchedObject)
            _cameraTargetTransform.position = _watchedObject.transform.position;
    }

    public static void Pan(Vector3 touchPositionDiff)
    {
        var translationV3 = touchPositionDiff * Time.deltaTime * 24;
        _cameraTargetTransform.Translate(new Vector3(- translationV3.x, - translationV3.y, 0));
    }

    public static void Orbit(Vector3 touchPositionDiff)
    {
        var rotationV3 = touchPositionDiff * Time.deltaTime * 16;
        // Slower orbit speed when nearer to orbit pole
        var yRot = rotationV3.x * Mathf.Cos(_cameraTargetTransform.localEulerAngles.x * 0.0174533f);

        _cameraTargetTransform.Rotate(new Vector3(- rotationV3.y, yRot, 0));
        // _cameraTargetTransform.Rotate(new Vector3(- rotationV3.y, rotationV3.x, 0));
        var rot = _cameraTargetTransform.localEulerAngles;
        rot.z = 0;
        rot.x = Mathf.Clamp(rot.x, instance.minPitch, instance.maxPitch);
        _cameraTargetTransform.localEulerAngles = rot;
    }

    public static void Zoom(int zoomValue)  // -1 | 1 comes in
    {
        // var coefficient = - cameraComponent.transform.localPosition.z / instance.maxZoom;  // Could be used for faster zoom on higher distance

        cameraComponent.transform.localPosition = new Vector3(0, 0,                 // z must be inverted, because it's supposed to be negative
            Mathf.Clamp(cameraComponent.transform.localPosition.z + zoomValue * 5 /* * coefficient*/, - instance.maxZoom, - instance.minZoom));
    }

    /// <summary>
    ///     Look at the provided vector once
    /// </summary>
    /// <param name="lookAt">Vector to look at</param>
    public static void LookAtV3(Vector3 lookAt)
    {
        _cameraTargetTransform.position = lookAt;
    }

    /// <summary>
    ///     Camera will constantly watch the provided object
    /// <para>
    ///     Watched object must call <c>OrbitCamera.CheckIfNotWatched()</c> in its <c>OnDestroy()</c> method
    /// </para>
    /// </summary>
    /// <param name="obj">Object to watch</param>
    public static void WatchObject(GameObject obj = null)
    {
        _watchedObject = obj;
    }
    
    /// <summary>
    ///     Must be called in <c>OnDestroy()</c> method of watched object
    /// </summary>
    /// <param name="obj">Object to stop watching before it's destroyed</param>
    public static void CheckIfWatched(GameObject obj)
    {
        if (_watchedObject == obj)
            _watchedObject = null;
    }
}
