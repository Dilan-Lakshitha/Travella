namespace Travella.Application.DTOs
{
    public class AddItineraryDayDto
    {
        public int ItineraryId { get; set; }

        public int DayNumber { get; set; }

        public string OvernightLocation { get; set; } = null!;
    }
}
