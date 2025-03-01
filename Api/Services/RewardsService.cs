﻿using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services
{
    public class RewardsService : IRewardsService
    {
        private const double StatuteMilesPerNauticalMile = 1.15077945;
        private readonly int _defaultProximityBuffer = 10;
        private int _proximityBuffer;
        private readonly int _attractionProximityRange = 200;
        private readonly IGpsUtil _gpsUtil;
        private readonly IRewardCentral _rewardsCentral;
        private static int count = 0;

        public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
        {
            _gpsUtil = gpsUtil ?? throw new ArgumentNullException(nameof(gpsUtil));
            _rewardsCentral = rewardCentral ?? throw new ArgumentNullException(nameof(rewardCentral));
            _proximityBuffer = _defaultProximityBuffer;
        }

        public void SetProximityBuffer(int proximityBuffer)
        {
            if (proximityBuffer <= 0)
                throw new ArgumentException("Proximity buffer must be greater than 0.", nameof(proximityBuffer));

            _proximityBuffer = proximityBuffer;
        }

        public void SetDefaultProximityBuffer()
        {
            _proximityBuffer = _defaultProximityBuffer;
        }

        public async Task CalculateRewardsAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            count++;

            // Capture les noms des attractions déjà récompensées dans un HashSet
            var rewardedAttractions = new HashSet<string>(
                user.UserRewards.Select(r => r.Attraction.AttractionName));

            // Filtre et capture les localisations visitées de l'user
            var userLocationsSnapshot = user.VisitedLocations
                .Where(location => location != null)
                .ToList();

            var attractionsSnapshot = await _gpsUtil.GetAttractionsAsync();

            // Traite les localisations
            await Parallel.ForEachAsync(userLocationsSnapshot, async (visitedLocation, _) =>
            {
                // Filtre attractions :
                // - Proches de la localisation visitée
                // - Non récompensées
                var nearbyAttractions = attractionsSnapshot
                    .Where(attraction =>
                        !rewardedAttractions.Contains(attraction.AttractionName) &&
                        NearAttraction(visitedLocation, attraction));

                // Traite les attractions proches pour ajouter des récompenses
                foreach (var attraction in nearbyAttractions)
                {
                    var rewardPoints = await GetRewardPointsAsync(attraction, user);

                    // Lock pour éviter les accès concurrents à la liste des récompenses
                    lock (user.UserRewards)
                    {
                        // Nouvelle récompense
                        user.AddUserReward(new UserReward(visitedLocation, attraction, rewardPoints));

                        // Ajoute l'attraction au HashSet pour éviter de traiter plusieurs fois la même attraction
                        rewardedAttractions.Add(attraction.AttractionName);
                    }
                }
            });
        }

        public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
        {
            ArgumentNullException.ThrowIfNull(attraction);
            ArgumentNullException.ThrowIfNull(location);

            return GetDistance(attraction, location) <= _attractionProximityRange;
        }

        private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
        {
            ArgumentNullException.ThrowIfNull(visitedLocation);
            ArgumentNullException.ThrowIfNull(attraction);

            return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
        }

        public async Task<int> GetRewardPointsAsync(Attraction attraction, User user)
        {
            ArgumentNullException.ThrowIfNull(attraction);
            ArgumentNullException.ThrowIfNull(user);

            return await _rewardsCentral.GetAttractionRewardPointsAsync(attraction.AttractionId, user.UserId);
        }

        public double GetDistance(Locations loc1, Locations loc2)
        {
            if (loc1 == null || loc2 == null)
                throw new ArgumentNullException(loc1 == null ? nameof(loc1) : nameof(loc2));

            double lat1 = Math.PI * loc1.Latitude / 180.0;
            double lon1 = Math.PI * loc1.Longitude / 180.0;
            double lat2 = Math.PI * loc2.Latitude / 180.0;
            double lon2 = Math.PI * loc2.Longitude / 180.0;

            double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                    + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

            double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
            return StatuteMilesPerNauticalMile * nauticalMiles;
        }
    }
}
