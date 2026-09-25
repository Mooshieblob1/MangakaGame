using System;
using System.Diagnostics;
using System.IO;
using Godot;

namespace MangakaGame;

/// <summary>Development captures only. Report attachments and artwork retain their supported formats.</summary>
internal static class ScreenshotCapture
{
    internal static void SaveAvif(Image image,string output)
    {
        using var source=typeof(ScreenshotCapture).Assembly.GetManifestResourceStream("MangakaGame.ScreenshotAvif")
            ??throw new InvalidOperationException("The screenshot encoder is missing.");
        using var reader=new StreamReader(source);
        var start=new ProcessStartInfo(System.Environment.GetEnvironmentVariable("MANGAKA_SCREENSHOT_PYTHON")??"python")
        {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
        start.ArgumentList.Add("-c");start.ArgumentList.Add(reader.ReadToEnd());start.ArgumentList.Add(Path.GetFullPath(output));
        using var encoder=Process.Start(start)??throw new InvalidOperationException("Could not start the AVIF screenshot encoder.");
        var errors=encoder.StandardError.ReadToEndAsync();var messages=encoder.StandardOutput.ReadToEndAsync();
        // PNG is only an in-memory interchange buffer; no PNG screenshot is written.
        encoder.StandardInput.BaseStream.Write(image.SavePngToBuffer());encoder.StandardInput.Close();
        if(!encoder.WaitForExit(60000)){encoder.Kill(true);throw new InvalidOperationException("AVIF screenshot encoding timed out.");}
        if(encoder.ExitCode!=0)throw new InvalidOperationException("AVIF screenshot encoding failed. Use Python with Pillow AVIF support. "+errors.GetAwaiter().GetResult());
        messages.GetAwaiter().GetResult();
    }
}
