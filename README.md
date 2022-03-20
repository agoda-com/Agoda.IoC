# Agoda IoC Extensions 
![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/agoda-com/Agoda.IoC/.NET%20Core%20Build%20and%20Publish/main)
![Nuget](https://img.shields.io/nuget/v/agoda.ioc.netcore)
![Codecov](https://img.shields.io/codecov/c/github/agoda-com/agoda.ioc)

C# IoC extension library, used at Agoda for Registration of classes into IoC container based on Attributes. 

## The Problem?

In some of our larger projects at Agoda, the Dependency injection registration was done in a single or set of "configuration" classes. These large configuration type files are troublesome due to frequency of merge conflicts. Also to look at a normal class and know if it will be run as a singleton or transient you need to dig into these configuration classes.

By declaring the IoC configuration at the top of each class in an attribute it makes it immediately clear to the developer what the class's lifecycle is when running, and avoids large complex configuration classes that are prone to merge conflicts.

## Adding to your project

Install the package, then add to your Startup like below.

```powershell
Install-Package Agoda.IoC.NetCore
```

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services.AutoWireAssembly(new[]{typeof(Startup).Assembly}, isMockMode);
        }
```

You need to pass in an array of all the assemblies you want to scan in your project for registration. As well as a boolean indicating if your application is running in Mocked mode or not.

## Usage in your project

The basic usage of this project allows you to use 3 core attributes on your classes (RegisterTransient, RegisterPerRequest, RegisterSingleton) like the following code:

```csharp

    // Simple registration
    public interface IService {}
    [RegisterSingleton] /// replaces services.AddSingleton<IService, Service>();
    public class Service : IService {}

```
Replaces something like this in your startup.
```csharp
services.AddSingleton<IService , Service>();
```

The library will assembly scan your app at start-up and register services in IoC container based on the attribute and its parameters.

Factory options are available for registration with the attributes, as seen below, the factory needs a "Build" method

```csharp
    [RegisterSingleton(Factory = typeof(MySingletonFactory))]
    public class SingletonFromFactory : ISingletonFromFactory
    {
        // implementation here
    }
    public class MySingletonFactory : IComponentFactory<ISingletonFromFactory>
    {
        public ISingletonFromFactory Build(IComponentResolver c)
        {
            return new SingletonFromFactory("test");
        }
    }
```

For services with multiple interfaces the interface can be explicitly declared like below

```csharp
// This class implements 2 interfaces, but we explicitly tell it to register only 1.
    [RegisterTransient(For = typeof(IExplicitlyRegisteredInterface))]
    public class ServiceWithExplicitInterfaceRegistration : IExplicitlyRegisteredInterface, IInterfaceThatShouldNotGetRegistered {}
```
It can also be used to register multiple instances

```csharp

    [RegisterSingleton(For = typeof(IMultipleAttributes1))]
    [RegisterSingleton(For = typeof(IMultipleAttributes2))]
    public class MultipleAttributes : IMultipleAttributes1, IMultipleAttributes2 {}
```
## Keyed Registration


```csharp

    [RegisterSingleton(Key = "Service_1")]
    public class KeyedFactoryService1 : IKeyedFactoryService {} 
    [RegisterPerRequest(Key = "Service_2")]
    public class KeyedFactoryService2 : IKeyedFactoryService {}


public ctor(IKeyedComponentFactory<IKeyedFactoryService> _keyedFactoryService)
{
    _service2 = _keyedFactoryService.GetByKey("Service_2")
}
```

And may more options...

## Mocked Mode?

Mocked mode is used for mocking external dependencies, this should be used on repository type classes that access a database for example. So you can run system tests on your application without the need for external dependencies.

Below example demonstrates using attributes to indicate a mock option for a registration.

```csharp

    // Mocked registration
    public class MockService : IServiceWithMock {}
    public interface IServiceWithMock{}
    [RegisterTransient(Mock = typeof(MockService))]
    public class ServiceWithMock : IServiceWithMock {}

```
## Autofac support?

A lot of the functionality of this library is already in autofac, the reason for adding autofac support was to allow easier migration of some of our net framework projects to netcore using this library.

Autofac functionality is does not include support for open generic service registration. 

## A Unity 3.5 Project?

Some of the old legacy systems at Agoda run an old version of unity, and this library was originally developed against that. These days everything is moving towards net core, but we still decided to publish the original unity library as well.

## This is using reflection, isn't that slow?

Reflection is not slow, it's pretty fast in C# actually. Where you will hit problems with reflection and speed is if you are doing thousands or millions of operations, like in a http request on a busy website, using it at startup like this you are doing very few operations and only when the application starts.

## How to get started?

Install the nuget package into your csproj project file

```bash
dotnet add package Agoda.IoC.NetCore
```

Add the library into your startup.cs

```csharp
      services.AutoWireAssembly(new[]{typeof(Startup).Assembly}, isMockMode);
```

or for net 6 minimal API Program.cs

```csharp
      builder.Services.AutoWireAssembly(new[]{typeof(Startup).Assembly}, isMockMode);
```

All of the lines like this in your startup

```csharp
services.AddSingleton<IService , Service>();
```

Can be removed, and each one that you remove, find the class and add the appropriate attribute

```csharp

    [RegisterSingleton] ///add this line
    public class Service : IService 
    {
    // code that does something
    }

```
For net core the mappings of registration method to attribute are

```csharp
services.AddTransient<>(); // [RegisterTransient]
services.AddScoped<>(); // [RegisterPerRequest]
services.AddSingleton<>(); // [RegisterSingleton]
```

If the class inherit's from multiple Interfaces, use the "For" proeprty on the attribute

```csharp
    [RegisterSingleton(For = typeof(IMultipleAttributes1))]
```

This should cover most common use cases, for more complex one's you'll need to use the other attribute options metioned above.


## Dedication
A large amount of the code in this repository was written by Michael Alastair Chamberlain who is no longer with us, so it is with this community contribution we remember him.
