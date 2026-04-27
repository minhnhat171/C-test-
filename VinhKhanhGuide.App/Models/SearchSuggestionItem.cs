namespace VinhKhanhGuide.App.Models;

public class SearchSuggestionItem
{
    public string Text { get; set; } = string.Empty;
    public string SupportingText { get; set; } = string.Empty;
    public bool IsRecent { get; set; }

    public string LeadingIconSource => IsRecent ? "refresh_icon.svg" : "search_icon.svg";
}
