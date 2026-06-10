using ComelitVirtualModule.Models;
using VirtualComelitModule.Models;

namespace ComelitVirtualModule
{
    internal sealed class ComelitBusResponder
    {
        private readonly EthernetTlsClient _client;
        private readonly Dictionary<byte, Module8Output> _modules;
        private readonly Dictionary<byte, VirtualModuleMemory> _moduleMemories;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public ComelitBusResponder(EthernetTlsClient client, IEnumerable<VirtualOutputModuleOptions> modules)
        {
            _client = client;
            _modules = modules.ToDictionary(
                module => module.Address,
                module =>
                {
                    var output = new Module8Output(module.Address);
                    output.UpdateOutputState(module.InitialState);
                    return output;
                });
            _moduleMemories = _modules.Keys.ToDictionary(address => address, _ => new VirtualModuleMemory());
        }

        public async Task HandleFrameAsync(byte[] frame, CancellationToken cancellationToken = default)
        {
            if (frame.Length != Helpers.FrameLength)
                return;

            if (!TryHandleFrame(frame, out var response))
                return;

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _client.WriteAsync(response, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }

            Console.WriteLine($"TX: {BitConverter.ToString(response)}");
        }

        private bool TryHandleFrame(byte[] frame, out byte[] response)
        {
            response = [];

            if (IsPollAddress(frame))
                return TryPoll(frame, out response);

            if (IsOutputsNormal(frame))
                return TryOutputsNormal(frame, out response);

            if (IsForceOutputs(frame))
                return TryForceOutputs(frame, out response);

            if (IsReadMemory(frame))
                return TryReadMemory(frame, out response);

            if (IsWriteMemory(frame))
                return TryWriteMemory(frame, out response);

            if (IsReadMemoryWithPage(frame))
                return TryReadMemoryWithPage(frame, out response);

            if (IsWriteMemoryWithPage(frame))
                return TryWriteMemoryWithPage(frame, out response);

            if (IsReadOutput16Bit(frame))
                return TryReadOutput16Bit(frame, out response);

            if (IsWriteOutput16Bit(frame))
                return TryWriteOutput16Bit(frame, out response);

            return false;
        }

        private bool TryPoll(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_modules.TryGetValue(address, out var module))
                return false;

            response = [0x55, 0x01, address, 0x00, module.GetOutputState()];
            return true;
        }

        private bool TryOutputsNormal(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_modules.TryGetValue(address, out var module))
                return false;

            var mask = frame[3];
            var buttonStatus = frame[4];
            module.SetMaskedOutputs(mask, buttonStatus != 0);

            response = [0x55, 0x00, address, mask, module.GetOutputState()];
            return true;
        }

        private bool TryForceOutputs(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_modules.TryGetValue(address, out var module))
                return false;

            var mask = frame[3];
            var values = frame[4];
            module.ApplyOutputMask(mask, values);

            response = [0x55, 0x02, address, mask, module.GetOutputState()];
            return true;
        }

        private bool TryReadMemory(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_moduleMemories.TryGetValue(address, out var memory))
                return false;

            var cell = frame[3];
            response = [(byte)TechnobusCommand.MemoryResponse, 0x00, address, cell, memory.Read(0, cell)];
            return true;
        }

        private bool TryWriteMemory(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_moduleMemories.TryGetValue(address, out var memory))
                return false;

            var cell = frame[3];
            var value = frame[4];
            memory.Write(0, cell, value);
            response = [(byte)TechnobusCommand.MemoryResponse, 0x00, address, cell, value];
            return true;
        }

        private bool TryReadMemoryWithPage(byte[] frame, out byte[] response)
        {
            response = [];
            var page = frame[1];
            var address = frame[2];

            if (!_moduleMemories.TryGetValue(address, out var memory))
                return false;

            var cell = frame[3];
            response = [(byte)TechnobusCommand.MemoryWithPageResponse, page, address, cell, memory.Read(page, cell)];
            return true;
        }

        private bool TryWriteMemoryWithPage(byte[] frame, out byte[] response)
        {
            response = [];
            var page = frame[1];
            var address = frame[2];

            if (!_moduleMemories.TryGetValue(address, out var memory))
                return false;

            var cell = frame[3];
            var value = frame[4];
            memory.Write(page, cell, value);
            response = [(byte)TechnobusCommand.MemoryWithPageResponse, page, address, cell, value];
            return true;
        }

        private bool TryReadOutput16Bit(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_modules.TryGetValue(address, out var module))
                return false;

            response = [(byte)TechnobusCommand.Response, frame[1], address, 0x00, module.GetOutputState()];
            return true;
        }

        private bool TryWriteOutput16Bit(byte[] frame, out byte[] response)
        {
            response = [];
            var address = frame[2];

            if (!_modules.TryGetValue(address, out var module))
                return false;

            module.UpdateOutputState(frame[4]);
            response = [(byte)TechnobusCommand.Response, frame[1], address, frame[3], frame[4]];
            return true;
        }

        private static bool IsPollAddress(byte[] frame)
            => frame[0] == 0x55 && frame[1] == (byte)DataType.Interrogate;

        private static bool IsOutputsNormal(byte[] frame)
            => frame[0] == 0x55 && frame[1] == (byte)TechnobusCommand.OutputsNormal;

        private static bool IsForceOutputs(byte[] frame)
            => frame[0] == 0x55 && frame[1] == (byte)TechnobusCommand.ForceOutputs;

        private static bool IsReadMemory(byte[] frame)
            => frame[0] == (byte)TechnobusCommand.ReadMemory;

        private static bool IsWriteMemory(byte[] frame)
            => frame[0] == (byte)TechnobusCommand.WriteMemory;

        private static bool IsReadMemoryWithPage(byte[] frame)
            => frame[0] == (byte)TechnobusCommand.ReadMemoryWithPage;

        private static bool IsWriteMemoryWithPage(byte[] frame)
            => frame[0] == (byte)TechnobusCommand.WriteMemoryWithPage;

        private static bool IsReadOutput16Bit(byte[] frame)
            => frame[0] == (byte)TechnobusCommand.ReadOutput16Bit;

        private static bool IsWriteOutput16Bit(byte[] frame)
            => frame[0] == (byte)TechnobusCommand.WriteOutput16Bit;
    }
}
