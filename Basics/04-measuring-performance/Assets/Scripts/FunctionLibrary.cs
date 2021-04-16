using System;
using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary {

    public delegate Vector3 Function (float u, float v, float t);

    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus, Knot }

    private static readonly Function[] Functions = { Wave, MultiWave, Ripple, Sphere, Torus, Knot };

    public static Function GetFunction (FunctionName name) {
        return Functions[(int)name];
    }

    private static Vector3 Wave (float u, float v, float t) {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + v + t));
        p.z = v;
        return p;
    }

    private static Vector3 MultiWave (float u, float v, float t) {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + 0.5f * t));
        p.y += 0.5f * Sin(2f * PI * (v + t));
        p.y += Sin(PI * (u + v + 0.25f * t));
        p.y *= 1f / 2.5f;
        p.z = v;
        return p;
    }

    private static Vector3 Ripple (float u, float v, float t) {
        var d = Sqrt(u * u + v * v);
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (4f * d - t));
        p.y /= 1f + 10f * d;
        p.z = v;
        return p;
    }

    private static Vector3 Sphere (float u, float v, float t) {
        var r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
        var s = r * Cos(0.5f * PI * v);
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r * Sin(0.5f * PI * v);
        p.z = s * Cos(PI * u);
        return p;
    }

    private static Vector3 Torus (float u, float v, float t) {
        var r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        var r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
        var s = r2 * Cos(PI * v) + r1;
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r2 * Sin(PI * v);
        p.z = s * Cos(PI * u);
        return p;
    }
   
    private static Vector3 Knot (float u, float v, float t)
    {
        u *= PI;
        v *= t;
        const int r = 5;
        Vector3 p;
        p.x = r * Sin(3 * u) / (2 + Cos(v));
        p.y = (Sin(u) + 2 * Sin(2 * u)) / (2 + Cos(v + PI * 2 / 3));
        p.z = (Cos(u) - 2 * Cos(2 * u)) * (2 + Cos(v)) * (2 + Cos(v + PI * 2 / 3)) / 4;
        return p;
    }
}