namespace MangakaSim;
/// <summary>Which Helper-Chan messages are new since the timeline last looked. A replaced or trimmed thread logs only its newest message.</summary>
public static class TimelineGuidance
{
    public static IReadOnlyList<string> NewSteps(IReadOnlyList<GuidanceMessage> thread,GuidanceMessage? lastSeen)
    {
        if(lastSeen is null)return thread.Select(m=>m.Step).ToList();
        for(var i=thread.Count-1;i>=0;i--)if(ReferenceEquals(thread[i],lastSeen))return thread.Skip(i+1).Select(m=>m.Step).ToList();
        return thread.Count>0?[thread[^1].Step]:[];
    }
}
