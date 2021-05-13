using System;
using UnityEngine;

public class GravityBox : GravitySource
{
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private Vector3 boundaryDistance = Vector3.one;
    [SerializeField, Min(0f)] private float innerDistance = 0f, innerFalloffDistance = 0f;
    [SerializeField, Min(0f)] private float outerDistance = 0f, outerFalloffDistance = 0f;

    private float _innerFalloffFactor, _outerFalloffFactor;

    public override Vector3 GetGravity(Vector3 position)
    {
        position = transform.InverseTransformDirection(position - transform.position);
        var vector = Vector3.zero;
        var outside = 0;
        if (position.x > boundaryDistance.x)
        {
            vector.x = boundaryDistance.x - position.x;
            outside = 1;
        }
        else if (position.x < -boundaryDistance.x)
        {
            vector.x = -boundaryDistance.x - position.x;
            outside = 1;
        }

        if (position.y > boundaryDistance.y)
        {
            vector.y = boundaryDistance.y - position.y;
            outside += 1;
        }
        else if (position.y < -boundaryDistance.y)
        {
            vector.y = -boundaryDistance.y - position.y;
            outside += 1;
        }

        if (position.z > boundaryDistance.z)
        {
            vector.z = boundaryDistance.z - position.z;
            outside += 1;
        }
        else if (position.z < -boundaryDistance.z)
        {
            vector.z = -boundaryDistance.z - position.z;
            outside += 1;
        }

        if (outside > 0)
        {
            var distance = outside == 1 ? Mathf.Abs(vector.x + vector.y + vector.z) : vector.magnitude;
            if (distance > outerFalloffDistance)
            {
                return Vector3.zero;
            }

            var g = gravity / distance;
            if (distance > outerDistance)
            {
                g *= 1f - (distance - outerDistance) * _outerFalloffFactor;
            }

            return transform.TransformDirection(g * vector);
        }

        Vector3 distances;
        distances.x = boundaryDistance.x - Mathf.Abs(position.x);
        distances.y = boundaryDistance.y - Mathf.Abs(position.y);
        distances.z = boundaryDistance.z - Mathf.Abs(position.z);
        if (distances.x < distances.y)
        {
            if (distances.x < distances.z)
            {
                vector.x = GetGravityComponent(position.x, distances.x);
            }
            else
            {
                vector.z = GetGravityComponent(position.z, distances.z);
            }
        }
        else if (distances.y < distances.z)
        {
            vector.y = GetGravityComponent(position.y, distances.y);
        }
        else
        {
            vector.z = GetGravityComponent(position.z, distances.z);
        }

        return transform.InverseTransformDirection(vector);
    }

    private float GetGravityComponent(float coordinate, float distance)
    {
        if (distance > innerFalloffDistance)
        {
            return 0f;
        }

        var g = gravity;
        if (distance > innerDistance)
        {
            g *= 1f - (distance - innerDistance) * _innerFalloffFactor;
        }

        return coordinate > 0f ? -g : g;
    }

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        boundaryDistance = Vector3.Max(boundaryDistance, Vector3.zero);
        var maxInner = Mathf.Min(
            Mathf.Min(boundaryDistance.x, boundaryDistance.y), boundaryDistance.z
        );
        innerDistance = Mathf.Min(innerDistance, maxInner);
        innerFalloffDistance =
            Mathf.Max(Mathf.Min(innerFalloffDistance, maxInner), innerDistance);
        outerFalloffDistance = Mathf.Max(outerFalloffDistance, outerDistance);

        _innerFalloffFactor = 1f / (innerFalloffDistance - innerDistance);
        _outerFalloffFactor = 1f / (outerFalloffDistance - outerDistance);
    }

    private void OnDrawGizmos()
    {
        var gravityBoxTransform = transform;
        Gizmos.matrix =
            Matrix4x4.TRS(gravityBoxTransform.position, gravityBoxTransform.rotation, Vector3.one);
        Vector3 size;
        if (innerFalloffDistance > innerDistance)
        {
            Gizmos.color = Color.cyan;
            size.x = 2f * (boundaryDistance.x - innerFalloffDistance);
            size.y = 2f * (boundaryDistance.y - innerFalloffDistance);
            size.z = 2f * (boundaryDistance.z - innerFalloffDistance);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        if (innerDistance > 0f)
        {
            Gizmos.color = Color.yellow;
            size.x = 2f * (boundaryDistance.x - innerDistance);
            size.y = 2f * (boundaryDistance.y - innerDistance);
            size.z = 2f * (boundaryDistance.z - innerDistance);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, 2f * boundaryDistance);
        if (outerDistance > 0f)
        {
            Gizmos.color = Color.yellow;
            DrawGizmosOuterCube(outerDistance);
        }

        if (outerFalloffDistance > outerDistance)
        {
            Gizmos.color = Color.cyan;
            DrawGizmosOuterCube(outerFalloffDistance);
        }
    }

    private void DrawGizmosOuterCube(float distance)
    {
        Vector3 a, b, c, d;
        a.y = b.y = boundaryDistance.y;
        d.y = c.y = -boundaryDistance.y;
        b.z = c.z = boundaryDistance.z;
        d.z = a.z = -boundaryDistance.z;
        a.x = b.x = c.x = d.x = boundaryDistance.x + distance;
        DrawGizmosRect(a, b, c, d);
        a.x = b.x = c.x = d.x = -a.x;
        DrawGizmosRect(a, b, c, d);

        a.x = d.x = boundaryDistance.x;
        b.x = c.x = -boundaryDistance.x;
        a.z = b.z = boundaryDistance.z;
        c.z = d.z = -boundaryDistance.z;
        a.y = b.y = c.y = d.y = boundaryDistance.y + distance;
        DrawGizmosRect(a, b, c, d);
        a.y = b.y = c.y = d.y = -a.y;
        DrawGizmosRect(a, b, c, d);

        a.x = d.x = boundaryDistance.x;
        b.x = c.x = -boundaryDistance.x;
        a.y = b.y = boundaryDistance.y;
        c.y = d.y = -boundaryDistance.y;
        a.z = b.z = c.z = d.z = boundaryDistance.z + distance;
        DrawGizmosRect(a, b, c, d);
        a.z = b.z = c.z = d.z = -a.z;
        DrawGizmosRect(a, b, c, d);

        distance *= 0.5773502692f;
        var size = boundaryDistance;
        size.x = 2f * (size.x + distance);
        size.y = 2f * (size.y + distance);
        size.z = 2f * (size.z + distance);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }

    private static void DrawGizmosRect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}