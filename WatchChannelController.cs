using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KickFuckerApi.Models;
using KickFuckerApi.Services;

namespace ChannelWatcherApi.Controllers
{
    /// <summary>
    /// Provides endpoints for managing channel watching tasks.
    /// </summary>
    [ApiController]
    [Route("api/kick-viewers")]
    public class KickViewTasksController : ControllerBase
    {
        private readonly BotManager _botManager;

        public KickViewTasksController(BotManager botManager)
        {
            _botManager = botManager;
        }

        /// <summary>
        /// Creates a new channel watching task.
        /// </summary>
        /// <param name="request">The request containing channel and count.</param>
        /// <returns>The created channel watching task.</returns>
        [HttpPost("task")]
        public async Task<IActionResult> CreateWatchChannelTask([FromBody] CreateChannelWatcherRequest request)
        {
            if (request.Delay <= 0)
            {
                request.Delay = 50;
            }
            var watchChannelTask = await _botManager.StartWatchingChannelAsync(request.Channel, request.Count, request.Delay);

            return CreatedAtAction(nameof(GetTaskStatus), new { id = watchChannelTask.Id }, watchChannelTask);
        }

        /// <summary>
        /// Lists all channel watching tasks.
        /// </summary>
        /// <returns>A list of channel watching tasks.</returns>
        [HttpGet("tasks")]
        public async Task<IActionResult> ListWatchChannelTasks([FromQuery] bool onlyWorking = false)
        {
            var watchChannelTasks = await _botManager.GetAllKickViewTasksAsync();

            if (onlyWorking)
            {
                watchChannelTasks = watchChannelTasks.Where(task => task.CurrentStatus == KickViewTaskStatus.Initializing || task.CurrentStatus == KickViewTaskStatus.Running).ToList();
            }

            return Ok(watchChannelTasks);
        }

        /// <summary>
        /// Retrieves the status of a specific channel watching task.
        /// </summary>
        /// <param name="id">The task ID.</param>
        /// <returns>The channel watching task with the specified ID.</returns>
        [HttpGet("task/{id}")]
        public async Task<IActionResult> GetTaskStatus(int id)
        {
            var watchChannelTask = await _botManager.GetKickViewTaskByIdAsync(id);

            if (watchChannelTask == null)
            {
                return NotFound($"WatchChannelTask with ID '{id}' not found.");
            }

            return Ok(watchChannelTask);
        }

        /// <summary>
        /// Stops a specific channel watching task.
        /// </summary>
        /// <param name="id">The task ID.</param>
        /// <returns>A confirmation message if the task is stopped successfully.</returns>
        [HttpDelete("task/{id}")]
        public async Task<IActionResult> StopWatchChannelTask(int id, [FromQuery] int delay)
        {
            var success = await _botManager.StopKickViewTaskAsync(id, delay);

            if (!success)
            {
                return NotFound($"WatchChannelTask with ID '{id}' not found.");
            }

            return Ok($"Stopped WatchChannelTask with ID '{id}'.");
        }
        /// <summary>
        /// Increases the viewer count for a specific channel watching task.
        /// </summary>
        /// <param name="id">The task ID.</param>
        /// <param name="count">Number of viewers to increase.</param>
        /// <param name="delay">Delay between each increase action.</param>
        /// <returns>A confirmation message if the viewer count is increased successfully.</returns>
        [HttpPut("task/{id}/increase-viewers")]
        public async Task<IActionResult> IncreaseViewers(int id, [FromQuery] int count, [FromQuery] int delay = 50)
        {
            try
            {
                await _botManager.IncreaseViewersAsync(id, count, delay);
                return Ok($"Increased viewer count by {count} for WatchChannelTask with ID '{id}'.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"WatchChannelTask with ID '{id}' not found.");
            }
        }

        /// <summary>
        /// Decreases the viewer count for a specific channel watching task.
        /// </summary>
        /// <param name="id">The task ID.</param>
        /// <param name="count">Number of viewers to decrease.</param>
        /// <param name="delay">Delay between each decrease action.</param>
        /// <returns>A confirmation message if the viewer count is decreased successfully.</returns>
        [HttpPut("task/{id}/decrease-viewers")]
        public async Task<IActionResult> DecreaseViewers(int id, [FromQuery] int count, [FromQuery] int delay = 50)
        {
            try
            {
                await _botManager.DecreaseViewersAsync(id, count, delay);
                return Ok($"Decreased viewer count by {count} for WatchChannelTask with ID '{id}'.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"WatchChannelTask with ID '{id}' not found.");
            }
        }
    }
    
    /// <summary>
    /// Represents a request to create a new channel watcher.
    /// </summary>
    public class CreateChannelWatcherRequest
    {
        public string Channel { get; set; }
        public int Count { get; set; }
        public int Delay { get; set; }
    }
}
