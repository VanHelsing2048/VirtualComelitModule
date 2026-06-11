using ComelitVirtualModule.Models;
using VirtualComelitModule.Models;

namespace ComelitVirtualModule
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var options = AddonOptions.Load();
            var modules = options.GetModules();

            Console.WriteLine($"Connecting to Comelit bus {options.ComelitIp}:{options.ComelitPort}");
            Console.WriteLine($"Virtual modules: {string.Join(", ", modules.Select(m => $"{m.Name}[{m.Type}]@{m.Address}"))}");

            await using var client = new EthernetTlsClient(options.ComelitIp, options.ComelitPort, options.ComelitPassword);
            var responder = new ComelitBusResponder(client, modules);
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            client.FrameReceived += frame =>
            {
                Console.WriteLine($"RX: {BitConverter.ToString(frame)}");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await responder.HandleFrameAsync(frame, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Frame handling error: {ex.Message}");
                    }
                }, cts.Token);
            };

            await client.ConnectAsync(cts.Token).ConfigureAwait(false);
            Console.WriteLine("Connected. Listening for Comelit bus frames.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Stopping.");
            }
        }
    }
}
