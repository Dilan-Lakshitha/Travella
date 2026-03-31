namespace Travella.Domain.Entities
{
    public class ItineraryStaff
    {
        public int Id { get; set; }

        public int ItineraryId { get; set; }

        public int StaffId { get; set; }

        public Itinerary? Itinerary { get; set; }

        public Staff? Staff { get; set; }
    }
}
