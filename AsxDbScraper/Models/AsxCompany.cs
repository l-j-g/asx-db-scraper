namespace AsxDbScraper.Models;

public class AsxCompany
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
} 