using ComelitVirtualModule.Models;

namespace ComelitVirtualModule.Interfaces
{
    internal interface IModule
    {
        short ModuleId { get; }
        ModuleType ModuleType { get; }
    }
}
