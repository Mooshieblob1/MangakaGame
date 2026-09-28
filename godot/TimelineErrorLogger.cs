using System.Collections.Generic;
using Godot;

namespace MangakaGame;

// T1.7: listens to Godot's error stream, where unexpected C# exceptions from callbacks also land, so they reach the
// session timeline instead of only the engine log. Called from any thread: it only queues text, never calls Godot.
public partial class TimelineErrorLogger : Logger
{
    private const int MaxPerSession = 20;
    private readonly object _gate = new();
    private readonly Queue<string> _pending = new();
    private readonly HashSet<string> _seen = new();

    public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify,
        int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType is not ((int)ErrorType.Error or (int)ErrorType.Script)) return;
        var text = string.IsNullOrWhiteSpace(rationale) ? code : rationale;
        var first = (text ?? "").Split('\n')[0].Trim();
        if (first.Length > 300) first = first[..300];
        lock (_gate)
        {
            if (_seen.Count >= MaxPerSession || !_seen.Add(function + "|" + first)) return;
            // C# exceptions all arrive through the engine's own LogException frame, which says nothing useful.
            var where = function.Contains("ExceptionUtils.LogException") ? "" : function;
            _pending.Enqueue(string.IsNullOrEmpty(where) ? first : where + ": " + first);
        }
    }

    public bool TryTake(out string text)
    {
        lock (_gate) return _pending.TryDequeue(out text!);
    }
}
