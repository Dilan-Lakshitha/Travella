namespace Travella.Application.DTOs
{
    public class SaveGoogleAttractionDto
    {
        public string Name { get; set; } = string.Empty;
        public string PlaceId { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class SaveGoogleAttractionResponseDto
    {
        public int Id { get; set; }
        public bool AlreadyExists { get; set; }
    }
}
