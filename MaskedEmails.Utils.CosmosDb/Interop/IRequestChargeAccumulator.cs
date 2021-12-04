namespace Utils.CosmosDb.Interop
{
    public interface IRequestChargeAccumulator
    {
        void AccumulateRequestCharges(string name, double requestCharge);
    }
}
