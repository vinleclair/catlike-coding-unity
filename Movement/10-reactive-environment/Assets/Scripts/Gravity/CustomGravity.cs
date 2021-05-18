using System;
using UnityEngine;
using System.Collections.Generic;

public static class CustomGravity
{
    private static readonly List<GravitySource> Sources = new List<GravitySource>();

    public static void Register(GravitySource source)
    {
        Debug.Assert(
            !Sources.Contains(source),
            "Duplicate registration of gravity source!", source
        );
        Sources.Add(source);
    }

    public static void Unregister(GravitySource source)
    {
        Debug.Assert(
            Sources.Contains(source),
            "Unregistration of unknown gravity source!", source
        );
        Sources.Remove(source);
    }
    
    public static Vector3 GetGravity (Vector3 position)
    {
        var g = Vector3.zero;
        
        for (var i = 0; i < Sources.Count; i++)
        {
            g += Sources[i].GetGravity(position);
        }

        return g;
    }

    public static Vector3 GetGravity (Vector3 position, out Vector3 upAxis)
    {
        var g = Vector3.zero;
        
        for (var i = 0; i < Sources.Count; i++)
        {
            g += Sources[i].GetGravity(position);
        }

        upAxis = -g.normalized;
        return g;
    }

    public static Vector3 GetUpAxis (Vector3 position)
    {
        var g = Vector3.zero;
        
        for (var i = 0; i < Sources.Count; i++)
        {
            g += Sources[i].GetGravity(position);
        }

        return -g.normalized;
    }
}