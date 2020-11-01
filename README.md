# Agoda IoC Extensions 
![Build](https://github.com/agoda-com/Agoda.IoC/workflows/.github/workflows/build.yml/badge.svg?branch=main)
![Nuget](https://img.shields.io/nuget/v/agoda.ioc.netcore)
![Codecov](https://img.shields.io/codecov/c/github/agoda-com/agoda.ioc)

Share dotnet C# IoC implementation, used at Agoda for Registration of classes into IoC container based on Attributes. 

## The Problem?

Mostly in our larger projects at Agoda, teh Dependency inject registration was done in a single or set of "configuration" classes, these large configuration type files are troublesome due to frequency of merge conflicts. Also to look at a normal class and know if it will be run as a singleton or transient you need to dig into these configuration classes.

By declaring the IoC configuration at the top of each class it makes it immediately clear to the developer how the class's lifecycle is when running, and avoids large complex configuration classes.

## Adding to your project

Install the package, then add to your startup.cs class

```powershell
Install-Package Agoda.IoC.NetCore
```

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services.AutoWireAssembly(new[]{typeof(Startup).Assembly}, isMockMode);
        }
```

You need to pass in an array of all the assemblies you want to scan in your project for registration. As well as a boolean indicating if you applcaiiton is running in Mocked mode or not.

## Usage in your project

The basic usage of this project allow you to use 3 core attributes on your classes (RegisterTransient, RegisterPerRequest, RegisterSingleton) like the following code

```csharp

    // Simple registration
    public interface IService {}
    [RegisterPerRequest]
    public class Service : IService {}

```

The library will assembly scan your app at start-up and register services in IoC container based on the attribute and it's parameters.

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
Cas also be used to register multiple instances

```csharp

    [RegisterSingleton(For = typeof(IMultipleAttributes1))]
    [RegisterSingleton(For = typeof(IMultipleAttributes2))]
    public class MultipleAttributes : IMultipleAttributes1, IMultipleAttributes2 {}
```

And may more options...

## Mocked Mode?

Mocked mode is used for mocking external dependencies, this should be used on repositories that access a database for example. so you can run system tests on your application without the need for external dependencies.

Below example demonstrates using attributes to indicate a mock option for a registration.

```csharp

    // Mocked registration
    public class MockService : IServiceWithMock {}
    public interface IServiceWithMock{}
    [RegisterTransient(Mock = typeof(MockService))]
    public class ServiceWithMock : IServiceWithMock {}

```

## A Unity 3.5 Project?

Some of the old legacy systems at Agoda run an old version of unity, and this library was originally developed against that. These days every thing is moving towards net core, but we still decided to publish the original unity library as well.

## Dedication

A large amount of the code in this repository was written by Micheal Alastair Chamberlain who is no longer with us, so it is with this community contribution we remember him.