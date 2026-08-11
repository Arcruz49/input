using System.Threading.Tasks;

namespace Input.Services;

public interface IScreenRecorder
{
    void Start(string outputPath);
    Task StopAsync();
}