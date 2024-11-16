using GpsUtil.Location;
using TourGuide.Users;

namespace TourGuideTest
{
    public class RewardServiceTest : IClassFixture<DependencyFixture>
    {
        private readonly DependencyFixture _fixture;

        public RewardServiceTest(DependencyFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserGetRewardsAsync()
        {
            await _fixture.InitializeAsync(0);
            var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");

            var attraction = (await _fixture.GpsUtil.GetAttractionsAsync()).First();
            user.AddToVisitedLocations(new VisitedLocation(user.UserId, attraction, DateTime.Now));

            await _fixture.TourGuideService.TrackUserLocationAsync(user);

            var userRewards = user.UserRewards;
            _fixture.TourGuideService.Tracker.StopTracking();

            Assert.True(userRewards.Count == 1);
        }

        [Fact]
        public async Task IsWithinAttractionProximityAsync()
        {
            var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
            var attraction = attractions.First();

            Assert.True(_fixture.RewardsService.IsWithinAttractionProximity(attraction, attraction));
        }

        [Fact]
        public async Task NearAllAttractionsAsync()
        {
            await _fixture.InitializeAsync(1);
            _fixture.RewardsService.SetProximityBuffer(int.MaxValue);

            var users = await _fixture.TourGuideService.GetAllUsersAsync();
            var user = users.First();

            await _fixture.RewardsService.CalculateRewardsAsync(user);

            var userRewards = await _fixture.TourGuideService.GetUserRewardsAsync(user);
            _fixture.TourGuideService.Tracker.StopTracking();

            var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
            Assert.Equal(attractions.Count, userRewards.Count);
        }
    }
}
