using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using KickFuckerApi.Data;
using KickFuckerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KickFuckerApi.Services
{
    public class BotManager
    {
        private readonly ConcurrentDictionary<int, BotInstance> _botInstances = new();
        private int _nextKey = 1;
        
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public BotManager(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _ = SyncKickViewTasks(_cancellationTokenSource.Token);
        }

        public async Task<KickViewTask> StartWatchingChannelAsync(string channel, int count, int delay)
        {
            var key = _nextKey++;
            var botInstance = new BotInstance();
            _botInstances.TryAdd(key, botInstance);
            botInstance.BotStopped += BotInstanceOnBotStopped;
            _ = Task.Run(() => botInstance.Start(channel, count, delay));

            var kickViewTask = new KickViewTask
            {
                CreatedAt = DateTime.UtcNow,
                TargetChannel = channel,
                TargetViewCount = count,
                CurrentStatus = KickViewTaskStatus.Initializing,
            };

            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

            databaseContext.KickViewTasks.Add(kickViewTask);
            await databaseContext.SaveChangesAsync();
            botInstance.KickViewTaskId = kickViewTask.Id;
            return kickViewTask;
        }
        private void BotInstanceOnBotStopped(object? sender, EventArgs e)
        {
            if (sender is BotInstance stoppedBot)
            {
                // Find the key associated with the stopped bot instance
                int? keyToRemove = null;
                foreach (var keyValuePair in _botInstances)
                {
                    if (keyValuePair.Value == stoppedBot)
                    {
                        keyToRemove = keyValuePair.Key;
                        break;
                    }
                }

                if (keyToRemove != null)
                {
                    // Remove the stopped bot instance from the dictionary
                    _botInstances.TryRemove(keyToRemove.Value, out _);

                    // Unsubscribe from the BotStopped event
                    stoppedBot.BotStopped -= BotInstanceOnBotStopped;
                }

                // Perform any additional cleanup or logging tasks here
                Console.WriteLine($"Bot instance with key {keyToRemove} has stopped watching channel.");
            }
        }

        public BotInstance GetBotInstance(int key)
        {
            _botInstances.TryGetValue(key, out var botInstance);
            return botInstance;
        }
    
        public ConcurrentDictionary<int, BotInstance> GetAllBotInstances()
        {
            return _botInstances;
        }

        public void StopWatchingChannel(int key, int delay)
        {
            if (_botInstances.TryGetValue(key, out var botInstance))
            {
                _ = botInstance.StopAsync(delay);
                _botInstances.TryRemove(key, out _);
            }
        }
        public async Task<KickViewTask> GetKickViewTaskByIdAsync(int id)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

            return await databaseContext.KickViewTasks.FindAsync(id);
        }

        public async Task<List<KickViewTask>> GetAllKickViewTasksAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

            // Retrieve and return the list of KickViewTasks
            return await databaseContext.KickViewTasks.ToListAsync();
        }
        
        public async Task<bool> StopKickViewTaskAsync(int taskId, int delay)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

            var kickViewTask = await databaseContext.KickViewTasks.FindAsync(taskId);

            if (kickViewTask != null)
            {
                kickViewTask.CurrentStatus = KickViewTaskStatus.Stopping;
                await databaseContext.SaveChangesAsync();
                // Find the associated BotInstance and stop it
                foreach (var botInstance in _botInstances.Values)
                {
                    if (botInstance.KickViewTaskId == taskId)
                    {
                        _ = botInstance.StopAsync(delay);
                        break;
                    }
                }

                kickViewTask.CurrentStatus = KickViewTaskStatus.Completed;
                await databaseContext.SaveChangesAsync();
                return true;
            }

            return false;
        }


        private KickViewTaskStatus MapBotStatusToKickViewTaskStatus(BotStatus botStatus)
        {
            return botStatus switch
            {
                BotStatus.Starting => KickViewTaskStatus.Initializing,
                BotStatus.Started => KickViewTaskStatus.Running,
                BotStatus.Stopping => KickViewTaskStatus.Stopping,
                BotStatus.Stopped => KickViewTaskStatus.Completed,
                _ => throw new ArgumentOutOfRangeException(nameof(botStatus), botStatus, "Invalid bot status value."),
            };
        }

        private async Task SyncKickViewTasks(CancellationToken cancellationToken)
        {
            bool firstTime = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

                if (firstTime)
                {
                    foreach (var t in databaseContext.KickViewTasks)
                    {
                        t.CurrentStatus = KickViewTaskStatus.Completed;
                    }

                    firstTime = false;
                }
                
                foreach (var keyValuePair in _botInstances)
                {
                    int key = keyValuePair.Key;
                    BotInstance botInstance = keyValuePair.Value;

                    // Find the KickViewTask using the BotInstance.KickViewTaskId property
                    var kickViewTask = await databaseContext.KickViewTasks.FindAsync(botInstance.KickViewTaskId);
                    if (kickViewTask != null)
                    {
                        // Update the KickViewTask with the current state of the BotInstance
                        kickViewTask.ActiveViewers = botInstance.WorkingClients;
                        kickViewTask.CurrentStatus = MapBotStatusToKickViewTaskStatus(botInstance.Status);
                    }
                }

                foreach (var task in databaseContext.KickViewTasks.Where(t => t.CurrentStatus == KickViewTaskStatus.Running))
                {
                    bool botInstanceExists = false;

                    foreach (var keyValuePair in _botInstances)
                    {
                        int key = keyValuePair.Key;
                        BotInstance botInstance = keyValuePair.Value;

                        if (botInstance.KickViewTaskId == task.Id)
                        {
                            botInstanceExists = true;
                            break;
                        }
                    }

                    if (!botInstanceExists)
                    {
                        task.CurrentStatus = KickViewTaskStatus.Completed;
                    }
                }

                
                // Save changes to the database
                await databaseContext.SaveChangesAsync();
                // Wait for a given interval before syncing again
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public async Task IncreaseViewersAsync(int taskId, int count, int delay)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

            var kickViewTask = await databaseContext.KickViewTasks.FindAsync(taskId);

            if (kickViewTask != null)
            {
                foreach (var botInstance in _botInstances.Values)
                {
                    if (botInstance.KickViewTaskId == taskId)
                    {
                        _ = Task.Run(() => botInstance.IncreaseViewersAsync(count, delay));
                        break;
                    }
                }
            }
            else
            {
                throw new KeyNotFoundException("No KickViewTask found with the given ID");
            }
        }

        public async Task DecreaseViewersAsync(int taskId, int count, int delay)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<KickFuckerDbContext>();

            var kickViewTask = await databaseContext.KickViewTasks.FindAsync(taskId);

            if (kickViewTask != null)
            {
                foreach (var botInstance in _botInstances.Values)
                {
                    if (botInstance.KickViewTaskId == taskId)
                    {
                        _ = botInstance.DecreaseViewersAsync(count, delay);
                        break;
                    }
                }
            }
            else
            {
                throw new KeyNotFoundException("No KickViewTask found with the given ID");
            }
        }

    }
}
