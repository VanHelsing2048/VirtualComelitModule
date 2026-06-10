namespace ComelitVirtualModule.Interfaces
{
    internal interface IModuleOutputs<TState> :IModule 
        where TState : unmanaged
    {
        /// Numero di uscite gestite (8, 16, 32, …)
        int OutputsCount { get; }

        /// Legge l'intero stato (bitmask) nel tipo TState
        TState GetOutputState();

        /// Aggiorna lo stato (bitmask) e allinea le proprietà/indicizzatore
        void UpdateOutputState(TState state);

        /// Accesso rapido per singola uscita (0..OutputsCount-1)
        bool this[int index] { get; }
    }
}
