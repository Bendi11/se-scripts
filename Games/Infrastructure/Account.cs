
using System.Collections.Generic;


/// A user account associated with an entity ID pair (seated / unseated)
/// containing balance and tracking data
public class Account {
    /// Username as entered by the user
    public string Name;
    /// Hash of the user's password
    public long PasswordHash;
    /// The value that actually tracks how many chips this account owns
    public long ChipsBalance;
}

/// Abstraction over account authorization and manipulation that allows for handling transactions offsite if required in the future
public class TransactionManager {
    /// Create a new active session for the user identified by the given entity ID
    ///
    /// Returns a SessionToken
    public IEnumerator<Yield> Authorize(string username, string password, long id) {
        yield return Yield.Await;
    }     
}

/// A token that can reference a logged-in user
public struct SessionToken {
    /// Entity ID of the user as detected by a sensor when sitting in a seat
    public long EntityId;
}
