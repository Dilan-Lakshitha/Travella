namespace Travella.Application.Interfaces
{
    public interface IReviewRepository
    {
        Task<int> AddAsync(int itineraryId, int reviewerId, string reviewerRole, string comments, string status);
    }
}
