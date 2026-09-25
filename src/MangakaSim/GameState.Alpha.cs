using System.Text.Json;
using System.Text.Json.Nodes;

namespace MangakaSim;

public partial class GameState
{
    public static GameState ImportAlphaV7(string json)
    {
        try
        {
            var node = JsonNode.Parse(json)!.AsObject();
            if (node[nameof(Version)]?.GetValue<int>() != 7) throw new InvalidDataException("Choose a version-7 career.");
            try { FromJson(json); }
            catch (InvalidDataException ex) when (ex.Message.StartsWith("Save file version 7 is not supported.", StringComparison.Ordinal)) { }
            var state = JsonSerializer.Deserialize<GameState>(json, JsonOptions)!;
            state.ValidateSave();
            // Additive format change: preserve the original replay boundary and every log entry.
            // Recursively update the single non-nested checkpoint without resetting the timeline.
            if (state.World.ReplayCheckpoint is {} checkpoint)
                state.World.ReplayCheckpoint = ImportAlphaV7(checkpoint).ToJson();
            state.Version = CurrentVersion;
            state.ValidateSave();
            return state;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException or NullReferenceException)
        { throw new InvalidDataException("Could not import this career.", ex); }
    }
}
