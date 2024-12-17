using GpsUtil.Location;
using TourGuide.Users;
using TripPricer;

namespace TourGuideTest
{
    public class TourGuideServiceTour : IClassFixture<DependencyFixture>
    {
        private readonly DependencyFixture _fixture;

        public TourGuideServiceTour(DependencyFixture fixture)
        {
            _fixture = fixture;
        }

        public void Dispose()
        {
            _fixture.Cleanup();
        }

        [Fact]
        public async Task GetUserLocation()
        {
            _fixture.Initialize(0);
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
            var visitedLocation = await _fixture.TourGuideService.TrackUserLocationAsync(user);
            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.Equal(user.UserId, visitedLocation.UserId);
        }

        [Fact]
        public async Task AddUser()
        {
            _fixture.Initialize(0);
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
            var user2 = new User(Guid.NewGuid(), "jon2", "000", "jon2@tourGuide.com");

            await _fixture.TourGuideService.AddUserAsync(user);
            await _fixture.TourGuideService.AddUserAsync(user2);

            var retrievedUser = await _fixture.TourGuideService.GetUserAsync(user.UserName);
            var retrievedUser2 = await _fixture.TourGuideService.GetUserAsync(user2.UserName);

            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.Equal(user, retrievedUser);
            Assert.Equal(user2, retrievedUser2);
        }

        [Fact]
        public async Task GetAllUsers()
        {
            _fixture.Initialize(0);
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
            var user2 = new User(Guid.NewGuid(), "jon2", "000", "jon2@tourGuide.com");

            await _fixture.TourGuideService.AddUserAsync(user);
            await _fixture.TourGuideService.AddUserAsync(user2);

            var allUsers = _fixture.TourGuideService.GetAllUsersAsync();

            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.Contains(user, allUsers);
            Assert.Contains(user2, allUsers);
        }

        [Fact]
        public async Task TrackUser()
        {
            _fixture.Initialize();
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
            var visitedLocation = await _fixture.TourGuideService.TrackUserLocationAsync(user);

            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.Equal(user.UserId, visitedLocation.UserId);
        }

        [Fact]
        public async Task GetNearbyAttractions()
        {
            _fixture.Initialize(0);
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
            var visitedLocation = await _fixture.TourGuideService.TrackUserLocationAsync(user);

            List<Attraction> attractions = await _fixture.TourGuideService.GetNearByAttractionsAsync(visitedLocation);

            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.Equal(5, attractions.Count);
        }

        [Fact]
        public async Task GetTripDeals()
        {
            _fixture.Initialize(0);
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
            List<Provider> providers = await _fixture.TourGuideService.GetTripDealsAsync(user);

            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.Equal(5, providers.Count);
        }
    }
}
