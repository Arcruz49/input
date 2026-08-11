using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Input.Services;

namespace Input.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly RecordingOrchestrator _orchestrator;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private string _statusText = "Pronto pra gravar";

    public MainWindowViewModel(RecordingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        try
        {
            _orchestrator.Start();
            IsRecording = true;
            StatusText = "Gravando...";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro ao iniciar: {ex.Message}";
        }
    }

    private bool CanStart() => !IsRecording;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task Stop()
    {
        try
        {
            await _orchestrator.StopAsync();
            IsRecording = false;
            StatusText = "Gravação finalizada";
        }
        catch (Exception ex)
        {
            StatusText = $"Erro ao parar: {ex.Message}";
        }
    }

    private bool CanStop() => IsRecording;

    partial void OnIsRecordingChanged(bool value)
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}