﻿using GpsUtil.Location;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, User> _internalUserMap = new();
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
        ArgumentNullException.ThrowIfNull(user);

        return user.UserRewards;
    }

    public async Task<VisitedLocation> GetUserLocationAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.VisitedLocations.Count != 0 ? user.GetLastVisitedLocation() : await TrackUserLocationAsync(user);
    }
    public ValueTask<User> GetUserAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName)) throw new ArgumentException("UserName cannot be null or empty.");

        _internalUserMap.TryGetValue(userName, out var user);
        return new ValueTask<User>(user);
    }

    public IEnumerable<User> GetAllUsersAsync() => _internalUserMap.Values;

    public async ValueTask AddUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _internalUserMap.TryAdd(user.UserName, user);

        await Task.CompletedTask;
    }

    public async Task<List<Provider>> GetTripDealsAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        int cumulativeRewardPoints = user.UserRewards.Sum(i => i.RewardPoints);

        var providers = await _tripPricer.GetPriceAsync(
            TripPricerApiKey,
            user.UserId,
            user.UserPreferences.NumberOfAdults,
            user.UserPreferences.NumberOfChildren,
            user.UserPreferences.TripDuration,
            cumulativeRewardPoints
        );

        user.TripDeals = providers;
        return providers;
    }

    public async Task<VisitedLocation> TrackUserLocationAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var locationTask = _gpsUtil.GetUserLocationAsync(user.UserId);
        var calculateRewardsTask = Task.Run(() => _rewardsService.CalculateRewardsAsync(user));

        var visitedLocation = await locationTask;
        user.AddToVisitedLocations(visitedLocation);

        await calculateRewardsTask;
        return visitedLocation;
    }

    public async Task<List<Attraction>> GetNearByAttractionsAsync(VisitedLocation visitedLocation)
    {
        ArgumentNullException.ThrowIfNull(visitedLocation);

        var attractions = await _gpsUtil.GetAttractionsAsync();

        return attractions
            .AsParallel()
            .Select(attraction => (Attraction: attraction, Distance: _rewardsService.GetDistance(attraction, visitedLocation.Location)))
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
        Parallel.For(0, InternalTestHelper.GetInternalUserNumber(), i =>
        {
            var userName = $"internalUser{i}";
            var user = new User(Guid.NewGuid(), userName, "000", $"{userName}@tourGuide.com");

            Parallel.For(0, 3, j =>
            {
                var visitedLocation = new VisitedLocation(user.UserId, new Locations(GenerateRandomLatitude(), GenerateRandomLongitude()), GetRandomTime());
                user.AddToVisitedLocations(visitedLocation);
            });

            _internalUserMap.TryAdd(userName, user);
        });

        _logger.LogDebug("Created {InternalUserCount} internal test users.", InternalTestHelper.GetInternalUserNumber());
    }

    public static readonly ThreadLocal<Random> ThreadSafeRandom = new(() => new Random());

    private static double GenerateRandomLongitude()
    {
        return Random.Shared.NextDouble() * 360 - 180;
    }

    private static double GenerateRandomLatitude()
    {
        return Random.Shared.NextDouble() * 180 - 90;
    }

    private static DateTime GetRandomTime()
    {
        return DateTime.UtcNow.AddDays(-Random.Shared.Next(30));
    }
}
