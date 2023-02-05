namespace Content.Server.Paper;

[RegisterComponent]
public sealed class StampStateHandlerComponent : Component
{
    [DataField("stampStateCollection")]
    public string[] StampStateCollection { get; } = { "paper_stamp-generic"};

    [DataField("stampNameCollection")]
    public string[] StampNameCollection { get; } = { "stamp-component-stamped-name-default" };

    public int CurrentStateIndex { get; set; } = 0;
}
