using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] private Transform pointPrefab;
    [SerializeField, Range(10, 100)] private int resolution = 10;
    private Transform[] points;

    private void Awake()
    {
        var step = 2f / resolution;
        var scale = Vector3.one * step;
        var position = Vector3.zero;
        points = new Transform[resolution];
        for (var i = 0; i < points.Length; i++)
        {
            var point = Instantiate(pointPrefab, transform, false);
            position.x = (i + 0.5f) * step - 1f;
            point.localPosition = position; 
            point.localScale = scale;
            points[i] = point;
        }
    }

    private void Update ()
    {
        var time = Time.time;
        foreach (var point in points)
        {
            var position = point.localPosition;
            position.y =  Mathf.Sin(Mathf.PI * (position.x + time)); 
            point.localPosition = position;
        }
    }
}
