namespace Utils.CosmosDb.Interop
{
    public interface ICosmosRequestChargeOperations : ICosmosOperations
    {
        // TODO: set / reset
        // TODO: stack / push / pop

        double RequestCharges { get; }
    }
}
