using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField] private Transform playerInputSpace = default;
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 10f, maxAirAcceleration = 1f;
    [SerializeField, Range(0f, 10f)] private float jumpHeight = 2f;
    [SerializeField, Range(0, 5)] private int maxAirJumps = 0;
    [SerializeField, Range(0f, 90f)] private float maxGroundAngle = 25f, maxStairsAngle = 50f;
    [SerializeField, Range(0f, 100f)] private float maxSnapSpeed = 100f;
    [SerializeField, Min(0f)] private float probeDistance = 1f;
    [SerializeField] private LayerMask probeMask = -1, stairsMask = -1;

    private Rigidbody _body, _connectedBody, _previousConnectedBody;
    private Vector3 _velocity, _desiredVelocity, _connectionVelocity;
    private Vector3 _connectionWorldPosition, _connectionLocalPosition;
    private Vector3 _contactNormal, _steepNormal;
    private bool _desiredJump;
    private int _groundContactCount, _steepContactCount;
    private bool ONGround => _groundContactCount > 0;
    private bool ONSteep => _steepContactCount > 0;
    private int _jumpPhase;
    private float _minGroundDotProduct, _minStairsDotProduct;
    private int _stepsSinceLastGrounded, _stepsSinceLastJump;
    private Vector3 _upAxis, _rightAxis, _forwardAxis;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.useGravity = false;
        OnValidate();
    }

    private void OnValidate()
    {
        _minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        _minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        if (playerInputSpace)
        {
            _rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, _upAxis);
            _forwardAxis =
                ProjectDirectionOnPlane(playerInputSpace.forward, _upAxis);
        }
        else
        {
            _rightAxis = ProjectDirectionOnPlane(Vector3.right, _upAxis);
            _forwardAxis = ProjectDirectionOnPlane(Vector3.forward, _upAxis);
        }

        _desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        _desiredJump |= Input.GetButtonDown("Jump");
    }

    private void FixedUpdate()
    {
        var gravity = CustomGravity.GetGravity(_body.position, out _upAxis);
        UpdateState();
        AdjustVelocity();

        if (_desiredJump)
        {
            _desiredJump = false;
            Jump(gravity);
        }

        _velocity += gravity * Time.deltaTime;

        _body.velocity = _velocity;

        ClearState();
    }

    private void ClearState()
    {
        _groundContactCount = _steepContactCount = 0;
        _contactNormal = _steepNormal = _connectionVelocity = Vector3.zero;
        _previousConnectedBody = _connectedBody;
        _connectedBody = null;
    }

    private void UpdateState()
    {
        _stepsSinceLastGrounded += 1;
        _stepsSinceLastJump += 1;
        _velocity = _body.velocity;
        if (ONGround || SnapToGround() || CheckSteepContacts())
        {
            _stepsSinceLastGrounded = 0;

            if (_stepsSinceLastJump > 1)
            {
                _jumpPhase = 0;
            }

            if (_groundContactCount > 1)
            {
                _contactNormal.Normalize();
            }
        }
        else
        {
            _contactNormal = _upAxis;
        }

        if (_connectedBody)
        {
            if (_connectedBody.isKinematic || _connectedBody.mass >= _body.mass)
            {
                UpdateConnectionState();
            }
        }
    }

    private void UpdateConnectionState()
    {
        if (_connectedBody == _previousConnectedBody)
        {
            var connectionMovement = _connectedBody.transform.TransformPoint(_connectionLocalPosition) -
                                     _connectionWorldPosition;
            _connectionVelocity = connectionMovement / Time.deltaTime;
        }

        _connectionWorldPosition = _body.position;
        _connectionLocalPosition = _connectedBody.transform.InverseTransformPoint(_connectionWorldPosition);
    }

    private bool SnapToGround()
    {
        if (_stepsSinceLastGrounded > 1 || _stepsSinceLastJump <= 3) //TODO default 2 
        {
            return false;
        }

        var speed = _velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }

        if (!Physics.Raycast(_body.position, -_upAxis, out var hit, probeDistance, probeMask)) //TODO Add more rays?
        {
            return false;
        }

        var upDot = Vector3.Dot(_upAxis, hit.normal);
        if (upDot < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }

        _groundContactCount = 1;
        _contactNormal = hit.normal;
        var dot = Vector3.Dot(_velocity, hit.normal);
        if (dot > 0f)
        {
            _velocity = (_velocity - hit.normal * dot).normalized * speed;
        }

        _connectedBody = hit.rigidbody;
        return true;
    }

    private bool CheckSteepContacts()
    {
        if (_steepContactCount > 1)
        {
            _steepNormal.Normalize();
            var upDot = Vector3.Dot(_upAxis, _steepNormal);
            if (upDot >= _minGroundDotProduct)
            {
                _steepContactCount = 0;
                _groundContactCount = 1;
                _contactNormal = _steepNormal;
                return true;
            }
        }

        return false;
    }

    private void AdjustVelocity()
    {
        var xAxis = ProjectDirectionOnPlane(_rightAxis, _contactNormal);
        var zAxis = ProjectDirectionOnPlane(_forwardAxis, _contactNormal);

        var relativeVelocity = _velocity - _connectionVelocity;
        var currentX = Vector3.Dot(relativeVelocity, xAxis);
        var currentZ = Vector3.Dot(relativeVelocity, zAxis);

        var acceleration = ONGround ? maxAcceleration : maxAirAcceleration;
        var maxSpeedChange = acceleration * Time.deltaTime;

        var newX = Mathf.MoveTowards(currentX, _desiredVelocity.x, maxSpeedChange);
        var newZ = Mathf.MoveTowards(currentZ, _desiredVelocity.z, maxSpeedChange);

        _velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    private void Jump(Vector3 gravity)
    {
        Vector3 jumpDirection;
        if (ONGround)
        {
            jumpDirection = _contactNormal;
        }
        else if (ONSteep)
        {
            jumpDirection = _steepNormal;
            _jumpPhase = 0;
        }
        else if (maxAirJumps > 0 && _jumpPhase <= maxAirJumps)
        {
            if (_jumpPhase == 0)
            {
                _jumpPhase = 1;
            }

            jumpDirection = _contactNormal;
        }
        else
        {
            return;
        }

        _stepsSinceLastJump = 0;
        _jumpPhase += 1;
        var jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
        jumpDirection = (jumpDirection + _upAxis).normalized;
        var alignedSpeed = Vector3.Dot(_velocity, jumpDirection);
        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - _velocity.y, 0f);
        }

        _velocity += jumpDirection * jumpSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision)
    {
        var minDot = GetMinDot(collision.gameObject.layer);
        for (var i = 0; i < collision.contactCount; i++)
        {
            var normal = collision.GetContact(i).normal;
            var upDot = Vector3.Dot(_upAxis, normal);

            if (upDot >= minDot)
            {
                _groundContactCount += 1;
                _contactNormal += normal;
                _connectedBody = collision.rigidbody;
            }
            else if (upDot > -0.01f)
            {
                _steepContactCount += 1;
                _steepNormal += normal;
                if (_groundContactCount == 0)
                {
                    _connectedBody = collision.rigidbody;
                }
            }
        }
    }

    private static Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }

    private float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ? _minGroundDotProduct : _minStairsDotProduct;
    }
}