
/// An MPSC channel used to send values and notify tasks of the new values
public class Channel<T> {
    Task _consumer;
    T _val;

    public Channel(Task consumer) {
        _consumer = consumer;
        _val = default(T);
    }
    
    /// Await a value to be sent to the channel
    public Yield AwaitReady() => Yield.Await;
    
    /// Receive a value that was sent by a producer
    public T Receive() => _val;

    /// Send a value through the channel
    public void Send(T val) {
        _val = val;
        Tasks.Wake(_consumer);
    }
}
