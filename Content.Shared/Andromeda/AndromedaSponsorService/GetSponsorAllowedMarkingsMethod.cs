using Robust.Shared.ContentPack;

namespace Content.Shared.Andromeda.AndromedaSponsorService;

public sealed class GetSponsorAllowedMarkingsMethod
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    private readonly string _sponsorsFilePath = "/Prototypes/Andromeda/sponsors.txt";

    public bool FileIsValid()
    {
        string fileContent = _resourceManager.ContentFileReadAllText(_sponsorsFilePath);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public bool IsSponsor(Guid userId)
    {
        string fileContent = _resourceManager.ContentFileReadAllText(_sponsorsFilePath);
        string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            string[] parts = line.Split(';');

            if (Guid.TryParse(parts[0], out Guid userGuid) && userGuid == userId)
            {
                return true;
            }
        }

        return false;
    }

    public bool GetSponsorAllowedMarkings(Guid userId)
    {
        string fileContent = _resourceManager.ContentFileReadAllText(_sponsorsFilePath);
        string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return false;
        }

        foreach (string line in lines)
        {
            string[] parts = line.Split(';');

            if (Guid.TryParse(parts[0], out Guid userGuid) && userGuid == userId)
            {
                bool allowedMarkings;

                if (bool.TryParse(parts[3], out allowedMarkings))
                {
                    return allowedMarkings;
                }
            }
        }

        return false;
    }
}