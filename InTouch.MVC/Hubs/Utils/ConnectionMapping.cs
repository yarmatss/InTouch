namespace InTouch.MVC.Hubs.Utils;

// Helper class to track user connections
public class ConnectionMapping<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<string>> _connections = new();

    public int Count => _connections.Count;

    public void Add(T key, string connectionId)
    {
        lock (_connections)
        {
            if (!_connections.TryGetValue(key, out var connections))
            {
                connections = new HashSet<string>();
                _connections.Add(key, connections);
            }

            connections.Add(connectionId);
        }
    }

    public IEnumerable<string> GetConnections(T key)
    {
        if (_connections.TryGetValue(key, out var connections))
        {
            return connections;
        }
        return Enumerable.Empty<string>();
    }

    public void Remove(T key, string connectionId)
    {
        lock (_connections)
        {
            if (!_connections.TryGetValue(key, out var connections))
            {
                return;
            }

            connections.Remove(connectionId);

            if (connections.Count == 0)
            {
                _connections.Remove(key);
            }
        }
    }
}