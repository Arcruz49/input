using System;
using Input.Models;

namespace Input.Services;

public interface IInputCapture
{
    event Action<InputEvent>? EventCaptured;

    void Start();
    void Stop();
}