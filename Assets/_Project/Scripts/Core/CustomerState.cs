namespace CafeSim.Core
{
    /// <summary>
    /// Estados por los que pasa un cliente desde que llega al local
    /// hasta que sale (atendido o por abandono).
    /// </summary>
    public enum CustomerState
    {
        /// <summary>Recién llega; aún no se ha enrolado en ninguna cola.</summary>
        Entering,

        /// <summary>En la cola de la caja, esperando para ordenar.</summary>
        WaitingInLine,

        /// <summary>Siendo atendido por el cajero (tomando la orden).</summary>
        Ordering,

        /// <summary>En la cola del barista, esperando su bebida.</summary>
        WaitingDrink,

        /// <summary>Siendo atendido por el barista (preparando la bebida).</summary>
        BeingServed,

        /// <summary>Sentado consumiendo en una mesa.</summary>
        Consuming,

        /// <summary>Consumiendo de pie porque no había mesa libre.</summary>
        ConsumingStanding,

        /// <summary>Saliendo del local (atendido satisfactoriamente).</summary>
        Leaving,

        /// <summary>Abandonó la cola por exceder su paciencia.</summary>
        Abandoned,

        /// <summary>Rechazado al llegar: la cola correspondiente estaba llena
        /// o se alcanzó el máximo de clientes simultáneos.</summary>
        Rejected
    }
}
