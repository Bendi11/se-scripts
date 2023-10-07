
using System.Collections.Generic;


/// A user account associated with an entity ID pair (seated / unseated)
/// containing balance and tracking data
public class Account {
    /// Username as entered by the user
    public string Name;
    /// Hash of the user's password
    long _passwordHash;
    /// The value that actually tracks how many chips this account owns
    long _chipsBalance;
    /// Currently logged in user
    Session _session = null;

    public bool ValidPassword(long hash) => _passwordHash == hash;
}

/// Abstraction over account authorization and manipulation that allows for handling transactions offsite if required in the future
public class TransactionManager {
    Dictionary<string, Account> _accounts = new Dictionary<string, Account>();
    
    /// Result of calling the Authorize method
    public enum AuthorizeResult {
        InvalidUsername,
        InvalidPassword,
        Good,
    }

    /// Create a new active session for the user identified by the given entity ID
    public Yield Authorize(string username, string password, long id, out Session token, out AuthorizeResult result) {
        Account account;
        if(_accounts.TryGetValue(username, out account)) {
            if(account.ValidPassword(PasswordHash(password))) {
                token = null;
                result = AuthorizeResult.InvalidPassword;
                return Yield.Continue;
            }

            token = new Session() { EntityId = id };
            result = AuthorizeResult.Good;
            return Yield.Continue;
        } else {
            token = null;
            result = AuthorizeResult.InvalidUsername;
            return Yield.Continue;
        }
    }
    
    /// End the given session, ensuring that all operations on it are finished before revoking further access
    public IEnumerator<Yield> DestroySession(Session toDestroy) {
        yield return toDestroy.Lock.Lock();

        toDestroy.Account = null;

    }
    
    //TODO replace with a real password hash
    static long PasswordHash(string hash) { return (long)hash.GetHashCode(); }
}

/// A logged-in user referenced by their entity ID
public class Session {
    /// Entity ID of the user as detected by a sensor when sitting in a seat
    public long EntityId;
    /// Account that the session is modifying
    public Account Account;
    /// Mutex that controls **all** access to the account referenced by this session
    public Mutex Lock;
}
