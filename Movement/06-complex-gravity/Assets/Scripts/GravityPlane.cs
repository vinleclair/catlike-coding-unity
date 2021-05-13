using System;
using UnityEngine;
public class GravityPlane : GravitySource
{
    [SerializeField] private float gravity = 9.81f;
    [SerializeField, Min(0f)] private float range = 1f;

    public override Vector3 GetGravity(Vector3 position)
    {
        var gravityPlaneTransform = transform;
        var up = gravityPlaneTransform.up;
        var distance = Vector3.Dot(up, position - gravityPlaneTransform.position);
        if (distance > range)
        {
            return Vector3.zero;
        }

        var g = -gravity;
        if (distance > 0f)
        {
            g *= 1f - distance / range;
        }
        
        return g * up;
    }

    private void OnDrawGizmos()
    {
        var gravityPlaneTransform = transform;
        var scale = gravityPlaneTransform.localScale;
        scale.y = range;
        Gizmos.matrix = Matrix4x4.TRS(gravityPlaneTransform.position, gravityPlaneTransform.rotation, scale);
        var size = new Vector3(1f, 0f, 1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, size);
        if (range > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.up, size);
        }
    }
}
