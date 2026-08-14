using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Input.Services;

public sealed class FfmpegScreenRecorder : IScreenRecorder
{
    private Process? _process;

    public void Start(string outputPath)
    {
        if (_process is not null)
            throw new InvalidOperationException("Gravação já está em andamento.");

        var args = BuildFfmpegArgs(outputPath);

        _process = new Process
        {
            StartInfo = new ProcessStartInfo(ResolveFfmpegPath(), args)
            {
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.ErrorDataReceived += (_, _) => { };

        _process.Start();
        _process.BeginErrorReadLine();
    }

    public async Task StopAsync()
    {
        if (_process is null) return;

        await _process.StandardInput.WriteLineAsync("q");
        await _process.StandardInput.FlushAsync();

        var waitForExit = _process.WaitForExitAsync();
        await Task.WhenAny(waitForExit, Task.Delay(TimeSpan.FromSeconds(5)));

        if (!_process.HasExited)
            _process.Kill(entireProcessTree: true);

        _process.Dispose();
        _process = null;
    }

    private static string ResolveFfmpegPath()
    {
        var relativePath = OperatingSystem.IsWindows()
            ? Path.Combine("Tools", "ffmpeg", "win-x64", "ffmpeg.exe")
            : Path.Combine("Tools", "ffmpeg", "linux-x64", "ffmpeg");

        var bundledPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        return File.Exists(bundledPath) ? bundledPath : "ffmpeg";
    }

    private static string BuildFfmpegArgs(string outputPath)
    {
        if (OperatingSystem.IsWindows())
            return $"-f gdigrab -framerate 30 -i desktop -y \"{outputPath}\"";

        if (OperatingSystem.IsLinux())
        {
            var display = Environment.GetEnvironmentVariable("DISPLAY") ?? ":0.0";
            return $"-f x11grab -framerate 30 -i {display} -y \"{outputPath}\"";
        }

        throw new PlatformNotSupportedException("Apenas Windows e Linux (X11) são suportados.");
    }
}