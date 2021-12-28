using UnityEngine;
using UnityEngine.Animations;
using Vector3 = UnityEngine.Vector3;

// TODO: Limit serialized fields values (pitch [-90, 90], distance: positive values), allow to disable pitch limiting) 

/// <summary>
///     Creates the camera target, positions it to the same height as the camera has, attaches the camera to the target, sets the min pitch value
/// <para>
///     Usage: Add this script to a camera. Camera position z component is supposed to be negative.
/// </para>
/// <para>
///     Does not allow camera roll.
/// </para>
/// </summary>

public class OrbitCamera : MonoBehaviour
{
    private static OrbitCamera _instance;
    [SerializeField]    [Range(0, 90)]      [Tooltip("Minimal pitch in degrees")]
    private int minPitch = 5;  // Beware: Euler angles are clamped to [0, 360]
    [SerializeField]    [Range(0, 90)]      [Tooltip("Maximal pitch in degrees")]
    private int maxPitch = 85;
    [SerializeField]    [Range(0, 10000)]   [Tooltip("Minimal camera distance in scene units")]
    private int minZoom = 5;
    [SerializeField]    [Range(0, 10000)]   [Tooltip("Maximal camera distance in scene units")]
    private int maxZoom = 200;
    [SerializeField]    [Range(1, 255)]
    private byte orbitSpeed = 10;
    [SerializeField]    [Range(1, 255)]
    private byte panSpeed = 16;
    [SerializeField]                        [Tooltip("This object will be rotated in Y axis correspondingly to the camera rotation (optional)")]
    private GameObject uiRotateHorizontalUiElement;
    public static Camera cameraComponent;
    private static Transform _cameraTargetTransform;  // Should not be child of anything
    private static GameObject _watchedObject;

    void Awake()
    {
        _instance = this;
        _cameraTargetTransform = new GameObject("cameraTarget").transform;
        _cameraTargetTransform.Translate(0, transform.position.y, 0);
        transform.SetParent(_cameraTargetTransform, true);
        gameObject.AddComponent<LookAtConstraint>().constraintActive = true;  // It works without source object, strange...
        SetPitch(_instance.minPitch);
        cameraComponent = GetComponent<Camera>();
    }

    private void Update()
    {
        if (_watchedObject)
            _cameraTargetTransform.position = _watchedObject.transform.position;
    }

    public static void Pan(Vector3 touchPositionDiff)
    {
        var translationV3 = touchPositionDiff * Time.deltaTime * _instance.panSpeed;
        var newPosition = _cameraTargetTransform.position - _cameraTargetTransform.TransformDirection(translationV3);
        var minPosition = Grid3D.Grid[0][0][0].position;
        var maxPosition = Grid3D.Grid[Grid3D.instance.xCount - 1][Grid3D.instance.yCount - 1][Grid3D.instance.zCount - 1].position;
        
        newPosition.x = Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x);
        newPosition.y = Mathf.Clamp(newPosition.y, minPosition.y, maxPosition.y);
        newPosition.z = Mathf.Clamp(newPosition.z, minPosition.z, maxPosition.z);

        _cameraTargetTransform.position = newPosition;
    }

    /// <summary>
    ///     Orbits the camera relatively.
    /// </summary>
    /// <param name="touchPositionDiff">Should be <c>Input.mousePosition - lastMousePosition</c>.</param>
    public static void Orbit(Vector3 touchPositionDiff)
    {
        var rotationV3 = touchPositionDiff * Time.deltaTime * _instance.orbitSpeed;
        // Slower orbit speed when nearer to orbit pole
        var rotY = rotationV3.x * Mathf.Cos(_cameraTargetTransform.localEulerAngles.x * .0174533f);

        SetRotation(_cameraTargetTransform.localEulerAngles.x - rotationV3.y, _cameraTargetTransform.localEulerAngles.y + rotY);
    }

    /// <summary>
    ///     Zoom relatively.
    /// </summary>
    /// <param name="zoomValue">Should be -1 or 1, can be 0. Provide <c>(int)Input.mouseScrollDelta.y</c></param>
    public static void Zoom(int zoomValue)  // -1 | 1 comes in
    {
        // var coefficient = - cameraComponent.transform.localPosition.z / instance.maxZoom;  // Could be used for faster zoom on higher distance

        cameraComponent.transform.localPosition = new Vector3(0, 0,                 // z must be inverted, because it's supposed to be negative
            Mathf.Clamp(cameraComponent.transform.localPosition.z + zoomValue * 5 /* * coefficient*/, - _instance.maxZoom, - _instance.minZoom));
    }

    /// <summary>
    ///     Look at the provided vector once.
    /// </summary>
    /// <param name="lookAt">Vector to look at</param>
    public static void LookAtV3(Vector3 lookAt)
    {
        _cameraTargetTransform.position = lookAt;
    }

    /// <summary>
    ///     Camera will constantly watch the provided object.
    /// <para>
    ///     Disables camera panning.
    /// </para>
    /// <para>
    ///     Watched object must call <c>OrbitCamera.CheckIfNotWatched()</c> in its <c>OnDestroy()</c> method.
    /// </para>
    /// </summary>
    /// <param name="obj">Object to watch</param>
    public static void WatchObject(GameObject obj = null)
    {
        _watchedObject = obj;
    }
    
    /// <summary>
    ///     Must be called in <c>OnDestroy()</c> method of watched object.
    /// </summary>
    /// <param name="obj">Object to stop watching before it's destroyed.</param>
    public static void CheckIfWatched(GameObject obj)
    {
        if (_watchedObject == obj)
            _watchedObject = null;
    }

    /// <summary>
    ///     Sets rotation of camera target to absolute value.
    /// </summary>
    /// <para>
    ///     x = pitch (vertical position of camera)
    /// </para>
    /// <para>
    ///     y = yaw (horizontal position of camera)
    /// </para>
    /// <param name="pitch">Pitch angle in degrees</param>
    /// <param name="yaw">Yaw angle in degrees</param>
    public static void SetRotation(float pitch, float yaw)
    {
        pitch = Mathf.Clamp(pitch, _instance.minPitch, _instance.maxPitch);
        _cameraTargetTransform.eulerAngles = new Vector3(pitch, yaw, 0);

        UpdateRotateHorizontalUiElement();
    }

    /// <summary>
    ///     Sets the pitch angle, affects camera vertical position.
    /// </summary>
    /// <param name="pitch">Pitch value in degrees, will be clamped between minPitch and maxPitch value.</param>
    public static void SetPitch(float pitch)
    {
        SetRotation(pitch, _cameraTargetTransform.eulerAngles.y);
    }

    /// <summary>
    ///     Sets the yaw angle, affects camera vertical position.
    /// </summary>
    /// <param name="yaw">Yaw value in degrees.</param>
    public static void SetYaw(float yaw)
    {
        SetRotation(_cameraTargetTransform.eulerAngles.x, yaw);
    }

    /// <summary>
    ///     Sets camera distance from camera target. Use to adjust (inverse of) zoom.
    /// </summary>
    /// <param name="distance">Distance in world units, will be clamped between minZoom and maxZoom.</param>
    public static void SetDistance(float distance)
    {
        if (distance < 0)
        {
            Debug.LogWarning("Distance must be of a positive value. No change was made.");
            return;
        }

        if (distance < _instance.minZoom || distance > _instance.maxZoom)
        {
            var oldDistance = distance;
            distance = Mathf.Clamp(oldDistance, _instance.minZoom, _instance.maxZoom);
            Debug.LogWarning($"Distance = {oldDistance} is out of zoom limits [{_instance.minZoom}, {_instance.maxZoom}]. Value was clamped to distance = {distance}");
        }

        cameraComponent.transform.localPosition = new Vector3(0, 0, - distance);
    }

    public static void Set(Vector3 targetPosition, float pitch, float yaw)
    {
        LookAtV3(targetPosition);
        SetRotation(pitch, yaw);
    }

    public static void Set(Vector3 targetPosition, float pitch, float yaw, float distance)
    {
        Set(targetPosition, pitch, yaw);
        SetDistance(distance);
    }

    /// <summary>
    ///     Aligns Y rotation with camera Y rotation
    /// </summary>
    /// <param name="rotY">Camera Y rotation component</param>
    static void UpdateRotateHorizontalUiElement()
    {
        if (_instance.uiRotateHorizontalUiElement)
            _instance.uiRotateHorizontalUiElement.transform.localEulerAngles = new Vector3(0, - _cameraTargetTransform.localEulerAngles.y, 0);
        // _instance.uiRotateHorizontalUiElement.transform.Rotate(Vector3.up, oldRotY - rotY);  // rotate relatively
    }
}
