using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Input.Data;
using Input.Models;

namespace Input.Services;

public sealed class RecordingOrchestrator : IAsyncDisposable
{
    private readonly IScreenRecorder _screenRecorder;
    private readonly IInputCapture _inputCapture;
    private readonly SessionStore _sessionStore;
    private readonly InputEventExporter _exporter;
    private readonly AppPaths _paths;

    private InputEventWriter? _eventWriter;
    private readonly Stopwatch _clock = new();
    private long _currentSessionId;
    private string _currentVideoPath = string.Empty;
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public RecordingOrchestrator(
        IScreenRecorder screenRecorder,
        IInputCapture inputCapture,
        SessionStore sessionStore,
        InputEventExporter exporter,
        AppPaths paths)
    {
        _screenRecorder = screenRecorder;
        _inputCapture = inputCapture;
        _sessionStore = sessionStore;
        _exporter = exporter;
        _paths = paths;
        _inputCapture.EventCaptured += OnInputEventCaptured;
    }

    public void Start()
    {
        if (_isRecording)
            throw new InvalidOperationException("Já existe uma gravação em andamento.");

        var videoPath = Path.Combine(
            _paths.RecordingsDirectory,
            $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4");

        _currentSessionId = _sessionStore.CreateSession(videoPath);
        _currentVideoPath = videoPath;
        _eventWriter = new InputEventWriter(_paths.DatabasePath);

        // A ordem importa: o clock começa a contar ANTES dos dois serviços
        // iniciarem de fato, garantindo que nenhum evento fique "antes do zero".
        _clock.Restart();
        _screenRecorder.Start(videoPath);
        _inputCapture.Start();

        _isRecording = true;
    }

    public async Task StopAsync()
    {
        if (!_isRecording) return;

        _inputCapture.Stop();
        await _screenRecorder.StopAsync();

        if (_eventWriter is not null)
            await _eventWriter.DisposeAsync();

        _sessionStore.CloseSession(_currentSessionId);
        await _exporter.ExportSessionAsync(_currentSessionId, _currentVideoPath);

        _clock.Stop();
        _isRecording = false;
    }

    private void OnInputEventCaptured(InputEvent ev)
    {
        if (!_isRecording || _eventWriter is null) return;

        ev.SessionId = _currentSessionId;
        ev.OffsetMs = _clock.ElapsedMilliseconds;
        _eventWriter.Enqueue(ev);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRecording)
            await StopAsync();

        _inputCapture.EventCaptured -= OnInputEventCaptured;
    }
}