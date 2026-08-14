using CommunityToolkit.Mvvm.ComponentModel;
using Input.ViewModels;

namespace Input.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LoginViewModel _loginViewModel;
    private readonly RecordingViewModel _recordingViewModel;

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    public MainWindowViewModel(LoginViewModel loginViewModel, RecordingViewModel recordingViewModel)
    {
        _loginViewModel = loginViewModel;
        _recordingViewModel = recordingViewModel;

        _loginViewModel.LoginSucceeded += () => CurrentViewModel = _recordingViewModel;

        CurrentViewModel = _loginViewModel;
    }
}