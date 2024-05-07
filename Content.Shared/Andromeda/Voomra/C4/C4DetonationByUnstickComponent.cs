using Robust.Shared.GameStates;

namespace Content.Shared.Andromeda.Voomra.C4;

/// <summary>
///     Компонент-маркер для детонации C4 при попытке открепить её
/// </summary>
/// <seealso cref="T:Content.Shared.Andromeda.Voomra.C4.C4DetonationByUnstickSystem"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class C4DetonationByUnstickComponent : Component
{
    /// <summary>
    ///     Альтернатива устаревшему <see cref="Robust.Shared.GameObjects.IComponent.Owner">IComponent.Owner</see>
    /// </summary>
    [ViewVariables]
    public EntityUid Owner2 { get; set; } = EntityUid.Invalid;

    /// <summary>
    ///     Флаг детонации при попытке снятия
    /// </summary>
    [ViewVariables]
    public bool Detonation { get; set; } = false;
}
