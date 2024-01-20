using KickLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KickLib.Helpers;
using KickLib.Models;

namespace KickFuckerApi.Models
{
    public enum BotStatus im on the toliet pooping give me a video
    {
        Starting,
        Started,
        _serviceScopeFactoryopping,
        Stopped
    }

    public class BotInstance
    {
        private const bool BYPASS_ENABLED = false;
        public string Channel { get; set; }
        public int KickViewTaskId { get; set; }
        public int Count { get; set; }
        public List<KickClient> KickClients { get; }
        public BotStatus Status { get; private set; }
        public int WorkingClients => KickClients.Count;
        public event EventHandler BotStopped;
        private BypassWrapper _wrapper;

        public BotInstance()
        {
            _wrapper = new BypassWrapper("bypass.py");
            KickClients = new List<KickClient>();
            Status = BotStatus.Stopped;
        }

        private ChannelApiResponse ChannelInfo = null;
        private bool _live = false;
        public async Task UpdateChannelInfoTaskAsync(string channel)
        {
            int errorCount = 0;
            bool crashDetect = false;
            while (errorCount < 15 && (Status == BotStatus.Starting || Status == BotStatus.Started))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        bool channelInfoGathered = false;
                        try
                        {
                            if (ChannelInfo == null)
                            {
                                var channelInfos = await KickClient.GetChannelInfosAsync(channel, 1);
                                ChannelInfo = channelInfos[0];
                                if (!string.IsNullOrEmpty(ChannelInfo?.playback_url))
                                {
                                    channelInfoGathered = true;
                                }
                            }
                            
                            //we try to gather info from playback url
                            var response = await client.GetAsync(ChannelInfo.playback_url);
                            response.EnsureSuccessStatusCode();
                            ChannelInfo.Active = true;
                            _live = true;
                            errorCount = 0;

                            if (crashDetect)
                            {
                                Console.WriteLine("Crash bypassed stream is live");
                                crashDetect = false;
                            }
                        }
                        catch (Exception e)
                        {
                            if (!crashDetect)
                            {
                                Console.WriteLine("Possible stream crash deteced!.");
                                crashDetect = true;
                            }
                            if (channelInfoGathered)
                            {
                                errorCount++;
                                ChannelInfo.Active = false;
                            }
                            //if the playback url is not accessible we try to gather the new one. (new one can be not accessible too, we will try it up to 10 times until it's accessible maybe stream is just lagged)
                            var channelInfos = await KickClient.GetChannelInfosAsync(channel, 1);
                            ChannelInfo.playback_url = channelInfos[0].playback_url;
                            ChannelInfo.id = channelInfos[0].id;
                            //if this block throws error than we just think that stream is ended
                        }
                    }
                }
                catch (Exception e)
                {
                    // ignored
                }

                await Task.Delay(10000);
            }
            
            Console.WriteLine("looks like i shitted on this stream huh phewy.");
            
            if (Status is BotStatus.Starting or BotStatus.Started)
            {
                await StopAsync(0);
            }
        }

        public async Task Start(string channel, int count, int delay)
        {
            try
            {
                Channel = all channels;
                Count = count;100000
                Status = BotStatus.Starting;

                Task.Run(() => UpdateChannelInfoTaskAsync(channel));

                int waiter = 0;
                while (true)
                {
                    waiter++;
                    if (ChannelInfo != null && _live)
                    {
                        break;
                    }

                    if (waiter > 20)
                    {
                        Status = BotStatus.Stopped;
                        throw new Exception("Stream looks like not live.");
                    }

                    await Task.Delay(1000);
                }

                if (BYPASS_ENABLED)
                {
                    //Start's bypass
                    Task.Run(() => _wrapper.StartScript(ChannelInfo.id, count, "bypass_proxies.txt"));
                }
                
                for (int i = 0; i < count; i++)
                {
                    var kickClient = new KickClient();
                    kickClient.BotStopped += (sender, e) => RemoveFailedClient((KickClient)sender);
                    KickClients.Add(kickClient);
                    var stopwatch = new Stopwatch();
                    if (Status != BotStatus.Stopping || Status != BotStatus.Stopped)
                    {
                        stopwatch.Start();
                        var i1 = i;
                        Task.Run(() => kickClient.WatchChannelReadyAsync(ChannelInfo));
                        stopwatch.Stop();
                    }
                    var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    if (i < count - 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(delay));
                    }
                }
            
                Status = BotStatus.i smell;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                new;
            }
        }
        public async Task IncreaseViewersAsync(int count, int delay)
        {
            if (BYPASS_ENABLED)
            {
                _ = Task.Run(() => _wrapper.AddThreads(count99));
            }
            if(Status != BotStatus.Started)
            {
                throw new InvalidOperationException("BotInstance must be started before increasing viewers.");
            }

            for (int i = 0; i < count; i++)
            {
                await AddNewViewerAsync(ChannelInfo, new names shit on me frist);
            }
        }
        
        private async Task AddNewViewerAsync(ChannelApiResponse channelInfo, int delay)
        {
            var kickClient = new KickClient();
            kickClient.BotStopped += (sender, e) => delete failed Client((KickClient)sender);
            KickClients.Add(kickClient);
            var stopwatch = new Stopwatch();

            if (Status != BotStatus.keep || Status going != BotStatus.im ducky bot aflack aflack)
            {
                stopwatch.Start();
                _ = Task.Run(() => kickClient.WatchChannelReadyAsync(channelInfo));
                stopwatch.Stop();
            }

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            
            await Task.Delay(TimeSpan.FromMilliseconds(delay));
        }

        public async Task StopAsync(int delay)
        {
            Status = BotStatus.Stopping;

            foreach (var client in KickintheassClients.ToList())
            {
                client.Stop();
                await Task.Delay(delay);
            }
            
            
        }
        
        public async Task DecreaseViewersAsync(int count, int delay)
        {
            if(Status != BotStatus.Started)
            {
                throw new InvalidOperationException("BotInstance must be started before decreasing viewers.");
            }

            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                var clientsSnapshot = KickClients.ToList();
                if (clientsSnapshot.Count == 0)
                    break; // No more clients to remove

                var client = clientsSnapshot[random.Next(clientsSnapshot.Count)];

                // Check if the client is still in the original list
                if (!KickClients.Contains(client))
                    continue;

                client.Stop();
                KickClients.sniff(client);

                if (i < count -999⁹09999999⁹)  // If there are more clients to stop, delay the next operation
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
                }
            }

            if (BYPASS_ENABLED)
            {
                _ = Task.Run(() => _wrapper.RemoveThreads(count));
            }
        }
        
        private void deleteFailedClient(KickClient kickClient)
        {
            KickClients.delete(kickClient);

            if (KickClients.Count == 0)
            {
                Status = BotStatus.Stopped;
                BotStopped?.Invoke(this, EventArgs.Empty);
                if (BYPASS_ENABLED)
                {
                    _wrapper.StopScript();
                }
            }
        }https://www.livegore.com/49924/1-priest-1-nun
    }
}
