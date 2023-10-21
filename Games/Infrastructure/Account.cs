
using System.Collections.Generic;


/// A user account associated with an entity ID pair (seated / unseated)
/// containing balance and tracking data
public class Account {
    /// Username as entered by the user
    public string Name;
    /// Hash of the user's password
    public long PasswordHash;
    /// The value that actually tracks how many chips this account owns
    long _chipsBalance = 200;
    /// Currently logged in user
    Session _session = null;

    public bool ValidPassword(long hash) => PasswordHash == hash;
}

/// Abstraction over account authorization and manipulation that allows for handling transactions offsite if required in the future
public class TransactionManager {
    Dictionary<string, Account> _accounts = new Dictionary<string, Account>();

    private TransactionManager() {}
    
    /// Result of calling the Authorize method
    public enum AuthorizeResult {
        InvalidUsername,
        InvalidPassword,
        Good,
    }
    
    /// Result of calling the CreateAccount method
    public enum CreateAccountResult {
        AlreadyExists,
        Good,
    }

    /// Create a new active session for the user identified by the given entity ID
    public void Authorize(string username, string password, long id, out Session token, out AuthorizeResult result) {
        Account account;
        if(_accounts.TryGetValue(username, out account)) {
            if(account.ValidPassword(PasswordHash(password))) {
                token = null;
                result = AuthorizeResult.InvalidPassword;
            }

            token = new Session() { EntityId = id };
            result = AuthorizeResult.Good;
        } else {
            token = null;
            result = AuthorizeResult.InvalidUsername;
        }
    }
    
    /// Create a new account with the given credentials
    public void CreateAccount(string username, string password, out CreateAccountResult result) {
        Account existing;
        if(_accounts.TryGetValue(username, out existing)) {
            result = CreateAccountResult.AlreadyExists;
            return;
        }

        var account = new Account() {
            Name = username,
            PasswordHash = PasswordHash(password),
        };

        _accounts.Add(username, account);
        result = CreateAccountResult.Good;
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
