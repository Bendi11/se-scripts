
using System.Collections.Generic;

/// Global state containing all blocks on the casino
public class Casino {
    public TransactionManager _mgr {
        get;
        private set;
    }

    public Casino Instance {
        get;
        private set;
    } = new Casino();
    
    /// A map of device locators to their login terminal states
    public Dictionary<string, LoginTerminal> _login;
    
    /// Handle notifications for the given login terminals
    public void OnNotification(string arg) {
        LoginTerminal term;
        if(_login.TryGetValue(arg, out term)) {
            
        }
    }
}
