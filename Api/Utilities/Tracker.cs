using System.Diagnostics;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Utilities;

public class Tracker
{
    private readonly ILogger<Tracker> _logger;
    private static readonly TimeSpan TrackingPollingInterval = TimeSpan.FromMinutes(5);
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly ITourGuideService _tourGuideService;

    public Tracker(ITourGuideService tourGuideService, ILogger<Tracker> logger)
    {
        _tourGuideService = tourGuideService ?? throw new ArgumentNullException(nameof(tourGuideService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Lance le traqueur
        Task.Run(() => RunAsync(), _cancellationTokenSource.Token);
    }

    // Assure l'arrêt du suivi
    public void StopTracking()
    {
        _cancellationTokenSource.Cancel();
    }

    private async Task RunAsync()
    {
        var stopwatch = new Stopwatch();

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // Récupère tous les utilisateurs de manière asynchrone
                var users = await _tourGuideService.GetAllUsersAsync();
                _logger.LogDebug($"Begin Tracker. Tracking {users.Count} users.");

                stopwatch.Start();

                // Suit la localisation des utilisateurs de manière asynchrone
                var trackTasks = users.Select(user => _tourGuideService.TrackUserLocationAsync(user));
                await Task.WhenAll(trackTasks);

                stopwatch.Stop();

                _logger.LogDebug($"Tracker Time Elapsed: {stopwatch.ElapsedMilliseconds / 1000.0} seconds.");

                stopwatch.Reset();

                _logger.LogDebug("Tracker sleeping");
                await Task.Delay(TrackingPollingInterval, _cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                break; // Sort de la boucle si l'annulation est demandée
            }
            catch (Exception ex)
            {
                _logger.LogError($"Tracker encountered an error: {ex.Message}");
            }
        }

        _logger.LogDebug("Tracker stopping");
    }
}
