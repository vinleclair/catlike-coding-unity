using UnityEngine;

public class GravitySphere : GravitySource
{
    [SerializeField] private float gravity = 9.81f;
    [SerializeField, Min(0f)] private float innerFallOffRadius = 1f, innerRadius = 5f;
    [SerializeField, Min(0f)] private float outerRadius = 10f, outerFalloffRadius = 15f;

    private float _innerFalloffFactor, _outerFalloffFactor;

    public override Vector3 GetGravity(Vector3 position)
    {
        var vector = transform.position - position;
        var distance = vector.magnitude;
        if (distance > outerFalloffRadius || distance < innerFallOffRadius)
        {
            return Vector3.zero;
        }

        var g = gravity / distance;
        if (distance > outerRadius)
        {
            g *= 1f - (distance - outerRadius) * _outerFalloffFactor;
        }
        else if (distance < innerRadius)
        {
            g *= 1f - (innerRadius - distance) * _innerFalloffFactor;
        }

        return g * vector;
    }

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        innerFallOffRadius = Mathf.Max(innerFallOffRadius, 0f);
        innerRadius = Mathf.Max(innerRadius, innerFallOffRadius);
        outerRadius = Mathf.Max(outerRadius, innerRadius);
        outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);

        _innerFalloffFactor = 1f / (innerRadius - innerFallOffRadius);
        _outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
    }

    private void OnDrawGizmos()
    {
        var p = transform.position;
        if (innerFallOffRadius > 0f && innerFallOffRadius < innerRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p, innerFallOffRadius);
        }

        Gizmos.color = Color.yellow;
        if (innerRadius > 0f && innerRadius < outerRadius)
        {
            Gizmos.DrawWireSphere(p, innerRadius);
        }

        Gizmos.DrawWireSphere(p, outerRadius);
        if (outerFalloffRadius > outerRadius)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p, outerFalloffRadius);
        }
    }
}