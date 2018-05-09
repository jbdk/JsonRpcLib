# JsonRpcLib
##### C# DotNetCore 2.1+ Client/Server Json RPC library
<i>Using Span&lt;T&gt;, Memory&lt;T&gt; and IO pipelines</i>

[![license](https://img.shields.io/github/license/jbdk/JsonRpcLib.svg)](LICENSE.md)
[![Build Status](https://travis-ci.org/jbdk/JsonRpcLib.svg?branch=master)](https://travis-ci.org/jbdk/JsonRpcLib)


### Current performance 
Run the PerfTest app
 - 8 threads 1,000,000 json notify -> static class call: `1,250,000 requests/sec`
 - 8 threads 100,000 json invoke -> static class call: `35,000 requests/sec` 

Test machine: 3.4 Ghz i5 3570

# The Server
JsonRpc server using SocketListener class (corefxlab)
````csharp
public class MyServer : JsonRpcServer
{
    readonly SocketListener _listener;
    TaskCompletionSource<int> _tcs = new TaskCompletionSource<int>();

    public MyServer(int port)
    {
        _listener = new SocketListener();
        _listener.Start(new IPEndPoint(IPAddress.Any, port));
        _listener.OnConnection(OnConnection);
    }

    private Task OnConnection(SocketConnection connection)
    {
        IClient client = AttachClient(connection.GetRemoteIp(), connection);
        ClientConnected.Set();
        return _tcs.Task;
    }

    public override void Dispose()
    {
        _tcs.TrySetCanceled();
        _listener?.Stop();
        base.Dispose();
    }
}
````

Start the server and register methods

````csharp
const int port = 7733;
using(var server = new MyServer(port))
{
    // Bind to functions on static class
    server.Bind(typeof(Target));    

    // Bind to a delegate
    server.Bind("DelegateMethod", (Action<int,int>)( (a, b)
        => Debug.WriteLine($"DelegateMethod. a={a} b={b}") ));
}

static class Target
{
    public static int TestMethod()
    {
        Debug.WriteLine("Method1 called");
        return 42;
    }
}

````
# The Client
JsonRpc client using SocketConnection class (corefxlab)
````csharp
public class MyClient
{
    private readonly SocketConnection _conn;

    public static async Task<JsonRpcClient> ConnectAsync(int port)
    {
        var c = await SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
        return new JsonRpcClient(c);
    }
}
````

Connect client to server and call the methods
````csharp
const int port = 7733;
using(var client = MyClient.ConnectAsync(port).Result)
{
    var result = client.Invoke<int>("TestMethod");
    client.Invoke("DelegateMethod", 44, 76);

    // Fire-and-forget 
    client.Notify("DelegateMethod", 2, 6);
}
````
