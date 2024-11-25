using System.Diagnostics;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Utilities;

public class Tracker
{
    private readonly ILogger<Tracker> _logger;
    private static readonly TimeSpan TrackingPollingInterval = TimeSpan.FromMinutes(5);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ITourGuideService _tourGuideService;

    public Tracker(ITourGuideService tourGuideService, ILogger<Tracker> logger)
    {
        _tourGuideService = tourGuideService ?? throw new ArgumentNullException(nameof(tourGuideService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Start the tracking process in a background task
        Task.Run(() => RunAsync(), _cancellationTokenSource.Token);
    }

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
                // Fetch all users asynchronously
                var users = _tourGuideService.GetAllUsersAsync();
                _logger.LogDebug("Begin Tracker. Tracking {UserCount} users.", users.Count());

                stopwatch.Start();

                // Track user locations concurrently
                var trackTasks = users.Select(user => TrackUserLocationWithLoggingAsync(user));
                await Task.WhenAll(trackTasks);

                stopwatch.Stop();
                _logger.LogDebug("Tracker Time Elapsed: {Stopwatch} seconds.", stopwatch.ElapsedMilliseconds / 1000.0);

                stopwatch.Reset();

                // Delay for the configured polling interval
                _logger.LogDebug("Tracker sleeping");
                await Task.Delay(TrackingPollingInterval, _cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Tracking canceled gracefully.");
                break; // Exit the loop when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError("Tracker encountered an error: {ex}", ex);
            }
        }

        _logger.LogDebug("Tracker stopping");
    }

    private async Task TrackUserLocationWithLoggingAsync(User user)
    {
        try
        {
            await _tourGuideService.TrackUserLocationAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error tracking location for user {user.UserName}: {ex.Message}");
        }
    }
}
