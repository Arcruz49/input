using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Input.Data;
using Input.Services;
using Input.ViewModels;
using Input.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Input;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var paths = new AppPaths();

        services.AddSingleton(paths);
        services.AddSingleton<IScreenRecorder, FfmpegScreenRecorder>();
        services.AddSingleton<IInputCapture, SharpHookInputCapture>();
        services.AddSingleton(_ => new SessionStore(paths.DatabasePath));
        services.AddSingleton(_ => new InputEventExporter(paths.DatabasePath));
        services.AddSingleton<RecordingOrchestrator>();
        services.AddSingleton<MainWindowViewModel>();
    }
}