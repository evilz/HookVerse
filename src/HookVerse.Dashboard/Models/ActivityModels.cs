namespace HookVerse.Dashboard.Models;

/// <summary>
/// Activity item for the activity log
/// </summary>
public class ActivityItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ActivityUser User { get; set; } = new();
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public ActivityStatus? Status { get; set; }
    public string? Assignee { get; set; }
    public List<ActivityTag>? Tags { get; set; }
    public string? Comment { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

/// <summary>
/// User information for activity item
/// </summary>
public class ActivityUser
{
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

/// <summary>
/// Status information for activity item
/// </summary>
public class ActivityStatus
{
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Tag information for activity item
/// </summary>
public class ActivityTag
{
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
