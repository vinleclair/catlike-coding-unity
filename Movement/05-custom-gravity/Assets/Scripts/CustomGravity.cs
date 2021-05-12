using UnityEngine;

public static class CustomGravity {

    public static Vector3 GetGravity (Vector3 position) {
        return position.normalized * Physics.gravity.y;
    }

    public static Vector3 GetGravity (Vector3 position, out Vector3 upAxis) {
        var up = position.normalized;
        upAxis = Physics.gravity.y < 0f ? up : -up;
        return up * Physics.gravity.y;
    }

    public static Vector3 GetUpAxis (Vector3 position) {
        var up = position.normalized;
        return Physics.gravity.y < 0f ? up : -up;
    }
}