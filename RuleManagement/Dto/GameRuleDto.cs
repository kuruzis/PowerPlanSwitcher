namespace RuleManagement.Dto;

public class GameRuleDto : RuleDto, IRuleDto
{
    public List<string> Games { get; set; } = [];

    public IReadOnlyList<string> GetGameNames() =>
        [.. (Games ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    public override string GetDescription()
    {
        var games = GetGameNames();

        return games.Count == 0
            ? Strings.Rule_Game_Desc_Any
            : string.Format(
                Strings.Rule_Game_Desc,
                string.Join(", ", games));
    }
}
