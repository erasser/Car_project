using UnityEngine;
using UnityEngine.Animations;
using static UiController;
using static UnityEngine.Mathf;
using Vector3 = UnityEngine.Vector3;

// TODO: Limit serialized fields values (pitch [-90, 90], distance: positive values), allow to disable pitch limiting) 

/// <summary>
/// <para>
///     Creates the camera target, positions it to the same height as the camera has, attaches the camera to the target, sets the min pitch value. Does not allow camera roll.
/// </para>
/// <para>
///     Usage: Add this script to a camera. Camera position z component is supposed to be negative.
/// </para>
/// <para>
///     Can be initialized with Set().
/// </para>
/// </summary>

public class OrbitCamera : MonoBehaviour
{
    [Range(0, 90)]      [Tooltip("Minimal pitch in degrees")]
    public int minPitch = 5;  // Beware: Euler angles are clamped to [0, 360]
    [Range(0, 90)]      [Tooltip("Maximal pitch in degrees")]
    public int maxPitch = 85;
    [Range(0, 10000)]   [Tooltip("Minimal camera distance in scene units")]
    public int minZoom = 5;
    [Range(0, 10000)]   [Tooltip("Maximal camera distance in scene units")]
    public int maxZoom = 400;
    [Range(1, 255)]
    public byte orbitSpeed = 20;
    [Range(1, 255)]
    public byte panSpeed = 16;
    [Space]
                        [Tooltip("This object will be rotated in Y axis correspondingly to the camera rotation (optional)")]
    public GameObject uiRotateHorizontalUiElement;

    public static OrbitCamera orbitCamera;
    public static Camera cameraComponent;
    public static Transform cameraTargetTransform;  // Should not be child of anything
    static GameObject _watchedObject;
    static Vector3 _minTargetPosition;  // Must be set "from the outside" for pan to work. Use SetTargetPositionLimits().
    static Vector3 _maxTargetPosition;  //                            ——————||——————

    void Awake()
    {
        orbitCamera = this;
        cameraTargetTransform = new GameObject("cameraTarget").transform;
        cameraTargetTransform.Translate(0, transform.position.y, 0);
        transform.SetParent(cameraTargetTransform, true);
        gameObject.AddComponent<LookAtConstraint>().constraintActive = true;  // It works without source object, strange...
        SetPitch(orbitCamera.minPitch);  // So the pitch limit is satisfied
        cameraComponent = GetComponent<Camera>();

        #if !UNITY_EDITOR
            orbitSpeed /= 4;
        #endif
    }

    void Update()
    {
        if (_watchedObject)
            cameraTargetTransform.position = _watchedObject.transform.position;
    }

    public static void Pan(Vector3 touchPositionDiff)
    {
        var translationV3 = touchPositionDiff * Time.deltaTime * orbitCamera.panSpeed;
        var newPosition = cameraTargetTransform.position - cameraTargetTransform.TransformDirection(translationV3);

        newPosition.x = Clamp(newPosition.x, _minTargetPosition.x, _maxTargetPosition.x);
        newPosition.y = Clamp(newPosition.y, _minTargetPosition.y, _maxTargetPosition.y);
        newPosition.z = Clamp(newPosition.z, _minTargetPosition.z, _maxTargetPosition.z);

        cameraTargetTransform.position = newPosition;
    }

    /// <summary>
    ///     Orbits the camera relatively.
    /// </summary>
    /// <param name="touchPositionDiff">Should be <c>Input.mousePosition - lastMousePosition</c>.</param>
    public static void Orbit(Vector3 touchPositionDiff)
    {
        var rotationV3 = touchPositionDiff * Time.deltaTime * orbitCamera.orbitSpeed;
        var localEulerAngles = cameraTargetTransform.localEulerAngles;
                                    // ↓ Slower orbit speed when nearer to orbit pole
        var rotY = rotationV3.x /* * Mathf.Cos(localEulerAngles.x * .0174533f)*/;

        SetRotation(localEulerAngles.x - rotationV3.y, localEulerAngles.y + rotY);
    }

    /// <summary>
    ///     Zoom relatively.
    /// </summary>
    /// <param name="zoomValue">Should be -1 or 1, can be 0. Provide <c>(int)Input.mouseScrollDelta.y</c></param>
    public static void Zoom(int zoomValue)  // -1 | 1 comes in
    {
        // var coefficient = - cameraComponent.transform.localPosition.z / instance.maxZoom;  // Could be used for faster zoom on higher distance

        cameraComponent.transform.localPosition = new (0, 0,                 // z must be inverted, because it's supposed to be negative
            Clamp(cameraComponent.transform.localPosition.z + zoomValue * 5 /* * coefficient*/, - orbitCamera.maxZoom, - orbitCamera.minZoom));
    }

    /// <summary>
    ///     Look at the provided vector once.
    /// </summary>
    /// <param name="lookAt">Vector to look at</param>
    public static void LookAtV3(Vector3 lookAt)
    {
        cameraTargetTransform.position = lookAt;
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
    /// <para>
    ///     Sets rotation of camera target to absolute value.
    /// </para>
    /// <para>
    ///     x = pitch (vertical position of camera)
    /// </para>
    /// <para>
    ///     y = yaw (horizontal position of camera)
    /// </para>
    /// </summary>
    /// <param name="pitch">Pitch angle in degrees</param>
    /// <param name="yaw">Yaw angle in degrees</param>
    public static void SetRotation(float pitch, float yaw)
    {
        pitch = Clamp(pitch, orbitCamera.minPitch, orbitCamera.maxPitch);
        cameraTargetTransform.eulerAngles = new (pitch, yaw, 0);

        Update3dUiTransform();
    }

    /// <summary>
    ///     Sets the pitch angle, affects camera vertical position.
    /// </summary>
    /// <param name="pitch">Pitch value in degrees, will be clamped between minPitch and maxPitch value.</param>
    public static void SetPitch(float pitch)
    {
        SetRotation(pitch, cameraTargetTransform.eulerAngles.y);
    }

    /// <summary>
    ///     Sets the yaw angle, affects camera vertical position.
    /// </summary>
    /// <param name="yaw">Yaw value in degrees.</param>
    public static void SetYaw(float yaw)
    {
        SetRotation(cameraTargetTransform.eulerAngles.x, yaw);
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

        if (distance < orbitCamera.minZoom || distance > orbitCamera.maxZoom)
        {
            var oldDistance = distance;
            distance = Clamp(oldDistance, orbitCamera.minZoom, orbitCamera.maxZoom);
            Debug.LogWarning($"Distance = {oldDistance} is out of zoom limits [{orbitCamera.minZoom}, {orbitCamera.maxZoom}]. Value was clamped to distance = {distance}");
        }

        cameraComponent.transform.localPosition = new (0, 0, - distance);
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
    ///     Use this to set scene bounds. Camera target will move inside these limits when camera panning.
    /// </summary>
    /// <param name="min">A point determining minimal limits.</param>
    /// <param name="max">A point determining maximal limits.</param>
    public static void SetTargetPositionLimits(Vector3 min, Vector3 max)
    {
        _minTargetPosition = min;
        _maxTargetPosition = max;
    }
}
