using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;
    private readonly IRewardsService _rewardsService;

    public TourGuideController(
        ITourGuideService tourGuideService,
        IRewardsService rewardsService
    )
    {
        _tourGuideService = tourGuideService;
        _rewardsService = rewardsService;
    }

    [HttpGet("getLocation")]
    public async Task<ActionResult<VisitedLocation>> GetLocationAsync([FromQuery] string userName)
    {
        var user = await GetUserAsync(userName);
        if (user == null)
        {
            return NotFound($"User with username '{userName}' not found.");
        }

        var location = await _tourGuideService.GetUserLocationAsync(user);
        return Ok(location);
    }

    // TODO: Change this method to no longer return a List of Attractions.
    // Instead: Get the closest five tourist attractions to the user - no matter how far away they are.
    // Return a new JSON object that contains:
    // Name of Tourist attraction, 
    // Tourist attractions lat/long, 
    // The user's location lat/long, 
    // The distance in miles between the user's location and each of the attractions.
    // The reward points for visiting each Attraction.
    //    Note: Attraction reward points can be gathered from RewardsCentral
    [HttpGet("getNearbyAttractions")]
    public async Task<ActionResult<List<object>>> GetNearbyAttractionsAsync([FromQuery] string userName)
    {
        // Récupération de l'utilisateur
        var user = await GetUserAsync(userName);
        if (user == null)
        {
            return NotFound($"User with username '{userName}' not found.");
        }

        // Récupération de la localisation de l'utilisateur
        var visitedLocation = await _tourGuideService.GetUserLocationAsync(user);

        // Récupération des attractions
        var attractions = await _tourGuideService.GetNearByAttractionsAsync(visitedLocation);

        // Calcul des 5 attractions les plus proches
        var closestAttractions = attractions
            .Select(attraction => new
            {
                AttractionName = attraction.AttractionName,
                AttractionLatitude = attraction.Latitude,
                AttractionLongitude = attraction.Longitude,
                UserLatitude = visitedLocation.Location.Latitude,
                UserLongitude = visitedLocation.Location.Longitude,
                DistanceInMiles = _rewardsService.GetDistance(
                    new Locations(attraction.Latitude, attraction.Longitude),
                    visitedLocation.Location),
                RewardPoints = _rewardsService.GetRewardPointsAsync(attraction, user)
            })
            .OrderBy(attraction => attraction.DistanceInMiles)
            .Take(5)
            .ToList();

        return Ok(closestAttractions);
    }

    [HttpGet("getRewards")]
    public async Task<ActionResult<List<UserReward>>> GetRewardsAsync([FromQuery] string userName)
    {
        var user = await GetUserAsync(userName);
        if (user == null)
        {
            return NotFound($"User with username '{userName}' not found.");
        }

        var rewards = await _tourGuideService.GetUserRewardsAsync(user);
        return Ok(rewards);
    }

    [HttpGet("getTripDeals")]
    public async Task<ActionResult<List<Provider>>> GetTripDealsAsync([FromQuery] string userName)
    {
        var user = await GetUserAsync(userName);
        if (user == null)
        {
            return NotFound($"User with username '{userName}' not found.");
        }

        var deals = await _tourGuideService.GetTripDealsAsync(user);
        return Ok(deals);
    }

    private async Task<User> GetUserAsync(string userName)
    {
        return await _tourGuideService.GetUserAsync(userName);
    }
}
