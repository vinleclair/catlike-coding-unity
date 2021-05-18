using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
    [SerializeField] private bool floatToSleep = false;
    [SerializeField] private float submergenceOffset = 0.5f;
    [SerializeField, Min(0.1f)] private float submergenceRange = 1f;
    [SerializeField, Min(0f)] private float buoyancy = 1f;
    [SerializeField] private Vector3 buoyancyOffset = Vector3.zero;
    [SerializeField, Range(0f, 10f)] private float waterDrag = 1f;
    [SerializeField] private LayerMask waterMask = 0;

    private Rigidbody _body;
    private float _floatDelay;
    private float _submergence;
    private Vector3 _gravity;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.useGravity = false;
    }

    private void FixedUpdate()
    {
        if (floatToSleep)
        {
            if (_body.IsSleeping())

            {
                _floatDelay = 0f;
                return;
            }

            if (_body.velocity.sqrMagnitude < 0.0001f)
            {
                _floatDelay += Time.deltaTime;
                if (_floatDelay >= 1f)
                {
                    return;
                }
            }
            else
            {
                _floatDelay = 0f;
            }
        }

        _gravity = CustomGravity.GetGravity(_body.position);
        if (_submergence > 0f)
        {
            var drag =
                Mathf.Max(0f, 1f - waterDrag * _submergence * Time.deltaTime);
            _body.velocity *= drag;
            _body.angularVelocity *= drag;
            _body.AddForceAtPosition(
                _gravity * -(buoyancy * _submergence),
                transform.TransformPoint(buoyancyOffset),
                ForceMode.Acceleration
            );
            _submergence = 0f;
        }

        _body.AddForce(_gravity, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((waterMask & (1 << other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_body.IsSleeping() && (waterMask & (1 << other.gameObject.layer)) != 0)
        {
            EvaluateSubmergence();
        }
    }

    private void EvaluateSubmergence()
    {
        var upAxis = -_gravity.normalized;
        if (Physics.Raycast(
            _body.position + upAxis * submergenceOffset,
            -upAxis, out var hit, submergenceRange + 1f,
            waterMask, QueryTriggerInteraction.Collide
        ))
        {
            _submergence = 1f - hit.distance / submergenceRange;
        }
        else
        {
            _submergence = 1f;
        }
    }
}