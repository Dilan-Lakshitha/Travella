using System;

namespace Travella.Application.DTOs
{
    public class ItineraryPricingDetailDto
    {
        public int Id { get; set; }

        public int ItineraryId { get; set; }

        public int CreatedBy { get; set; }

        public decimal DriverCost { get; set; }

        public decimal GuideCost { get; set; }

        public decimal VehicleCost { get; set; }

        public decimal MileageRate { get; set; }

        public decimal TotalKm { get; set; }

        public decimal AccommodationCost { get; set; }

        public string MealPlan { get; set; } = "BB";

        public decimal ProfitMargin { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }

    public static class PricingCalculator
    {
        public static decimal CalculateTotalAmount(
            decimal driverCost,
            decimal guideCost,
            decimal vehicleCost,
            decimal mileageRate,
            decimal totalKm,
            decimal accommodationCost,
            decimal profitMarginPercent)
        {
            var travelCost = mileageRate * totalKm;
            var baseCost = driverCost + guideCost + vehicleCost + accommodationCost + travelCost;
            return baseCost + (baseCost * profitMarginPercent / 100m);
        }

        public static void ApplyCalculatedTotal(ItineraryPricingInputDto dto)
        {
            dto.TotalAmount = CalculateTotalAmount(
                dto.DriverCost,
                dto.GuideCost,
                dto.VehicleCost,
                dto.MileageRate,
                dto.TotalKm,
                dto.AccommodationCost,
                dto.ProfitMargin);
        }
    }
}
