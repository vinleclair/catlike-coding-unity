using System;
using TMPro;
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    private enum DisplayMode
    {
        FPS,
        MS
    }
    
    [SerializeField] private TextMeshProUGUI display;
    [SerializeField] private DisplayMode displayMode = DisplayMode.FPS;
    [SerializeField, Range(0.1f, 2f)] private float sampleDuration = 1f;

    private int _frames;
    private float _duration, _bestDuration = float.MaxValue, _worstDuration;

    private void Update()
    {
        var frameDuration = Time.unscaledDeltaTime;
        _frames += 1;
        _duration += frameDuration;
        
    if (frameDuration < _bestDuration) {
        _bestDuration = frameDuration;
    }
    if (frameDuration > _worstDuration) {
        _worstDuration = frameDuration;
    }
    
    if (!(_duration >= sampleDuration)) return;

    if (displayMode == DisplayMode.FPS)
    {
        display.SetText(
            "FPS\n{0:0}\n{1:0}\n{2:0}", 
            1f / _bestDuration,
            _frames / _duration, 
            1f / _worstDuration
            ); 
    }
    else
    {
        display.SetText(
            "MS\n{0:1}\n{1:1}\n{2:1}",
            1000f * _bestDuration,
            1000f * _duration / _frames,
            1000f * _worstDuration
        );
    }
    _frames = 0;
    _duration = 0f;
    _bestDuration = float.MaxValue;
    _worstDuration = 0f;
    }
}