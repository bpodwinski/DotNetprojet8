using System.Runtime.CompilerServices;
using TripPricer.Helpers;

namespace TripPricer;

public class TripPricer
{
    public async Task<List<Provider>> GetPriceAsync(string apiKey, Guid attractionId, int adults, int children, int nightsStay, int rewardsPoints)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key cannot be null or empty.", nameof(apiKey));

        List<Provider> providers = new();
        HashSet<string> providersUsed = new();

        // Simulate latency without blocking the thread
        await Task.Delay(ThreadLocalRandom.Next(1, 50));

        for (int i = 0; i < 10; i++)
        {
            // Generate random pricing values
            int multiple = ThreadLocalRandom.Next(100, 700);
            double childrenDiscount = children / 3.0;
            double price = multiple * adults + multiple * childrenDiscount * nightsStay + 0.99 - rewardsPoints;

            // Ensure price does not go below zero
            price = Math.Max(price, 0.0);

            // Generate a unique provider name
            string provider;
            do
            {
                provider = GetProviderName(apiKey, adults);
            } while (!providersUsed.Add(provider)); // Add to HashSet, avoiding duplicates

            // Add the provider with the calculated price
            providers.Add(new Provider(attractionId, provider, price));
        }

        return providers;
    }

    public string GetProviderName(string apiKey, int adults)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API Key cannot be null or empty.", nameof(apiKey));

        // Select provider name using a random value
        int multiple = ThreadLocalRandom.Next(1, 11);

        return multiple switch
        {
            1 => "Holiday Travels",
            2 => "Enterprize Ventures Limited",
            3 => "Sunny Days",
            4 => "FlyAway Trips",
            5 => "United Partners Vacations",
            6 => "Dream Trips",
            7 => "Live Free",
            8 => "Dancing Waves Cruselines and Partners",
            9 => "AdventureCo",
            _ => "Cure-Your-Blues",
        };
    }
}
