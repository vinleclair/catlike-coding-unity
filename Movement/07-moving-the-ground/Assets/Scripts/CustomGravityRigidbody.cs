using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomGravityRigidbody : MonoBehaviour
{
    [SerializeField] private bool floatToSleep = false;
    
    private Rigidbody _body;
    private float _floatDelay;

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

        _body.AddForce(CustomGravity.GetGravity(_body.position), ForceMode.Acceleration);
    }
}