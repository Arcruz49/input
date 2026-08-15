using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Input.Services;

namespace Input.ViewModels;

public partial class RecordingViewModel : ViewModelBase
{
    private readonly RecordingOrchestrator _orchestrator;
    private readonly ApiClient _api;
    private readonly Stopwatch _clock = new();
    private readonly DispatcherTimer _ticker;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    private bool _isRecording;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    [NotifyPropertyChangedFor(nameof(ShowFooter))]
    private bool _isSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFooter))]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _statusIsError;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateLabel))]
    [NotifyPropertyChangedFor(nameof(ShowFooter))]
    private bool _hasRecordingToSend;

    [ObservableProperty]
    private string _elapsedText = "00:00";

    /// <summary>Texto da pílula de status no topo da tela.</summary>
    public string StateLabel =>
        IsRecording ? "Gravando"
        : IsSending ? "Enviando"
        : HasRecordingToSend ? "Gravação pronta"
        : "Pronto pra gravar";

    public bool ShowFooter =>
        HasRecordingToSend || IsSending || !string.IsNullOrEmpty(StatusText);

    /// <summary>Dica logo abaixo do botão redondo.</summary>
    public string HintText =>
        IsRecording ? "Clique para parar a gravação"
        : "Clique para começar a gravar a tela";

    public RecordingViewModel(RecordingOrchestrator orchestrator, ApiClient api)
    {
        _orchestrator = orchestrator;
        _api = api;

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _ticker.Tick += (_, _) => ElapsedText = Format(_clock.Elapsed);
    }

    private static string Format(TimeSpan t) =>
        t.TotalHours >= 1
            ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";

    [RelayCommand(CanExecute = nameof(CanToggle))]
    private async Task Toggle()
    {
        if (IsRecording)
            await Stop();
        else
            Start();
    }

    private bool CanToggle() => !IsSending;

    private void Start()
    {
        try
        {
            _orchestrator.Start();
            IsRecording = true;
            HasRecordingToSend = false;
            SetStatus(string.Empty);

            _clock.Restart();
            ElapsedText = "00:00";
            _ticker.Start();
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao iniciar: {ex.Message}", isError: true);
        }
        finally
        {
            OnPropertyChanged(nameof(HintText));
        }
    }

    private async Task Stop()
    {
        try
        {
            await _orchestrator.StopAsync();
            IsRecording = false;
            HasRecordingToSend = true;
            SetStatus("Dê um nome ao projeto para enviar a gravação.");
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao parar: {ex.Message}", isError: true);
        }
        finally
        {
            _ticker.Stop();
            _clock.Stop();
            OnPropertyChanged(nameof(HintText));
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task Send()
    {
        var videoPath = _orchestrator.LastVideoPath;
        var eventsPath = _orchestrator.LastEventsPath;

        if (videoPath is null || eventsPath is null)
        {
            SetStatus("Nenhuma gravação para enviar.", isError: true);
            return;
        }

        if (!File.Exists(videoPath) || !File.Exists(eventsPath))
        {
            SetStatus("Arquivos da gravação não encontrados no disco.", isError: true);
            return;
        }

        IsSending = true;
        SetStatus("Enviando para API...");
        try
        {
            await _api.UploadRecordingAsync(ProjectName, videoPath, eventsPath);
            SetStatus("Enviado com sucesso!");
            HasRecordingToSend = false;
            ProjectName = string.Empty;
            ElapsedText = "00:00";
        }
        catch (ApiException ex)
        {
            SetStatus(ex.Message, isError: true);
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao enviar: {ex.Message}", isError: true);
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSend() =>
        HasRecordingToSend &&
        !IsSending &&
        !IsRecording &&
        !string.IsNullOrWhiteSpace(ProjectName);

    private void SetStatus(string text, bool isError = false)
    {
        StatusText = text;
        StatusIsError = isError;
    }

    partial void OnIsRecordingChanged(bool value)
    {
        ToggleCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HintText));
    }

    partial void OnIsSendingChanged(bool value)
    {
        ToggleCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasRecordingToSendChanged(bool value) => SendCommand.NotifyCanExecuteChanged();
    partial void OnProjectNameChanged(string value) => SendCommand.NotifyCanExecuteChanged();
}
