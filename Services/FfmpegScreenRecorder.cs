using System;
using System.Diagnostics;
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
            StartInfo = new ProcessStartInfo("ffmpeg", args)
            {
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _process.Start();
    }

    public async Task StopAsync()
    {
        if (_process is null) return;

        await _process.StandardInput.WriteLineAsync("q");
        await _process.StandardInput.FlushAsync();

        await _process.WaitForExitAsync();
        _process.Dispose();
        _process = null;
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