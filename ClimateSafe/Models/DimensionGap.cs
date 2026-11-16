namespace ClimateSafe
{
    internal sealed class DimensionGap
    {
        public int DimensionId { get; }
        public string DimensionName { get; }
        public double Score { get; }

        public DimensionGap(int dimensionId, string dimensionName, double score)
        {
            DimensionId = dimensionId;
            DimensionName = dimensionName;
            Score = score;
        }
    }
}
