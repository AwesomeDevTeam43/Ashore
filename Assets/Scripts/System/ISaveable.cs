public interface ISaveable
{
    // Returns a serializable object holding the object's state
    object CaptureState();

    // Restores the object's state from the given data
    void RestoreState(object state);
}
