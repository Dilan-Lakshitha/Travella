using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewService(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public Task<int> AddReviewAsync(int itineraryId, int reviewerId, string reviewerRole, string comments, string status)
            => _reviewRepository.AddAsync(itineraryId, reviewerId, reviewerRole, comments, status);
    }
}
