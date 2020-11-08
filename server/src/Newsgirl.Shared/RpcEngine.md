# How to use

## Defining request handlers

In their simplest form, handler methods receive an object of type `RequestType` and return an object of type `ResponseType`. Both types are specified using the `RpcBindAttribute`, which accepts 2 arguments - one for each type. These arguments cannot be null.

The names of the methods don't matter, but they must be public non-virtual instance methods. 

Handler methods can only have parameters with types the same as the request type or any types that are whitelisted in `RpcEngineOptions.ParameterTypeWhitelist`. Objects of whitelisted types will be provided by registered middleware. Middleware will be explained in detail later.

The return type of a handler method must be either `Task<ResponseType>` or `Task<Result<ResponseType>>`.

Here is an example of a handler that receives a number, increments it an returns it:

```c#

public class MathHandler
{
    [RpcBind(typeof(IncrementRequest), typeof(IncrementResponse))]
    public async Task<IncrementResponse> Increment(IncrementRequest req)
    {
        return new IncrementResponse
        {
            IncrementedNumber = req.Number + 1,
        };
    }
}

public class IncrementRequest
{
    public int Number { get; set; }
}

public class IncrementResponse
{
    public int IncrementedNumber { get; set; }
}

```

The request type is the most important piece of a handler. It's name is used to identify the handler method uniquely so that requests are routed correctly. Multiple handlers with the same request types are not allowed. If they were to be allowed and a request came in, which method should we call? The request type must be a reference type.

The response type has to be present so that we can verify that the handler method returns it, but it does not have to be unique in any way. It can even be that same as the request type. The response type must be a reference type.

In the above example, the name of the type is `MathHandler`. That name doesn't matter, it can be anything you want. For the purposes of the RPC engine, it doesn't matter where a handler method is defined, multiple methods can be defined in the same type or in different types. The only restriction when it comes to the declaring type is that it must be a non-abstract class. Where the declaring type does matter is supplemental attribute specification.

Each time the handler is being invoked an instance of the declaring type will be acquired and used for the invocation. How that instance is obtained will be explained later.

Handler methods must never return null wrapped in `Task<>` or otherwise, doing so will always cause an exception.

## Processing requests

In order to process requests you must create and instance of `RpcEngine` passing `RpcEngineOptions` as an argument.

The most important property is `RpcEngineOptions.PotentialHandlerTypes` which accepts `Type[]`. These types will be scanned for methods decorated with `RpcBindAttribute`, which will make up the list of handlers that this `RpcEngine` instance can invoke. 

```c#
var rpcEngine = new RpcEngine(new RpcEngineOptions
{
    PotentialHandlerTypes = new[]
    {
        typeof(MathHandler),
    },
});

```

Types that don't have eligible handler methods are simply ignored. The idea here is that you don't have to register each handler type manually, just enumerate the types of an entire assembly and the `RpcEngine` instance will find those that it can use. If a method is marked with `RpcBindAttribute` but it violates some rule that will prevent it from being used as a handler - an exception will be thrown when `RpcEngine` is instantiated. A method marked with `RpcBindAttribute` will never be ignored - if it adheres to the rules, it will be used, if not - an exception will be thrown, describing what rule is being broken.

In order to invoke a handler method you must call `RpcEngine.Execute` passing an `RpcRequestMessage`. That object contains the name of the request that you are trying to invoke and the actual request object that gets passed to the handler method. An optional `Headers` property is available for use with middleware. The `Execute` method accepts an `InstanceProvider` object that it uses to resolve all handler and middleware types.

```c#

var requestMessage = new RpcRequestMessage
{
    Type = "IncrementRequest",
    Payload = new IncrementRequest { ... },
    Headers = ... // optional
};

var instanceProvider = ... // don't worry about this for now.

await rpcEngine.Execute(requestMessage, instanceProvider);

```

Exceptions are thrown when one of those things happen:

* if the request message or it's payload are null.
* if the payload is of different type that the one specified as a string in the `Type` property
* if there is no handler for that request type
* if the handler method itself throws an exception
* if any of the middleware instances throw an exception

The handler method will be invoked along with the registered middleware and will return value of the method (wrapped in `Result<>` if needed) will be returned from `Execute`.

`Execute` always returns `Task<Result<object>>`.   

The instance provider object is used to obtain an instance of the handler and middleware types (if registered).

## Middleware

The idea is very similar to ASP.NET middleware and expressjs middleware.

### Defining middleware

All middleware types derive from `RpcMiddleware` and as such have a `Run` method that accepts:

 * `RpcContext` - Contains the state for the current request: The request object, the response object, request metadata, and instances of types to be injected into the handler method. 
 * `InstanceProvider` - this is whatever you pass to `RpcEngine.Execute`.
 * `RpcRequestDelegate` - the 'next' piece of the chain. Either another middleware `Run` method or a method that directly calls the handler.

Here is the simplest implementation:

```c#

public class SimpleMiddleware : RpcMiddleware
{
    public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
    {
        await next(context, instanceProvider);
    }
}

```

And here is how to register it:

```c#

var rpcEngine = new RpcEngine(new RpcEngineOptions
{
    PotentialHandlerTypes = new[]
    {
        ...
    },
    MiddlewareTypes = new[]
    {
        typeof(SimpleMiddleware1),
        typeof(SimpleMiddleware2),
        typeof(SimpleMiddleware3),
    },
});

```

### Middleware execution

Middleware will be executed in the order specified in `RpcEngineOptions.MiddlewareTypes`. 

Here is an example of middleware that simply logs what requests are being processed:

```c#

public class ExampleLoggingMiddleware : RpcMiddleware
{
    public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
    {
        Console.WriteLine($"Before processing: {context.RequestMessage.Type}");

        await next(context, instanceProvider);

        Console.WriteLine($"After processing: {context.RequestMessage.Type}");
    }
}

```

Here is an example error handling middleware:

```c#

public class ExampleErrorHandlingMiddleware : RpcMiddleware
{
    public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
    {
        try
        {
            await next(context, instanceProvider);
        }
        catch (Exception exception)
        {
            context.SetResponse(Result.Error($"An error occurred while executing request: {exception.Message}"));
        }
    }
}

```

In an event that the next link in the chain (another middleware or the handler itself) throws an exception, we return an error `Result` to the caller, instead of propagating the exception. If you want to set the response from middleware, you need to call `SetResponse` which accepts an object of type `Result`.

### Supplemental attributes

Supplemental attributes are attributes that derive from `RpcSupplementalAttribute`. When a new `RpcEngine` instance is created supplemental attributes are collected from handler methods and their declaring type. They can be accessed by middleware using `context.GetSupplementalAttribute<AttributeType>`. Only one attribute specification is taken into account for each request type and if attributes are specified on both the handler method and it's declaring type - the one from the method is taken into account and the one from the type is ignored.

Here is an example of supplemental attribute usage:

```c#

public class AuthAttribute : RpcSupplementalAttribute
{
    public bool RequiresAuthentication { get; set; }
}

[Auth(RequiresAuthentication = false)] // used on both type and method - this one is ignored
public class ExampleHandler
{
    [Auth(RequiresAuthentication = true)] // used on both type and method - this one is used
    [RpcBind(typeof(ExampleRequest), typeof(ExampleResponse))]
    // in order to inject AuthResult you have to add it to the whitelist when creating the RpcEngine
    public Task<ExampleResponse> Example(ExampleRequest req, AuthResult authResult)
    {
        // use the userID somehow
        int userID = authResult.UserID;

        return Task.FromResult(new ExampleResponse
        {
            Number = req.Number + 1,
        });
    }
}

public class ExampleAuthenticationMiddleware : RpcMiddleware
{
    public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
    {
        string token = context.RequestMessage.Headers.GetValueOrDefault("Authorization");

        var authResult = await this.Authenticate(token);

        var authAttr = context.GetSupplementalAttribute<AuthAttribute>();

        if (authAttr != null // if we have such attribute 
            && authAttr.RequiresAuthentication // and it requires the user to be authenticated 
            && !authResult.IsLoggedIn // and the user is not authenticated
        )
        {
            context.SetResponse(Result.Error("Unauthorized access."));
            return;
        }

        // set the auth result so that it can be injected into the handler and it can find out the userID
        context.SetHandlerArgument(authResult);

        await next(context, instanceProvider); // next
    }

    /// <summary>
    /// Do authentication logic. We just check if the token is test123
    /// </summary>
    private Task<AuthResult> Authenticate(string token)
    {
        if (token != "test123")
        {
            return Task.FromResult(new AuthResult
            {
                IsLoggedIn = false,
            });
        }

        // TODO: decode the token and decide what to do

        return Task.FromResult(new AuthResult
        {
            UserID = 42,
            IsLoggedIn = true,
        });
    }
}

public class ExampleRequest
{
    public int Number { get; set; }
}

public class ExampleResponse
{
    public int Number { get; set; }
}

public class AuthResult
{
    public bool IsLoggedIn { get; set; }

    public int UserID { get; set; }
}

```

The above example declares `AuthAttribute` that extends `RpcSupplementalAttribute` and uses that attribute to decorate both the handler type and the handler method. When that happens the method's attributes wins. If the authentication is successful the handler method will receive the `AuthResult` object that is passed in `context.SetHandlerArgument` by the middleware. In case the authentication step fails an error result is being set by calling `context.SetResponse` and the middleware method returns, without calling the next chain in the middleware list, thus skipping the execution of the handler method or any further middleware.

### Future considerations

* The `RpcContext` abstraction exposes too much information to middleware classes. On the other hand, if we make some of the members private we will prevent dynamically emitted code to access them. One solution is to pass the context to the middleware as an interface. We can also run the dynamic assemblies in such a way that code can access private members of other assemblies.

* Currently when we generate dynamic types we don't clean them up afterwards, this is only going to become a problem if we create to many instances of `RpcEngine`, but it is a problem nonetheless. Maybe we can use `AssemblyLoadContext` to solve that problem. We have to do some research in order to understand how it works exactly and if it will harm performance in any way.

* Currently we create lots lots of new objects when we execute requests. We have to profile our code and determine how much work the GC does for each `Execute` call and remove some allocations by pooling objects an document the rest.
