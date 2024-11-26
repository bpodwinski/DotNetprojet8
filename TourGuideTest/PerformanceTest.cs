using GpsUtil.Location;
using System.Diagnostics;
using Xunit.Abstractions;

namespace TourGuideTest
{
    public class PerformanceTest : IClassFixture<DependencyFixture>
    {
        private readonly DependencyFixture _fixture;
        private readonly ITestOutputHelper _output;

        public PerformanceTest(DependencyFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        [InlineData(50000)]
        [InlineData(100000)]
        public async Task HighVolumeTrackLocationAsync(int userCount)
        {
            await _fixture.InitializeAsync(userCount);

            var allUsers = _fixture.TourGuideService.GetAllUsersAsync();

            Stopwatch stopWatch = Stopwatch.StartNew();

            var trackTasks = allUsers.Select(user => _fixture.TourGuideService.TrackUserLocationAsync(user));
            await Task.WhenAll(trackTasks);

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeTrackLocation ({userCount} users): Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds, $"Test failed for {userCount} users.");
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public async Task HighVolumeGetRewardsAsync(int userCount)
        {
            await _fixture.InitializeAsync(userCount);

            Stopwatch stopWatch = Stopwatch.StartNew();

            var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
            var attraction = attractions.First();

            var allUsers = _fixture.TourGuideService.GetAllUsersAsync();
            Parallel.ForEach(allUsers, user =>
            {
                user.AddToVisitedLocations(new VisitedLocation(user.UserId, attraction, DateTime.Now));
            });

            var rewardTasks = allUsers.Select(user => _fixture.RewardsService.CalculateRewardsAsync(user));
            await Task.WhenAll(rewardTasks);

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeGetRewards ({userCount} users): Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.All(allUsers, user => Assert.True(user.UserRewards.Count > 0, $"User {user.UserId} has no rewards."));
            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds, $"Test failed for {userCount} users.");
        }
    }
}
