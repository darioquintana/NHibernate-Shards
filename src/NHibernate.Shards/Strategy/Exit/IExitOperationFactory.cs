namespace NHibernate.Shards.Strategy.Exit
{
    public interface IExitOperationFactory
    {
        ExitOperation CreateExitOperation();
    }
}