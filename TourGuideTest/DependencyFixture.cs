using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Services;
using TourGuide.LibrairiesWrappers;
using Microsoft.Extensions.Logging;
using TourGuide.Utilities;

namespace TourGuideTest
{
    public class DependencyFixture
    {
        public DependencyFixture()
        {
            Task.Run(() => InitializeAsync()).GetAwaiter().GetResult();
        }

        public async Task CleanupAsync()
        {
            await InitializeAsync();
        }

        public async Task InitializeAsync(int internalUserNumber = 100)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var tourGuideLogger = loggerFactory.CreateLogger<TourGuideService>();

            InternalTestHelper.SetInternalUserNumber(internalUserNumber);

            RewardCentral = new RewardCentralWrapper();
            GpsUtil = new GpsUtilWrapper();
            RewardsService = new RewardsService(GpsUtil, RewardCentral);
            TourGuideService = new TourGuideService(tourGuideLogger, GpsUtil, RewardsService, loggerFactory);

            await Task.CompletedTask; // Placeholder si une logique asynchrone est nécessaire à l'avenir
        }

        public IRewardCentral RewardCentral { get; set; }
        public IGpsUtil GpsUtil { get; set; }
        public IRewardsService RewardsService { get; set; }
        public ITourGuideService TourGuideService { get; set; }
    }
}
