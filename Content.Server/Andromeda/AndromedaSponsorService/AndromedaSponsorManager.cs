using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.Andromeda.AndromedaSponsorService;

public sealed class AndromedaSponsorManager
{
    private readonly HashSet<Guid> _sponsors = new HashSet<Guid>();
    private readonly string _sponsorsFilePath = "Resources/Prototypes/Andromeda/sponsors.txt";

    public void LoadSponsors()
    {
        if (!File.Exists(_sponsorsFilePath))
        {
            File.WriteAllText(_sponsorsFilePath, string.Empty);
        }

        _sponsors.Clear();

        foreach (var line in File.ReadLines(_sponsorsFilePath))
        {
            string[] parts = line.Split(';');

            if (Guid.TryParse(parts[0], out var guid))
            {
                _sponsors.Add(guid);
            }
        }
    }

    public void SaveSponsors(Guid userId, bool? allowedAntag = null, string? color = null)
    {
        var lines = File.ReadAllLines(_sponsorsFilePath).ToList();
        var index = lines.FindIndex(line => line.StartsWith(userId.ToString()));

        if (index != -1)
        {
            lines[index] = $"{userId};{allowedAntag ?? false};{color ?? ""}";
        }
        else
        {
            lines.Add($"{userId};{allowedAntag ?? false};{color ?? ""}");
        }

        File.WriteAllLines(_sponsorsFilePath, lines);
    }

    public bool IsSponsor(Guid userId)
    {
        return _sponsors.Contains(userId);
    }

    public List<Guid> GetActiveSponsors()
    {
        return new List<Guid>(_sponsors);
    }

    public void AddSponsor(Guid userId, bool allowedAntag, string color)
    {
        _sponsors.Add(userId);

        SaveSponsors(userId, allowedAntag, color);
    }

    public void RemoveSponsor(Guid userId)
    {
        _sponsors.Remove(userId);

        var lines = File.ReadAllLines(_sponsorsFilePath).ToList();
        var index = lines.FindIndex(line => line.StartsWith(userId.ToString()));

        if (index != -1)
        {
            lines.RemoveAt(index);
            File.WriteAllLines(_sponsorsFilePath, lines);
        }
    }

    public bool GetSponsorAllowedAntag(Guid userId)
    {
        string[] lines = File.ReadAllLines(_sponsorsFilePath);

        foreach (string line in lines)
        {
            string[] parts = line.Split(';');

            if (Guid.Parse(parts[0]) == userId)
            {
                bool allowedAntag;
                if (bool.TryParse(parts[1], out allowedAntag))
                {
                    return allowedAntag;
                }
            }
        }

        return false;
    }

    public Color? GetSponsorOocColor(Guid userId)
    {
        string[] lines = File.ReadAllLines(_sponsorsFilePath);
        foreach (string line in lines)
        {
            string[] parts = line.Split(';');

            if (Guid.Parse(parts[0]) == userId)
            {
                if (string.IsNullOrWhiteSpace(parts[2]))
                {
                    return null;
                }

                Color color;
                if (Color.TryParse(parts[2], out color))
                {
                    return color;
                }
            }
        }

        return null;
    }

    public bool IsValidColor(string color)
    {
        return Regex.IsMatch(color, @"^#[0-9A-Fa-f]{6}$");
    }
}