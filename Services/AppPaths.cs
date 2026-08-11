using System;
using System.IO;

namespace Input.Services;

/// <summary>
/// Fonte única dos caminhos usados pelo app. Todo mundo que precisa do banco
/// ou da pasta de gravações pede aqui — assim writer, store e exporter nunca
/// apontam pra arquivos diferentes.
/// </summary>
public sealed class AppPaths
{
    public string RootDirectory { get; }
    public string RecordingsDirectory { get; }
    public string DatabasePath { get; }

    public AppPaths()
    {
        RootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Input");
        RecordingsDirectory = Path.Combine(RootDirectory, "recordings");
        DatabasePath = Path.Combine(RootDirectory, "Input.db");

        Directory.CreateDirectory(RecordingsDirectory);
    }
}
