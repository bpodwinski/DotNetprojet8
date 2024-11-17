namespace RewardCentral;

public class RewardCentral
{
    private readonly bool _simulateLatency;

    public RewardCentral(bool simulateLatency = true)
    {
        _simulateLatency = simulateLatency;
    }

    public async Task<int> GetAttractionRewardPointsAsync(Guid attractionId, Guid userId)
    {
        if (_simulateLatency)
        {
            int randomDelay = Random.Shared.Next(1, 1000);
            await Task.Delay(randomDelay);
        }

        int randomInt = Random.Shared.Next(1, 1000);
        return randomInt;
    }
}
