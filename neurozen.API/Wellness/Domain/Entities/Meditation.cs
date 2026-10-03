namespace neurozen.API.Wellness.Domain.Entities;

public class Meditation
{
    private Meditation()
    {
        Title = string.Empty;
        Description = string.Empty;
        ImageUrl = string.Empty;
        AudioUrl = string.Empty;
    }

    public int Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public int DurationMinutes { get; private set; }
    public string ImageUrl { get; private set; }
    public string AudioUrl { get; private set; }
}