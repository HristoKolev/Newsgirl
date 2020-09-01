# The problem

ASP.NET does not provide a way of creating and destroying http servers at runtime. The `IHost` abstraction is not meant to be used in such a way. It expects to be the only running thing the process. It's not restartable.

# The solution

This module is an HTTP server as a library, not as a framework. You create it, give it a port and a `RequestDelegate` and you start it, when requests come they get processed by the `RequestDelegate`, when you don't need it anymore, you stop it and dispose of it. You can start as many HTTP servers as you want and they are in no way tied to the current process as ASP.NET is by default.

# How to use

```c#

await using (var server = new CustomHttpServerImpl())
{
    async Task EchoDelegate(HttpContext httpContext)
    {
        string requestBody = await httpContext.Request.ReadUtf8();

        await httpContext.Response.WriteUtf8(requestBody);
    }

    // start on random port
    await server.Start(EchoDelegate, new[] {"http://127.0.0.1:0"});

    // stop it
    await server.Stop();

    // start it again on random port
    await server.Start(EchoDelegate, new[] {"http://127.0.0.1:0"});

    // await here, after we exit the using statement, server will shut down.
}

```

# Future considerations

* What happens when a request is aborted.
* What happens when a request is in process and we dispose the server.
