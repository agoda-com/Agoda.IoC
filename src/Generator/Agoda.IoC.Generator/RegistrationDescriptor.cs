﻿using Agoda.IoC.Generator.Abstractions;
using Microsoft.CodeAnalysis;

namespace Agoda.IoC.Generator;

internal class RegistrationDescriptor
{
    private readonly INamedTypeSymbol _registrationSymbol;

    internal RegistrationDescriptor(INamedTypeSymbol registrationSymbol) => _registrationSymbol = registrationSymbol;

    internal List<RegistrationContext> RegistrationContexts { get; private set; } = new List<RegistrationContext>();

    internal HashSet<string> NameSpaces { get; private set; } = new HashSet<string>();

    internal void Build() => ParseRegistrationAttributes();

    private void ParseRegistrationAttributes()
    {
        foreach (var registrationAttribute in _registrationSymbol.GetAttributes())
        {
            if (!TryGetRegistrationType(registrationAttribute, out var registrationType)) { continue; }


            if(registrationAttribute.AttributeClass is { } attributeClass && 
                attributeClass.ToDisplayString().Equals(Constants.RegisterHostedServiceName))
            {

                var hostedServiceClassName = _registrationSymbol.Name;
                var registrationContext = new RegistrationContext { 
                    RegistrationType = RegistrationType.HostedService, 
                    ConcreteType = hostedServiceClassName 
                };
                NameSpaces.Add(_registrationSymbol.ContainingNamespace.ToDisplayString());
                RegistrationContexts.Add(registrationContext);
            }
            else if (registrationAttribute.NamedArguments is { Length: > 0 })
            {
                var registrationContext = new RegistrationContext { RegistrationType = registrationType };
                foreach (var namedArguments in registrationAttribute.NamedArguments)
                {
                    switch (namedArguments.Key)
                    {
                        case nameof(ContainerRegistration.Concrete):
                            if (namedArguments.Value.Value is bool isConcrete)
                            {
                                NameSpaces.Add(_registrationSymbol.ContainingNamespace.ToDisplayString());
                                registrationContext.IsConcrete = isConcrete;

                                var concreteTypeName = _registrationSymbol.Name;

                                if (_registrationSymbol.TypeArguments.Any())
                                {
                                    var typeArgs = string.Join(",", _registrationSymbol.TypeArguments.Select(t => t.Name));
                                    concreteTypeName += $"<{typeArgs}>";
                                    registrationContext.IsOpenGeneric = true;
                                }

                                if (isConcrete) { registrationContext.ConcreteType = concreteTypeName; }
                                
                                if (!isConcrete && _registrationSymbol.Interfaces.FirstOrDefault() is { } firstInterface)
                                {
                                    registrationContext.ConcreteType = concreteTypeName;
                                    registrationContext.ForType = firstInterface.Name;
                                }


                            }
                            break;
                        case nameof(ContainerRegistration.ReplaceService):
                            registrationContext.IsReplaceService = namedArguments.Value.Value is bool isReplaceServices && isReplaceServices;
                            if (registrationAttribute.NamedArguments is { Length: 1 })
                            {
                                registrationContext.IsConcrete = true;
                                registrationContext.ConcreteType = _registrationSymbol.Name;
                            }
                            NameSpaces.Add(_registrationSymbol.ContainingNamespace.ToDisplayString());
                            break;
                        case nameof(ContainerRegistration.For):
                            if (namedArguments.Value.Value is not INamedTypeSymbol forAttribute) break;

                            registrationContext.IsOpenGeneric = forAttribute.TypeArguments.Any();
                            registrationContext.ForType = registrationContext.IsOpenGeneric
                                                         ? $"{forAttribute.Name}<{new string(',', forAttribute.TypeArguments.Length - 1)}>"
                                                         : forAttribute.Name;
                            NameSpaces.Add(forAttribute.ContainingNamespace.ToDisplayString());
                            registrationContext.ConcreteType = registrationContext.IsOpenGeneric
                                                        ? $"{_registrationSymbol.Name}<{new string(',', forAttribute.TypeArguments.Length - 1)}>"
                                                        : _registrationSymbol.Name;
                            NameSpaces.Add(_registrationSymbol.ContainingNamespace.ToDisplayString());
                            break;
                        case nameof(ContainerRegistration.Factory):
                            if (namedArguments.Value.Value is not INamedTypeSymbol factoryAttribute) break;

                            if (factoryAttribute.Interfaces.Any(i => i.ConstructedFrom.ToDisplayString().Equals(Constants.IMPLEMENTATION_FACTORY_INTERFACE, StringComparison.Ordinal)))
                            {
                                var className = factoryAttribute.MetadataName;
                                registrationContext.IsUseFactory = true;
                                registrationContext.ImplementationFactoryCode = $"sp => new {className}().Factory(sp)";
                                NameSpaces.Add(factoryAttribute.ContainingNamespace.ToDisplayString());
                            }
                            break;
                        case nameof(ContainerRegistration.OfCollection):
                            if (namedArguments.Value.Value is bool isCollection)
                            {
                                registrationContext.IsCollection = isCollection;
                            }
                            break;

                        case nameof(ContainerRegistration.Order):
                            if (namedArguments.Value.Value is int order)
                            {
                                registrationContext.Order = order;
                            }
                            break;
                        default:
                            break;
                    }
                }
                RegistrationContexts.Add(registrationContext);

            }
            // If Registration attribute do not configured For type attribute we will use the first interface
            else if (_registrationSymbol.Interfaces.Any())
            {
                var registrationContext = new RegistrationContext { RegistrationType = registrationType };
                var firstInterface = _registrationSymbol.Interfaces.FirstOrDefault();

                if (firstInterface.TypeArguments.Any())
                {
                    var comma = new string(',', firstInterface.TypeArguments.Length - 1);
                    registrationContext.ForType = $"{firstInterface.Name}<{comma}>";
                    registrationContext.ConcreteType = $"{_registrationSymbol.Name}<{comma}>";
                    registrationContext.IsOpenGeneric = true;                    
                }
                else
                {
                    registrationContext.ForType = firstInterface.Name;
                    registrationContext.ConcreteType = _registrationSymbol.Name;
                }
                NameSpaces.Add(firstInterface!.ContainingNamespace.ToDisplayString());
                NameSpaces.Add(_registrationSymbol!.ContainingNamespace.ToDisplayString());
                RegistrationContexts.Add(registrationContext);
            }
            // Implementation class
            else
            {
                var registrationContext = new RegistrationContext { RegistrationType = registrationType };
                if (_registrationSymbol.TypeArguments.Any())
                {
                    string comma = new string(',', _registrationSymbol.TypeArguments.Length - 1);
                    registrationContext.ConcreteType = $"{_registrationSymbol.Name}<{comma}>";
                    registrationContext.IsOpenGeneric = true;
                }
                else
                {
                    registrationContext.ConcreteType = _registrationSymbol.Name;
                }
                registrationContext.IsConcrete = true;
                NameSpaces.Add(_registrationSymbol.ContainingNamespace.ToDisplayString());
                RegistrationContexts.Add(registrationContext);
            }
        }
    }

    private static bool TryGetRegistrationType(AttributeData registrationAttribute, out RegistrationType registrationType)
    {
        registrationType = RegistrationType.Singleton;
        if (registrationAttribute.AttributeClass is not { } attributeClass)
        {
            return false;
        }

        var attributeFullName = attributeClass.ToDisplayString();
        if (!Constants.AttributeRegistrationTypes.TryGetValue(attributeFullName, out registrationType))
        {
            return false;
        }
        return true;
    }

}


