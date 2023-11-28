using Content.Shared.Drugs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Drugs;

/// <summary>
///     System to handle drug related overlays.
/// </summary>
public sealed class DrugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private RainbowOverlay _overlay = default!;
    private MixedGrayscaleOverlay _mixedGrayscaleOverlay = default!;

    public static string RainbowKey = "SeeingRainbows";
    public static string MixedGrayscaleKey = "CrazyRussianDrug";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeeingRainbowsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SeeingRainbowsComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<SeeingRainbowsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SeeingRainbowsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<CrazyRussianDrugComponent, ComponentInit>(OnInitSG);
        SubscribeLocalEvent<CrazyRussianDrugComponent, ComponentShutdown>(OnShutdownSG);
        SubscribeLocalEvent<CrazyRussianDrugComponent, LocalPlayerAttachedEvent>(OnPlayerAttachedSG);
        SubscribeLocalEvent<CrazyRussianDrugComponent, LocalPlayerDetachedEvent>(OnPlayerDetachedSG);

        _overlay = new();
        _mixedGrayscaleOverlay = new MixedGrayscaleOverlay();
    }

    #region SeeingRainbow

    private void OnPlayerAttached(EntityUid uid, SeeingRainbowsComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, SeeingRainbowsComponent component, LocalPlayerDetachedEvent args)
    {
        _overlay.Intoxication = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(EntityUid uid, SeeingRainbowsComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, SeeingRainbowsComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlay.Intoxication = 0;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    #endregion

    #region SeeingGray

    private void OnPlayerAttachedSG(EntityUid uid, CrazyRussianDrugComponent component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_mixedGrayscaleOverlay);
    }

    private void OnPlayerDetachedSG(EntityUid uid, CrazyRussianDrugComponent component, LocalPlayerDetachedEvent args)
    {
        _mixedGrayscaleOverlay.Intoxication = 0;
        _overlayMan.RemoveOverlay(_mixedGrayscaleOverlay);
    }

    private void OnInitSG(EntityUid uid, CrazyRussianDrugComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_mixedGrayscaleOverlay);
    }

    private void OnShutdownSG(EntityUid uid, CrazyRussianDrugComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _mixedGrayscaleOverlay.Intoxication = 0;
            _overlayMan.RemoveOverlay(_mixedGrayscaleOverlay);
        }
    }

    #endregion
}
