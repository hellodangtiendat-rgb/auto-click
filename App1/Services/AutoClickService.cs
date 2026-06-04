using App1.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace App1.Services
{
    public class AutoClickService
    {
        public async Task RunAsync(
            IList<ClickStep> steps,
            int loopCount,
            CancellationToken token,
            Action<string> log)
        {
            int currentLoop = 1;

            while (!token.IsCancellationRequested)
            {
                if (loopCount > 0 && currentLoop > loopCount)
                {
                    break;
                }

                log($"Vòng lặp {currentLoop}");

                foreach (ClickStep step in steps)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (step.Type == "Delay")
                    {
                        log($"Delay #{step.Index}: chờ {step.DelayMs}ms");

                        if (step.DelayMs > 0)
                        {
                            await Task.Delay(step.DelayMs, token);
                        }

                        continue;
                    }

                    if (step.Type == "Click")
                    {
                        log($"Click #{step.Index}: X={step.X}, Y={step.Y}");

                        MouseService.LeftClick(step.X, step.Y);

                        if (step.DelayMs > 0)
                        {
                            log($"Delay sau Click #{step.Index}: chờ {step.DelayMs}ms");
                            await Task.Delay(step.DelayMs, token);
                        }
                    }
                }

                currentLoop++;
            }
        }
    }
}