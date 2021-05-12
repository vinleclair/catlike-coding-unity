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
    
    private Rigidbody _body;
    private Vector3 _velocity, _desiredVelocity;
    private Vector3 _contactNormal, _steepNormal;
    private bool _desiredJump;
    private int _groundContactCount, _steepContactCount;
    private bool ONGround => _groundContactCount > 0;
    private bool ONSteep => _steepContactCount > 0;
    private int _jumpPhase;
    private float _minGroundDotProduct, _minStairsDotProduct;
    private int _stepsSinceLastGrounded, _stepsSinceLastJump;
    private Vector3 upAxis, rightAxis, forwardAxis;

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
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis =
                ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }

        _desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        _desiredJump |= Input.GetButtonDown("Jump");
    }

    private void FixedUpdate()
    {
        var gravity = CustomGravity.GetGravity(_body.position, out upAxis);
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
        _contactNormal = _steepNormal = Vector3.zero;
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
            _contactNormal = upAxis;
        }
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

        if (!Physics.Raycast(_body.position, -upAxis, out var hit, probeDistance, probeMask)) //TODO Add more rays?
        {
            return false;
        }

        var upDot = Vector3.Dot(upAxis, hit.normal);
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

        return true;
    }

    private bool CheckSteepContacts () {
        if (_steepContactCount > 1) {
            _steepNormal.Normalize();
            var upDot = Vector3.Dot(upAxis, _steepNormal);
            if (upDot >= _minGroundDotProduct) {
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
        var xAxis = ProjectDirectionOnPlane(rightAxis, _contactNormal);
        var zAxis = ProjectDirectionOnPlane(forwardAxis, _contactNormal);

        var currentX = Vector3.Dot(_velocity, xAxis);
        var currentZ = Vector3.Dot(_velocity, zAxis);

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
            if (_jumpPhase == 0) {
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
        jumpDirection = (jumpDirection + upAxis).normalized;
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
            var upDot = Vector3.Dot(upAxis, normal);

            if (upDot >= minDot)
            {
                _groundContactCount += 1;
                _contactNormal += normal;
            }
            else if (upDot > -0.01f)
            {
                _steepContactCount += 1;
                _steepNormal += normal;
            }
        }
    }

    private static Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal) {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }

    private float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ? _minGroundDotProduct : _minStairsDotProduct;
    }
}