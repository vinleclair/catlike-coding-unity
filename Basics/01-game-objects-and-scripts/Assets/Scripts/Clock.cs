using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private const float HoursToDegrees = -30f, MinutesToDegrees = -6f, SecondsToDegrees = -6f;

    [SerializeField] private Transform hoursPivot, minutesPivot, secondsPivot;

    private void Update()
    {
        TimeSpan currentTime = DateTime.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(0f, 0f, HoursToDegrees * (float)currentTime.TotalHours);
        minutesPivot.localRotation = Quaternion.Euler(0f, 0f, MinutesToDegrees * (float)currentTime.TotalMinutes);
        secondsPivot.localRotation = Quaternion.Euler(0f, 0f, SecondsToDegrees * (float)currentTime.TotalSeconds);
    }
}