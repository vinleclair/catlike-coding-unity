using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class DetectionZone : MonoBehaviour
{
    [SerializeField] private UnityEvent onFirstEnter = default, onLastExit = default;

    private List<Collider> _colliders = new List<Collider>();

    private void Awake()
    {
        enabled = false;
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        if (enabled && gameObject.activeInHierarchy)
        {
            return;
        }
#endif
        if (_colliders.Count > 0)
        {
            _colliders.Clear();
            onLastExit.Invoke();
        }
    }

    private void FixedUpdate()
    {
        for (var i = 0; i < _colliders.Count; i++)
        {
            var collider = _colliders[i];
            if (!collider || !collider.gameObject.activeInHierarchy)
            {
                _colliders.RemoveAt(i--);
                if (_colliders.Count == 0)
                {
                    onLastExit.Invoke();
                    enabled = false;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_colliders.Count == 0)
        {
            onFirstEnter.Invoke();
            enabled = true;
        }

        _colliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_colliders.Remove(other) && _colliders.Count == 0)
        {
            onLastExit.Invoke();
            enabled = false;
        }
    }
}