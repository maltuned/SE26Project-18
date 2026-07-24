namespace SE26Project_18.Backend.Models.Dtos;

public class GameRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public long[] TagsId { get; set; } = [];
}