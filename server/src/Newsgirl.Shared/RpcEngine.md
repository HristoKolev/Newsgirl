
* Introspection
  - Static anaisys
  - Code generation

* Simple routing

* Decoupled from HTTP
	- Internal code
	- Simpler testing

# How to use

## Defining request handlers

In their simplest form, handler methods receive an object of type `RequestType` and return an object of type `ResponseType`. Both types are specified using the `RpcBindAttribute`, which accepts 2 arguments - one for each type. These arguments cannot be null.

The names of the methods don't matter, but they must be public non-virtual instance methods. 

Handler methods can only have parameters with types the same as the request type or any types that are whitelisted in `RpcEngineOptions.HandlerArgumentTypeWhiteList`. Objects of whitelisted types will be provided by registered middleware. Middleware will be explained in detail later.

The return type of a handler method must be either `Task<ResponseType>` or `Task<RpcResult<ResponseType>>`.

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

The request type is the most important piece of a handler. It's name is used to identify the handler method uniquely so that requests are routed correctly. Multiple handlers with the same request types are not allowed. If they were to be allowed and a request came in, which method should we call? 

The response type has to be present so that we can verify that the handler method returns it, but it does not have to be unique in any way. It can even be that same as the request type.

In the above example, the name of the type is `MathHandler`. That name doesn't matter, it can be anything you want. For the purposes of the RPC engine, it doesn't matter where a handler method is defined, multiple methods can be defined in the same type or in different types. The only restriction when it comes to the declaring type is that it must be a non-abstract class. Where the declaring type does matter is when it comes to middleware and supplemental attributes.

Each time the handler is being invoked an instance of the declaring type will be acquired and used for the invocation. How that instance is obtained will be explained later.

Handler methods must never return nullm wrapped in Task<> or otherwise, doing so will always cause an exception.    

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

In order to invoke a handler method you must call `RpcEngine.Execute<ResponseType>` passing an `RpcRequestMessage`. That object contains the name of the request that you are trying to invoke and the actual request object that gets passed to the handler method. An optional `Headers` property is available for use with middleware. The `Execute` method accepts an `InstanceProvider` object that it uses to resolve all handler and middleware types. The response type must match the response type of the handler, otherwise you may get a runtime exception telling you that the return type is not supported.

```c#

var requestMessage = new RpcRequestMessage
{
    Type = "IncrementRequest",
    Payload = new IncrementRequest { ... },
    Headers = ... // optional
};

var instanceProvider = ... // don't worry about this for now.

await rpcEngine.Execute<IncrementResponse>(requestMessage, instanceProvider);

```

Exceptions are thrown when one of those things happen:

* if the request message or it's payload are null.
* if the payload is of different type that the one specified as a string in the `Type` property
* if there is no handler for that request type
* if the handler method itself throws an exception
* if any of the middleware instances throw an exception when invoked

The handler method will be invoked along with the registered middleware and will return value of the method (wrapped in `RpcResult<>` if needed) will be returned from `Execute`.

`Execute` always returns `Task<RpcResult<ResponseType>>` if the handler method returns `Task<ResponseType>` it will be converted to `Task<RpcResult<ResponseType>>`.   

## Declaring and using middleware
