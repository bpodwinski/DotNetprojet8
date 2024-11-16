using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;

namespace TourGuide.LibrairiesWrappers
{
    public class GpsUtilWrapper : IGpsUtil
    {
        public async Task<VisitedLocation> GetUserLocationAsync(Guid userId)
        {
            return await GpsUtil.GpsUtil.GetUserLocationAsync(userId);
        }

        public async Task<List<Attraction>> GetAttractionsAsync()
        {
            return await GpsUtil.GpsUtil.GetAttractionsAsync();
        }
    }
}
