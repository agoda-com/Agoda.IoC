using Agoda.IoC.Generator.Abstractions;
using GeneratorExample.Interfaces;
using Microsoft.Extensions.Hosting;

namespace GeneratorExample;


[RegisterHostedService]
public class TimedHostedService : IHostedService, IDisposable
{
    private int executionCount = 0;
    private Timer? _timer = null;

    public TimedHostedService() { }

    public Task StartAsync(CancellationToken stoppingToken)
    {

        _timer = new Timer(DoWork, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        var count = Interlocked.Increment(ref executionCount);
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}



[RegisterScoped]
public class ClassA : IClassA
{
}

[RegisterScoped(Concrete = false)]
public class ClassB : IClassA
{
}



[RegisterSingleton(Factory = typeof(ClassBImplementationFactory))]
public class ClassC : IClassC
{
}
public interface IClassC { }


public class ClassBImplementationFactory : IImplementationFactory<IClassC>
{
    public IClassC Factory(IServiceProvider serviceProvider)
    {
        return new ClassC();
    }
}

[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 2)]
public class Pipeline2 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}
[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 3)]
public class Pipeline3 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline2);
    }
}
[RegisterSingleton(For = typeof(IPipeline), OfCollection = true, Order = 1)]
public class Pipeline1 : IPipeline
{
    public string Invoke()
    {
        return nameof(Pipeline1);
    }
}
public interface IPipeline
{
    string Invoke();
}
[RegisterSingleton(For = typeof(IMiddleware), OfCollection = true, Order = 3)]
public class Middleware2 : IMiddleware
{
    public string Invoke()
    {
        return nameof(Middleware2);
    }
}
[RegisterSingleton(For = typeof(IMiddleware), OfCollection = true, Order = 5)]
public class Middleware1 : IMiddleware
{
    public string Invoke()
    {
        return nameof(Middleware1);
    }
}
public interface IMiddleware
{
    string Invoke();
}


[RegisterSingleton]
public class DoWork<T> where T : new()
{
    public T Process()
    {
        return new T();
    }
}

[RegisterSingleton]
public class GenericeDoWork<T> : IDoWork<T> where T : new ()
{

    public T Process()
    {
        return new T();
    }
}
public interface IDoWork<T> where T : new()
{
    T Process();
}
