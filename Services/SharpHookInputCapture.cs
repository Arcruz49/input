using SharpHook;
using SharpHook.Native;
using Input.Models;
using System;

namespace Input.Services;

public sealed class SharpHookInputCapture : IInputCapture, IDisposable
{
    private readonly TaskPoolGlobalHook _hook = new();

    public event Action<InputEvent>? EventCaptured;

    public SharpHookInputCapture()
    {
        _hook.KeyPressed += (_, e) =>
            Raise(InputEventType.KeyDown, keyOrButton: e.Data.KeyCode.ToString());

        _hook.KeyReleased += (_, e) =>
            Raise(InputEventType.KeyUp, keyOrButton: e.Data.KeyCode.ToString());

        _hook.MousePressed += (_, e) =>
            Raise(InputEventType.MouseDown, keyOrButton: e.Data.Button.ToString(), x: e.Data.X, y: e.Data.Y);

        _hook.MouseReleased += (_, e) =>
            Raise(InputEventType.MouseUp, keyOrButton: e.Data.Button.ToString(), x: e.Data.X, y: e.Data.Y);

        _hook.MouseMoved += (_, e) =>
            Raise(InputEventType.MouseMove, keyOrButton: null, x: e.Data.X, y: e.Data.Y);

        _hook.MouseWheel += (_, e) =>
            Raise(InputEventType.MouseWheel, keyOrButton: e.Data.Rotation.ToString(), x: e.Data.X, y: e.Data.Y);
    }

    public void Start()
    {
        _ = _hook.RunAsync();
    }

    public void Stop()
    {
        _hook.Stop();
    }

    private void Raise(InputEventType type, string? keyOrButton, short? x = null, short? y = null)
    {
        EventCaptured?.Invoke(new InputEvent
        {
            EventType = type,
            KeyOrButton = keyOrButton,
            X = x,
            Y = y
        });
    }

    public void Dispose()
    {
        _hook.Dispose();
    }
}