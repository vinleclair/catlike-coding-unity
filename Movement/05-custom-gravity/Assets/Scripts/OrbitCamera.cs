using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform focus = default;
    [SerializeField, Range(1f, 20f)] private float distance = 5f;
    [SerializeField, Min(0f)] private float focusRadius = 1f;
    [SerializeField, Range(0f, 1f)] private float focusCentering = 0.5f;
    [SerializeField, Range(1f, 360f)] private float rotationSpeed = 90f;
    [SerializeField, Range(-89f, 89f)] private float minVerticalAngle = -30f, maxVerticalAngle = 60f;
    [SerializeField, Min(0f)] private float alignDelay = 5f;
    [SerializeField, Range(0f, 90f)] private float alignSmoothRange = 45f;
    [SerializeField] private LayerMask obstructionMask = -1;

    private Vector3 _focusPoint, _previousFocusPoint;
    private Vector2 _orbitAngles = new Vector2(45f, 0f);
    private float _lastManualRotationTime;
    private Camera _regularCamera;
    private Quaternion gravityAlignment = Quaternion.identity;
    private Quaternion orbitRotation;

    private Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y = _regularCamera.nearClipPlane *
                            Mathf.Tan(0.5f * Mathf.Deg2Rad * _regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * _regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }

    private void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    private void Awake()
    {
        _regularCamera = GetComponent<Camera>();
        _focusPoint = focus.position;
        transform.localRotation = orbitRotation = Quaternion.Euler(_orbitAngles);
    }

    private void LateUpdate()
    {
        gravityAlignment = Quaternion.FromToRotation(
            gravityAlignment * Vector3.up, CustomGravity.GetUpAxis(_focusPoint)) * gravityAlignment;
        UpdateFocusPoint();

        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            orbitRotation = Quaternion.Euler(_orbitAngles);
        }

        var lookRotation = gravityAlignment * orbitRotation;
        var lookDirection = lookRotation * Vector3.forward;
        var lookPosition = _focusPoint - lookDirection * distance;

        var rectOffset = lookDirection * _regularCamera.nearClipPlane;
        var rectPosition = lookPosition + rectOffset;
        var castFrom = focus.position;
        var castLine = rectPosition - castFrom;
        var castDistance = castLine.magnitude;
        var castDirection = castLine / castDistance;

        if (Physics.BoxCast(
            castFrom, CameraHalfExtends, castDirection, out var hit, lookRotation, castDistance, obstructionMask
        ))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    private void UpdateFocusPoint()
    {
        _previousFocusPoint = _focusPoint;
        var targetPoint = focus.position;
        if (focusRadius > 0f)
        {
            var cameraDistance = Vector3.Distance(targetPoint, _focusPoint);
            var t = 1f;
            if (cameraDistance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }

            if (cameraDistance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / cameraDistance);
            }

            _focusPoint = Vector3.Lerp(targetPoint, _focusPoint, t);
        }
        else
        {
            _focusPoint = targetPoint;
        }
    }

    private bool ManualRotation()
    {
        var input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));
        const float e = 0.001f;

        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            _orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            _lastManualRotationTime = Time.unscaledTime;
            return true;
        }

        return false;
    }

    private bool AutomaticRotation()
    {
        if (Time.unscaledTime - _lastManualRotationTime < alignDelay)
        {
            return false;
        }

        var alignedDelta = Quaternion.Inverse(gravityAlignment) * (_focusPoint - _previousFocusPoint);
        var movement = new Vector2(
            alignedDelta.x,
            alignedDelta.z
        );
        var movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.000001f)
        {
            return false;
        }

        var headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        var deltaAbs = Mathf.Abs(Mathf.DeltaAngle(_orbitAngles.y, headingAngle));
        var rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }

        _orbitAngles.y = Mathf.MoveTowardsAngle(_orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    private void ConstrainAngles()
    {
        _orbitAngles.x = Mathf.Clamp(_orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (_orbitAngles.y < 0f)
        {
            _orbitAngles.y += 360f;
        }
        else if (_orbitAngles.y >= 360f)
        {
            _orbitAngles.y -= 360f;
        }
    }

    private static float GetAngle(Vector2 direction)
    {
        var angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle;
    }
}