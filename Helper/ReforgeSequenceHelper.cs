using ExileCore2;
using ExileCore2.PoEMemory;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ExileCore2.Shared;
using System.Windows.Forms;
using System.Threading;

namespace ReforgeHelper.Helper
{
    public class ReforgeSequenceHelper
    {
        private readonly Element _bench;
        private readonly Random _random;

        public ReforgeSequenceHelper(Element bench)
        {
            _bench = bench;
            _random = new Random();
        }

        public RectangleF? GetResultItemRect()
        {
            try
            {
                var resultSlot = _bench?.Children?.ElementAtOrDefault(3)?
                    .Children?.ElementAtOrDefault(1)?
                    .Children?.ElementAtOrDefault(0);

                if (resultSlot?.IsVisible == true)
                {
                    var rect = resultSlot.GetClientRect();
                    DebugWindow.LogMsg($"[ReforgeHelper] Found result item at: {rect.Center.X}, {rect.Center.Y}");
                    return rect;
                }
                else
                {
                    DebugWindow.LogMsg("[ReforgeHelper] Result item slot not found or not visible");
                }
            }
            catch (Exception ex)
            {
                DebugWindow.LogMsg($"[ReforgeHelper] Error getting result item rect: {ex.Message}");
            }
            return null;
        }

        public async Task CtrlClickPosition(Vector2 position, CancellationToken ct)
        {
            try
            {
                await MoveCursorSmoothly(position, ct);
                
                Input.KeyDown(Keys.ControlKey);
                await Task.Delay(50, ct);
                Input.Click(MouseButtons.Left);
                await Task.Delay(50, ct);
                Input.KeyUp(Keys.ControlKey);
            }
            catch (Exception ex)
            {
                DebugWindow.LogMsg($"[ReforgeHelper] Error during ctrl-click: {ex.Message}");
                throw;
            }
        }

        private async Task MoveCursorSmoothly(Vector2 targetPos, CancellationToken ct)
        {
            var currentPos = Input.ForceMousePosition;
            var distance = Vector2.Distance(currentPos, targetPos);
            var steps = Math.Clamp((int)(distance / 15), 8, 20);

            var totalTime = _random.Next(30, 80);
            var stepDelay = totalTime / steps;

            for (var i = 0; i < steps; i++)
            {
                if (ct.IsCancellationRequested) return;

                var t = (i + 1) / (float)steps;
                var randomOffset = new Vector2(
                    ((float)_random.NextDouble() * 2.5f) - 1.25f,
                    ((float)_random.NextDouble() * 2.5f) - 1.25f
                );
                var nextPos = Vector2.Lerp(currentPos, targetPos, t) + randomOffset;
                Input.SetCursorPos(nextPos);
                await Task.Delay(_random.Next(2, 5), ct);
            }

            Input.SetCursorPos(targetPos);
            await Task.Delay(25, ct);
        }
    }
}