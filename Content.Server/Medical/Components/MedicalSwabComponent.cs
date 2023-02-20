using System.Threading;

namespace Content.Server.Medical.Components;

[RegisterComponent]
public sealed class MedicalSwabComponent : Component
{
    [DataField("swabDelay")] public float SwabDelay = 2f;
    public CancellationTokenSource? CancelToken;
    public bool IsSolutionAdded;
}
