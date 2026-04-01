namespace Alea_Jacta_Est.Commands;

public interface ICommandQueue
{
    void Enqueue(IGameCommand command);
}
