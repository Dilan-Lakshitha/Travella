namespace Travella.Domain.Entities
{
    public class Attraction
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Country { get; set; } = null!;

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }
    }
}