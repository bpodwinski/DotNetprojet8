using GpsUtil.Location;
using System.Globalization;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TourGuide.Utilities;
using TripPricer;

namespace TourGuide.Services;

public class TourGuideService : ITourGuideService
{
    private readonly ILogger _logger;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardsService _rewardsService;
    private readonly TripPricer.TripPricer _tripPricer;
    public Tracker Tracker { get; private set; }
    private readonly Dictionary<string, User> _internalUserMap = new();
    private const string TripPricerApiKey = "test-server-api-key";
    private bool _testMode = true;

    public TourGuideService(ILogger<TourGuideService> logger, IGpsUtil gpsUtil, IRewardsService rewardsService, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _tripPricer = new();
        _gpsUtil = gpsUtil;
        _rewardsService = rewardsService;

        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        if (_testMode)
        {
            _logger.LogInformation("TestMode enabled");
            _logger.LogDebug("Initializing users");
            InitializeInternalUsers();
            _logger.LogDebug("Finished initializing users");
        }

        var trackerLogger = loggerFactory.CreateLogger<Tracker>();

        Tracker = new Tracker(this, trackerLogger);
        AddShutDownHook();
    }

    public async Task<List<UserReward>> GetUserRewardsAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        return await Task.FromResult(user.UserRewards);
    }

    public async Task<VisitedLocation> GetUserLocationAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        if (user.VisitedLocations.Any())
        {
            return await Task.FromResult(user.GetLastVisitedLocation());
        }

        return await TrackUserLocationAsync(user);
    }

    public async Task<User> GetUserAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName)) throw new ArgumentException("UserName cannot be null or empty.");
        return await Task.FromResult(_internalUserMap.ContainsKey(userName) ? _internalUserMap[userName] : null);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await Task.FromResult(_internalUserMap.Values.ToList());
    }

    public async Task AddUserAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        if (!_internalUserMap.ContainsKey(user.UserName))
        {
            _internalUserMap.Add(user.UserName, user);
        }

        await Task.CompletedTask;
    }

    public async Task<List<Provider>> GetTripDealsAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);

        var providers = await Task.Run(() =>
            _tripPricer.GetPrice(
                TripPricerApiKey,
                user.UserId,
                user.UserPreferences.NumberOfAdults,
                user.UserPreferences.NumberOfChildren,
                user.UserPreferences.TripDuration,
                cumulativeRewardPoints)
        );

        user.TripDeals = providers;
        return providers;
    }

    public async Task<VisitedLocation> TrackUserLocationAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var visitedLocation = await _gpsUtil.GetUserLocationAsync(user.UserId);
        user.AddToVisitedLocations(visitedLocation);
        await _rewardsService.CalculateRewardsAsync(user);
        return visitedLocation;
    }

    public async Task<List<Attraction>> GetNearByAttractionsAsync(VisitedLocation visitedLocation)
    {
        if (visitedLocation == null) throw new ArgumentNullException(nameof(visitedLocation));

        var attractions = await _gpsUtil.GetAttractionsAsync();

        return attractions
            .Select(attraction => new
            {
                Attraction = attraction,
                Distance = _rewardsService.GetDistance(attraction, visitedLocation.Location)
            })
            .OrderBy(a => a.Distance)
            .Take(5)
            .Select(a => a.Attraction)
            .ToList();
    }

    private void AddShutDownHook()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => Tracker.StopTracking();
    }

    /**********************************************************************************
    * 
    * Methods Below: For Internal Testing
    * 
    **********************************************************************************/

    private void InitializeInternalUsers()
    {
        for (int i = 0; i < InternalTestHelper.GetInternalUserNumber(); i++)
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");
            GenerateUserLocationHistory(user);
            _internalUserMap.Add(userName, user);
        }

        _logger.LogDebug($"Created {InternalTestHelper.GetInternalUserNumber()} internal test users.");
    }

    private void GenerateUserLocationHistory(User user)
    {
        for (int i = 0; i < 3; i++)
        {
            var visitedLocation = new VisitedLocation(user.UserId, new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()), GetRandomTime());
            user.AddToVisitedLocations(visitedLocation);
        }
    }

    private static readonly Random random = new();

    private double GenerateRandomLongitude()
    {
        return random.NextDouble() * (180 - (-180)) + (-180);
    }

    private double GenerateRandomLatitude()
    {
        return random.NextDouble() * (90 - (-90)) + (-90);
    }

    private DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-random.Next(30));
    }
}
