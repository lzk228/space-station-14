using Content.Shared.Andromeda.AndromedaSponsorService; // A-13 Sponsor service
using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<MarkingManager, MarkingManager>();
            IoCManager.Register<ContentLocalizationManager, ContentLocalizationManager>();
            IoCManager.Register<GetSponsorAllowedMarkingsMethod>(); // A-13 Sponsor service
        }
    }
}
