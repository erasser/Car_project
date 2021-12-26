using UnityEngine;
using UnityEngine.Animations;
using Vector3 = UnityEngine.Vector3;

// TODO: Limit serialized fields values (pitch [-90, 90], distance: positive values), allow to disable pitch limiting) 

/// <summary>
///     Creates the camera target, positions it to the same height as the camera has, attaches the camera to the target, sets the min pitch value
/// <para>
///     Usage: Add this script to a camera. Camera position z component is supposed to be negative.
/// </para>
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

    void Awake()
    {
        instance = this;
        _cameraTargetTransform = new GameObject("cameraTarget").transform;
        _cameraTargetTransform.Translate(0, transform.position.y, 0);
        transform.SetParent(_cameraTargetTransform, true);
        gameObject.AddComponent<LookAtConstraint>().constraintActive = true;  // It works without source object, strange...
        SetPitch(instance.minPitch);
        cameraComponent = GetComponent<Camera>();
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

    /// <summary>
    ///     Orbits the camera relatively.
    /// </summary>
    /// <param name="touchPositionDiff">Should be <c>Input.mousePosition - lastMousePosition</c>.</param>
    public static void Orbit(Vector3 touchPositionDiff)
    {
        var rotationV3 = touchPositionDiff * Time.deltaTime * 16;
        // Slower orbit speed when nearer to orbit pole
        var yRot = rotationV3.x * Mathf.Cos(_cameraTargetTransform.localEulerAngles.x * .0174533f);

        _cameraTargetTransform.Rotate(new Vector3(- rotationV3.y, yRot, 0));
        // _cameraTargetTransform.Rotate(new Vector3(- rotationV3.y, rotationV3.x, 0));
        var rot = _cameraTargetTransform.localEulerAngles;
        rot.z = 0;
        rot.x = Mathf.Clamp(rot.x, instance.minPitch, instance.maxPitch);
        _cameraTargetTransform.localEulerAngles = rot;
    }

    /// <summary>
    ///     Zoom relatively.
    /// </summary>
    /// <param name="zoomValue">Should be -1 or 1, can be 0. Provide <c>(int)Input.mouseScrollDelta.y</c></param>
    public static void Zoom(int zoomValue)  // -1 | 1 comes in
    {
        // var coefficient = - cameraComponent.transform.localPosition.z / instance.maxZoom;  // Could be used for faster zoom on higher distance

        cameraComponent.transform.localPosition = new Vector3(0, 0,                 // z must be inverted, because it's supposed to be negative
            Mathf.Clamp(cameraComponent.transform.localPosition.z + zoomValue * 5 /* * coefficient*/, - instance.maxZoom, - instance.minZoom));
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
    /// <param name="rotation">Rotation vector. Vector z component is ignored.</param>
    public static void SetRotation(Vector3 rotation)
    {
        if (rotation.x < instance.minPitch || rotation.x > instance.maxPitch)
        {
            var oldRotX = rotation.x;
            rotation.x = Mathf.Clamp(oldRotX, instance.minPitch, instance.maxPitch);
            Debug.LogWarning($"Rotation x = {oldRotX} is out of pitch limits [{instance.minPitch}, {instance.maxPitch}]. Value was clamped to x = {rotation.x}");
        }

        _cameraTargetTransform.eulerAngles = new Vector3(rotation.x, rotation.y, 0);
    }

    /// <summary>
    ///     Sets the pitch angle, affects camera vertical position.
    /// </summary>
    /// <param name="pitch">Pitch value in degrees, will be clamped between minPitch and maxPitch value.</param>
    public static void SetPitch(float pitch)
    {
        if (pitch < instance.minPitch || pitch > instance.maxPitch)
        {
            var oldRotX = pitch;
            pitch = Mathf.Clamp(oldRotX, instance.minPitch, instance.maxPitch);
            Debug.LogWarning($"Rotation pitch = {oldRotX} is out of pitch limits [{instance.minPitch}, {instance.maxPitch}]. Value was clamped to x = {pitch}");
        }

        _cameraTargetTransform.eulerAngles = new Vector3(pitch, _cameraTargetTransform.eulerAngles.y, 0);
    }

    /// <summary>
    ///     Sets the yaw angle, affects camera vertical position.
    /// </summary>
    /// <param name="yaw">Yaw value in degrees.</param>
    public static void SetYaw(float yaw)
    {
        _cameraTargetTransform.eulerAngles = new Vector3(_cameraTargetTransform.eulerAngles.x, yaw, 0);
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

        if (distance < instance.minZoom || distance > instance.maxZoom)
        {
            var oldDistance = distance;
            distance = Mathf.Clamp(oldDistance, instance.minZoom, instance.maxZoom);
            Debug.LogWarning($"Distance = {oldDistance} is out of zoom limits [{instance.minZoom}, {instance.maxZoom}]. Value was clamped to distance = {distance}");
        }

        cameraComponent.transform.localPosition = new Vector3(0, 0, - distance);
    }

    public static void Set(Vector3 targetPosition, float pitch, float yaw)
    {
        LookAtV3(targetPosition);
        SetPitch(pitch);
        SetYaw(yaw);
    }

    public static void Set(Vector3 targetPosition, float pitch, float yaw, float distance)
    {
        Set(targetPosition, pitch, yaw);
        SetDistance(distance);
    }
}
