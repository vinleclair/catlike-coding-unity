using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StableFloatingRigidbody : MonoBehaviour
{
    [SerializeField] private bool floatToSleep = false;
    [SerializeField] private float submergenceOffset = 0.5f;
    [SerializeField, Min(0.1f)] private float submergenceRange = 1f;
    [SerializeField, Min(0f)] private float buoyancy = 1f;
    [SerializeField] private Vector3[] buoyancyOffsets = default;
    [SerializeField, Range(0f, 10f)] private float waterDrag = 1f;
    [SerializeField] private LayerMask waterMask = 0;
    [SerializeField] private bool safeFloating = false;

    private Rigidbody _body;
    private float _floatDelay;
    private float[] _submergence;
    private Vector3 _gravity;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.useGravity = false;
        _submergence = new float[buoyancyOffsets.Length];
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
        var dragFactor = waterDrag * Time.deltaTime / buoyancyOffsets.Length;
        var buoyancyFactor = -buoyancy / buoyancyOffsets.Length;
        for (var i = 0; i < buoyancyOffsets.Length; i++)
        {
            if (_submergence[i] > 0f)
            {
                var drag =
                    Mathf.Max(0f, 1f - dragFactor * _submergence[i] * Time.deltaTime);
                _body.velocity *= drag;
                _body.angularVelocity *= drag;
                _body.AddForceAtPosition(
                    _gravity * (buoyancyFactor * _submergence[i]),
                    transform.TransformPoint(buoyancyOffsets[i]),
                    ForceMode.Acceleration
                );
                _submergence[i] = 0f;
            }
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
        var down = _gravity.normalized;
        var offset = down * -submergenceOffset;
        for (var i = 0; i < buoyancyOffsets.Length; i++)
        {
            var p = offset + transform.TransformPoint(buoyancyOffsets[i]);
            if (Physics.Raycast(
                p, down, out var hit, submergenceRange + 1f,
                waterMask, QueryTriggerInteraction.Collide
            ))
            {
                _submergence[i] = 1f - hit.distance / submergenceRange;
            }
            else if (!safeFloating || Physics.CheckSphere(p, 0.01f, waterMask, QueryTriggerInteraction.Collide))
            {
                _submergence[i] = 1f;
            }
        }
    }
}