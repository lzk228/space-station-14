namespace Content.Shared.Players.PlayTimeTracking;

public static class PlayTimeTrackingShared
{
    /// <summary>
    /// The prototype ID of the play time tracker that represents overall playtime, i.e. not tied to any one role.
    /// </summary>
    public const string TrackerOverall = "Overall";

    //A-13 AGhost Tracking Time system start
    /// <summary>
    /// Tracks admin time.
    /// </summary>
    public const string TrackerAdmin = "AdminTime";

    /// <summary>
    /// Tracks aghost time.
    /// </summary>
    public const string TrackerAGhost = "AGhostTime";

    /// <summary>
    /// Tracks obs time.
    /// </summary>
    public const string TrackerObserver = "ObserverTime";
    //A-13 AGhost Tracking Time system end
}
