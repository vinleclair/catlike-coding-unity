using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] private float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 10f, maxAirAcceleration = 1f;
    [SerializeField, Range(0f, 10f)] private float jumpHeight = 2f;
    [SerializeField, Range(0, 5)] private int maxAirJumps = 0;
    [SerializeField, Range(0f, 90f)] private float maxGroundAngle = 25f;

    private Rigidbody _body;

    private Vector3 _velocity, _desiredVelocity;

    private Vector3 _contactNormal;

    private bool _desiredJump;

    private int _groundContactCount;

    private bool ONGround => _groundContactCount > 0;
    
    private int _jumpPhase;

    private float _minGroundDotProduct;
    
    private Renderer _renderer;
    
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
        OnValidate();
    }
    
    private void OnValidate()
    {
        _minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);

        _desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        _desiredJump |= Input.GetButtonDown("Jump");
        
        _renderer.material.SetColor(
            ColorID, Color.white * (_groundContactCount * 0.25f)
        );
    }

    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();
        
        if (_desiredJump)
        {
            _desiredJump = false;
            Jump();
        }

        _body.velocity = _velocity;

        ClearState();
    }

	private void ClearState () {
        _groundContactCount = 0;
		_contactNormal = Vector3.zero;
	}
	
    private void UpdateState()
    {
        _velocity = _body.velocity;
        if (ONGround)
        {
            _jumpPhase = 0;
            if (_groundContactCount > 1) {
                _contactNormal.Normalize();
            }
        }
        else
        {
            _contactNormal = Vector3.up;
        }
    }

    private void AdjustVelocity()
    {
        var xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        var zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        var currentX = Vector3.Dot(_velocity, xAxis);
        var currentZ = Vector3.Dot(_velocity, zAxis);

        var acceleration = ONGround ? maxAcceleration : maxAirAcceleration;
        var maxSpeedChange = acceleration * Time.deltaTime;

        var newX = Mathf.MoveTowards(currentX, _desiredVelocity.x, maxSpeedChange);
        var newZ = Mathf.MoveTowards(currentZ, _desiredVelocity.z, maxSpeedChange);

        _velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    private void Jump()
    {
        if (!ONGround && _jumpPhase >= maxAirJumps) return;

        _jumpPhase += 1;
        var jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
        var alignedSpeed = Vector3.Dot(_velocity, _contactNormal);
        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - _velocity.y, 0f);
        }

        _velocity += _contactNormal * jumpSpeed;
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
        for (var i = 0; i < collision.contactCount; i++)
        {
            var normal = collision.GetContact(i).normal;

            if (!(normal.y >= _minGroundDotProduct)) continue;
            
            _groundContactCount += 1;
            _contactNormal += normal;
        }
    }

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - _contactNormal * Vector3.Dot(vector, _contactNormal);
    }
}