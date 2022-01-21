namespace Newsgirl.Server.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using Auth;
using Autofac;
using Autofac.Core;
using Shared;
using Xdxd.DotNet.Logging;
using Xdxd.DotNet.Shared;

public class HttpServerIoCModule : Module
{
    private readonly HttpServerApp app;

    public HttpServerIoCModule(HttpServerApp app)
    {
        this.app = app;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Globally managed
        builder.Register((_, _) => this.app.AppConfig).ExternallyOwned();
        builder.Register((_, _) => this.app.Log).As<Log>().ExternallyOwned();
        builder.Register((_, _) => this.app.RpcEngine).ExternallyOwned();
        builder.Register((_, _) => this.app.SessionCertificatePool).ExternallyOwned();

        // Per scope
        builder.Register((_, _) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
        builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();

        builder.Register(this.CreateErrorReporter).InstancePerLifetimeScope();
        builder.Register(this.CreateLogProcessor).InstancePerLifetimeScope();
        builder.RegisterType<AuthServiceImpl>().As<AuthService>().InstancePerLifetimeScope();
        builder.RegisterType<JwtServiceImpl>().As<JwtService>().InstancePerLifetimeScope();
        builder.RegisterType<PasswordServiceImpl>().As<PasswordService>().InstancePerLifetimeScope();
        builder.RegisterType<RpcRequestHandler>().InstancePerLifetimeScope();
        builder.RegisterType<LifetimeScopeInstanceProvider>().As<InstanceProvider>().InstancePerLifetimeScope();
        builder.RegisterType<AuthenticationFilter>().InstancePerLifetimeScope();
        builder.RegisterType<HttpRequestState>().InstancePerLifetimeScope();
        builder.RegisterType<RpcAuthorizationMiddleware>().InstancePerLifetimeScope();
        builder.RegisterType<RpcInputValidationMiddleware>().InstancePerLifetimeScope();
        builder.RegisterType<DateTimeServiceImpl>().As<DateTimeService>().InstancePerLifetimeScope();
        builder.RegisterType<RngServiceImpl>().As<RngService>().InstancePerLifetimeScope();

        // Always create
        var handlerClasses = this.app.RpcEngine.Metadata.Select(x => x.DeclaringType).Distinct().ToList();

        foreach (var handlerClass in handlerClasses)
        {
            builder.RegisterType(handlerClass);
        }

        base.Load(builder);
    }

    private ErrorReporter CreateErrorReporter(IComponentContext context, IEnumerable<Parameter> parameters)
    {
        var config = new ErrorReporterImplConfig
        {
            SentryDsn = this.app.AppConfig.SentryDsn,
            Environment = this.app.AppConfig.Environment,
            InstanceName = this.app.AppConfig.InstanceName,
            AppVersion = this.app.AppVersion,
        };

        return new ErrorReporterImpl(config);
    }

    private LogPreprocessor CreateLogProcessor(IComponentContext context, IEnumerable<Parameter> parameters)
    {
        var dateTimeService = context.Resolve<DateTimeService>();

        var config = new LogPreprocessorConfig
        {
            Environment = this.app.AppConfig.Environment,
            InstanceName = this.app.AppConfig.InstanceName,
            AppVersion = this.app.AppVersion,
        };

        return new LogPreprocessor(dateTimeService, config);
    }
}

public class LifetimeScopeInstanceProvider : InstanceProvider
{
    private readonly ILifetimeScope lifetimeScope;

    public LifetimeScopeInstanceProvider(ILifetimeScope lifetimeScope)
    {
        this.lifetimeScope = lifetimeScope;
    }

    public object Get(Type type)
    {
        return this.lifetimeScope.Resolve(type);
    }

    public T Get<T>()
    {
        return this.lifetimeScope.Resolve<T>();
    }
}

