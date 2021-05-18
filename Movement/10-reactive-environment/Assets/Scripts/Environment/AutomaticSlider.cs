using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 1f;
    [SerializeField] private bool autoReverse = false, smoothStep = false;
    [System.Serializable]
    public class OnValueChangedEvent : UnityEvent<float> { }
    [SerializeField] private OnValueChangedEvent onValueChanged = default;

    private float _value;

    public bool Reversed { get; set; }

    public bool AutoReverse {
        get => autoReverse;
        set => autoReverse = value;
    }

    private float SmoothedValue => 3f * _value * _value - 2f * _value * _value * _value;

    private void FixedUpdate()
    {
        var delta = Time.deltaTime / duration;
        if (Reversed)
        {
            _value -= delta;
            if (_value <= 0f)
            {
                if (autoReverse)
                {
                    _value = Mathf.Min(1f, -_value);
                    Reversed = false;
                }
                else
                {
                    _value = 0f;
                    enabled = false;
                }
            }
        }
        else
        {
            _value += delta;
            if (_value >= 1f)
            {
                if (autoReverse)
                {
                    _value = Mathf.Max(0f, 2f - _value);
                    Reversed = true;
                }
                else
                {
                    _value = 1f;
                    enabled = false;
                }
            }
        }
        onValueChanged.Invoke(smoothStep ? SmoothedValue : _value);
    }
}