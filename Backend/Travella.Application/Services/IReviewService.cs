namespace Travella.Application.Services
{
    public interface IReviewService
    {
        Task<int> AddReviewAsync(int itineraryId, int reviewerId, string reviewerRole, int companyId, string comments, string status);
    }
}
