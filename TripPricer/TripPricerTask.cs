namespace TripPricer;

public class TripPricerTask
{
    private readonly Guid _attractionId;
    private readonly string _apiKey;
    private readonly int _adults;
    private readonly int _children;
    private readonly int _nightsStay;
    private readonly int _rewardsPoints;

    public TripPricerTask(string apiKey, Guid attractionId, int adults, int children, int nightsStay, int rewardsPoints = 5)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key cannot be null or empty.", nameof(apiKey));

        _apiKey = apiKey;
        _attractionId = attractionId;
        _adults = adults;
        _children = children;
        _nightsStay = nightsStay;
        _rewardsPoints = rewardsPoints;
    }

    public async Task<List<Provider>> ExecuteAsync()
    {
        var tripPricer = new TripPricer();
        return await tripPricer.GetPriceAsync(_apiKey, _attractionId, _adults, _children, _nightsStay, _rewardsPoints);
    }
}
