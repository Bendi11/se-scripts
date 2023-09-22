
/// A user account associated with an entity ID pair (seated / unseated)
/// containing balance and tracking data
public class Account {
    public long EntityId;
    public long SeatedId;
    
    /// Name of the player as recorded by a sensor
    public string Name;
    
    /// The value that actually tracks how many chips this account owns
    public long ChipsBalance;
}
