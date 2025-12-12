namespace Ashore.AI.DecisionTree
{
    public interface IFeatureProvider
    {
        // Fill the provided array with features in the order expected by the model
        void CollectFeatures(float[] buffer);
        // Optional: number of features expected
        int FeatureCount { get; }
    }
}
