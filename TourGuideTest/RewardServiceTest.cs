using GpsUtil.Location;
using TourGuide.Users;

namespace TourGuideTest;

public class RewardServiceTest : IClassFixture<DependencyFixture>
{
    private readonly DependencyFixture _fixture;

    public RewardServiceTest(DependencyFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UserGetRewards()
    {
        _fixture.Initialize(0);
        var user = new User(Guid.NewGuid(), "jon", "000", "jon@tourGuide.com");
        var attraction = (await _fixture.GpsUtil.GetAttractionsAsync()).First();
        user.AddToVisitedLocations(new VisitedLocation(user.UserId, attraction, DateTime.Now));
        await _fixture.TourGuideService.TrackUserLocationAsync(user);
        var userRewards = user.UserRewards;
        _fixture.TourGuideService.Tracker.StopTracking();

        Assert.True(userRewards.Count == 1);
    }

    [Fact]
    public async Task IsWithinAttractionProximity()
    {
        var attraction = (await _fixture.GpsUtil.GetAttractionsAsync()).First();
        Assert.True(_fixture.RewardsService.IsWithinAttractionProximity(attraction, attraction));
    }

    [Fact]
    public async Task NearAllAttractions()
    {
        _fixture.Initialize(1);
        _fixture.RewardsService.SetProximityBuffer(int.MaxValue);

        var user = _fixture.TourGuideService.GetAllUsersAsync().First();
        await _fixture.RewardsService.CalculateRewardsAsync(user);

        var userRewards = await _fixture.TourGuideService.GetUserRewardsAsync(user);
        _fixture.TourGuideService.Tracker.StopTracking();

        var attractions = await _fixture.GpsUtil.GetAttractionsAsync();

        Assert.Equal(attractions.Count, userRewards.Count);
    }
}
