namespace Travella.Domain.Entities
{
    public class ItineraryAccommodation
    {
        public int Id { get; set; }

        public int ItineraryDayId { get; set; }

        public int AccommodationId { get; set; }

        public int MealPlanId { get; set; }

        public ItineraryDay? ItineraryDay { get; set; }

        public Accommodation? Accommodation { get; set; }

        public MealPlan? MealPlan { get; set; }
    }
}