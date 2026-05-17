namespace Travella.Domain.Entities
{
    public class Accommodation
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Location { get; set; } = null!;

        public decimal PricePerNight { get; set; }
    }
}