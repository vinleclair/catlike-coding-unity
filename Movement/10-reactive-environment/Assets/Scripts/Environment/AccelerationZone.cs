using System;
using UnityEngine;

public class AccelerationZone : MonoBehaviour
{
    [SerializeField, Min(0f)] private float acceleration = 10f, speed = 10f;

    private void OnTriggerEnter(Collider other)
    {
        var body = other.attachedRigidbody;
        if (body)
        {
            Accelerate(body);
        }
    }

    private void OnTriggerStay (Collider other) {
        var body = other.attachedRigidbody;
        if (body) {
            Accelerate(body);
        }
    }

    private void Accelerate(Rigidbody body)
    {
        var velocity = transform.InverseTransformDirection(body.velocity);
        if (velocity.y >= speed)
        {
            return;
        }

        velocity.y = acceleration > 0f ? Mathf.MoveTowards(velocity.y, speed, acceleration * Time.deltaTime) : speed;
        body.velocity = transform.TransformDirection(velocity);
        
        if (body.TryGetComponent(out MovingSphere sphere)) {
            sphere.PreventSnapToGround();
        }
    }
}
