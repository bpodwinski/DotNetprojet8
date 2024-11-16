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

        [Fact(Skip = "Delete Skip when you want to pass the test")]
        public async Task HighVolumeTrackLocationAsync()
        {
            // Augmentez le nombre d'utilisateurs ici pour tester les performances
            await _fixture.InitializeAsync(1000);

            var allUsers = await _fixture.TourGuideService.GetAllUsersAsync();

            Stopwatch stopWatch = Stopwatch.StartNew();

            var trackTasks = allUsers.Select(user => _fixture.TourGuideService.TrackUserLocationAsync(user));
            await Task.WhenAll(trackTasks);

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeTrackLocation: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }

        [Fact(Skip = "Delete Skip when you want to pass the test")]
        public async Task HighVolumeGetRewardsAsync()
        {
            // Augmentez le nombre d'utilisateurs ici pour tester les performances
            await _fixture.InitializeAsync(10000);

            Stopwatch stopWatch = Stopwatch.StartNew();

            var attractions = await _fixture.GpsUtil.GetAttractionsAsync();
            var attraction = attractions.First();

            var allUsers = await _fixture.TourGuideService.GetAllUsersAsync();
            foreach (var user in allUsers)
            {
                user.AddToVisitedLocations(new VisitedLocation(user.UserId, attraction, DateTime.Now));
            }

            var rewardTasks = allUsers.Select(user => _fixture.RewardsService.CalculateRewardsAsync(user));
            await Task.WhenAll(rewardTasks);

            stopWatch.Stop();
            _fixture.TourGuideService.Tracker.StopTracking();

            _output.WriteLine($"highVolumeGetRewards: Time Elapsed: {stopWatch.Elapsed.TotalSeconds} seconds.");

            Assert.All(allUsers, user => Assert.True(user.UserRewards.Count > 0));
            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }
    }
}
