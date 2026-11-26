namespace RuleManagement.Dto;

using GameManagement;

public class GameRuleDto : RuleDto, IRuleDto
{
    public List<string> Games { get; set; } = [];
    public List<string> Blacklist { get; set; } = [];

    public IReadOnlyList<string> GetGameNames() => NormalizeNames(Games);

    public IReadOnlyList<string> GetBlacklistNames() => NormalizeNames(Blacklist);

    private static IReadOnlyList<string> NormalizeNames(
        IEnumerable<string>? names) =>
        [.. (names ?? [])
            .Select(GameExecutableName.Normalize)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    public override string GetDescription()
    {
        var games = GetGameNames();
        var blacklist = GetBlacklistNames();

        if (games.Count == 0 && blacklist.Count == 0)
        {
            return Strings.Rule_Game_Desc_Any;
        }

        if (blacklist.Count == 0)
        {
            return string.Format(
                Strings.Rule_Game_Desc,
                string.Join(", ", games));
        }

        if (games.Count == 0)
        {
            return string.Format(
                Strings.Rule_Game_Desc_Blacklist,
                string.Join(", ", blacklist));
        }

        return string.Format(
            Strings.Rule_Game_Desc_WhiteAndBlacklist,
            string.Join(", ", games),
            string.Join(", ", blacklist));
    }
}
