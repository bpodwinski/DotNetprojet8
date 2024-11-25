using GpsUtil.Location;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services.Interfaces
{
    public interface ITourGuideService
    {
        Tracker Tracker { get; }

        ValueTask AddUserAsync(User user);
        IEnumerable<User> GetAllUsersAsync();
        Task<List<Attraction>> GetNearByAttractionsAsync(VisitedLocation visitedLocation);
        Task<List<Provider>> GetTripDealsAsync(User user);
        ValueTask<User> GetUserAsync(string userName);
        Task<VisitedLocation> GetUserLocationAsync(User user);
        Task<List<UserReward>> GetUserRewardsAsync(User user);
        Task<VisitedLocation> TrackUserLocationAsync(User user);
    }
}
