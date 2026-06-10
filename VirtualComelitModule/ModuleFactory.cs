using ComelitVirtualModule.Interfaces;
using ComelitVirtualModule.Models;

namespace ComelitVirtualModule
{
    internal static class ModuleFactory
    {
        private static readonly Dictionary<ModuleType, Func<short, IModule>> _registry = new()
        {
            { ModuleType.IOModule,  id => new Module8Output(id) }
        };

        public static IModule Create(ModuleType type, short moduleId)
        {
            if (_registry.TryGetValue(type, out var ctor))
                return ctor(moduleId);

            // fallback
            throw new NotSupportedException($"ModuleType non supportato: {type}");
        }

        // opzionale: per plugin/estensioni
        public static void Register(ModuleType type, Func<short, IModule> ctor)
            => _registry[type] = ctor;
    }
}
