﻿using UnityEngine;
using static UnityEngine.Mathf;

public static class FunctionLibrary
{
    public delegate Vector3 Function(float u, float v, float t);

    public enum FunctionName
    {
        Wave,
        MultiWave,
        Ripple,
        Sphere,
        Torus
    }

    private static readonly Function[] Functions = {Wave, MultiWave, Ripple, Sphere, Torus};
    
    public static int FunctionCount => Functions.Length;

    public static Function GetFunction(FunctionName name)
    {
        return Functions[(int) name];
    }
    
    public static FunctionName GetNextFunctionName(FunctionName name)
    {
        return (int) name < Functions.Length - 1 ? name + 1 : 0;
    }

    public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
    {
        var choice = (FunctionName) Random.Range(1, Functions.Length);
        return choice == name ? 0 : choice;
    }

    public static Vector3 Morph(
        float u, float v, float t, Function from, Function to, float progress
    )
    {
        return Vector3.LerpUnclamped(
            from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress)
        );
    }

    private static Vector3 Wave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + v + t));
        p.z = v;
        return p;
    }

    private static Vector3 MultiWave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + 0.5f * t));
        p.y += 0.5f * Sin(2f * PI * (v + t));
        p.y += Sin(PI * (u + v + 0.25f * t));
        p.y *= 1f / 2.5f;
        p.z = v;
        return p;
    }

    private static Vector3 Ripple(float u, float v, float t)
    {
        var d = Sqrt(u * u + v * v);
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (4f * d - t));
        p.y /= 1f + 10f * d;
        p.z = v;
        return p;
    }

    private static Vector3 Sphere(float u, float v, float t)
    {
        var r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));
        var s = r * Cos(0.5f * PI * v);
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r * Sin(0.5f * PI * v);
        p.z = s * Cos(PI * u);
        return p;
    }

    private static Vector3 Torus(float u, float v, float t)
    {
        var r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        var r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));
        var s = r2 * Cos(PI * v) + r1;
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r2 * Sin(PI * v);
        p.z = s * Cos(PI * u);
        return p;
    }
    
    private static Vector3 Knot(float u, float v, float t)
    {
        u = u * PI + Sin(0.5f * t);
        Vector3 p;
        p.x = Sin(u) + 2 * Sin(2 * u);
        p.y = Cos(u) - 2 * Cos(2 * u);
        p.z = -Sin(3 * u);
        return p;
    }
}