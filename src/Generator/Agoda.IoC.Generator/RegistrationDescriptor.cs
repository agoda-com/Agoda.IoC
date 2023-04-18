using Agoda.IoC.Generator.Abstractions;
using Microsoft.CodeAnalysis;
using System.Text;

namespace Agoda.IoC.Generator;

internal class RegistrationDescriptor
{
    private readonly INamedTypeSymbol _registrationSymbol;

    internal RegistrationDescriptor(INamedTypeSymbol registrationSymbol)
    {
        _registrationSymbol = registrationSymbol;
    }

    internal List<RegistrationContext> RegistrationContexts { get; private set; } = new List<RegistrationContext>();

    internal HashSet<string> NameSpaces { get; private set; } = new HashSet<string>();

    internal void Build()
    {
        ParseRegistrationAttributes();
    }

    internal string RegistrationCode()
    {
        var codes = new StringBuilder();
        foreach (var reg in RegistrationContexts)
        {
            var code = reg switch
            {
                { RegistrationType: RegistrationType.Singleton } singleton => BuildSingletonRegistrationCode(singleton),
                { RegistrationType: RegistrationType.Scoped } scopeRegister => BuildScopedRegistrationCode(scopeRegister),
                { RegistrationType: RegistrationType.Transient } transient => BuildTransientRegistrationCode(transient),
                _ => string.Empty
            };

            if (code is { Length: > 0 })
            {
                codes.AppendLine($"\t\t\t{code}");
            }
        }

        return codes.ToString();
    }

    internal string BuildSingletonRegistrationCode(RegistrationContext singleton)
        => singleton switch
        {
            { IsReplaceService: true } replaceCase =>
                replaceCase switch
                {
                    { IsConcrete: true } replace
                        => string.Format(Constants.GENERATE_REPLACE_SINGLETON_SOURCE, replace.ConcreteType),
                    { IsConcrete: false } replace
                        => string.Format(Constants.GENERATE_REPLACE_SINGLETON_INTERFACE_SOURCE, replace.ForType, replace.ConcreteType),
                },
            { IsReplaceService: false } normalCase =>
                normalCase switch
                {
                    { IsUseFactory: true, ImplementationFactoryCode: { Length: > 0 } } factory
                        => string.Format(Constants.GENERATE_SINGLETON_IMPLEMENTATION_FACTORY, factory.ImplementationFactoryCode),

                    { IsConcrete: true, ConcreteType: { Length: > 0 }, IsOpenGeneric: true } openGeneric
                        => string.Format(Constants.GENERATE_SINGLETON_OPEN_GENERIC_SOURCE, openGeneric.ConcreteType),

                    { IsConcrete: true, ConcreteType: { Length: > 0 } } register
                        => string.Format(Constants.GENERATE_SINGLETON_SOURCE, register.ConcreteType),

                    { IsConcrete: false, ForType: { Length: > 0 }, ConcreteType: { Length: > 0 }, IsOpenGeneric: true } openGeneric
                        => string.Format(Constants.GENERATE_SINGLETON_INTERFACE_OPEN_GENERIC_SOURCE, openGeneric.ForType, openGeneric.ConcreteType),

                    { IsConcrete: false, ForType: { Length: > 0 }, ConcreteType: { Length: > 0 } } register
                        => string.Format(Constants.GENERATE_SINGLETON_INTERFACE_SOURCE, register.ForType, register.ConcreteType),
                },
            _ => string.Format(Constants.GENERATE_SINGLETON_SOURCE, singleton.ConcreteType)
        };

    internal string BuildScopedRegistrationCode(RegistrationContext scoped)
        => scoped switch
        {

            { IsReplaceService: true } replaceCase =>
                  replaceCase switch
                  {
                      { IsConcrete: true } replace
                        => string.Format(Constants.GENERATE_REPLACE_SCOPED_SOURCE, replace.ConcreteType),
                      { IsConcrete: false } replace
                        => string.Format(Constants.GENERATE_REPLACE_SCOPED_INTERFACE_SOURCE, replace.ForType, replace.ConcreteType),
                  },

            { IsReplaceService: false } normalCase =>
                  normalCase switch
                  {
                      { IsUseFactory: true, ImplementationFactoryCode: { Length: > 0 } } factory
                        => string.Format(Constants.GENERATE_SCOPED_IMPLEMENTATION_FACTORY, factory.ImplementationFactoryCode),

                      { IsConcrete: true, ConcreteType: { Length: > 0 }, IsOpenGeneric: true } openGeneric
                        => string.Format(Constants.GENERATE_SCOPED_OPEN_GENERIC_SOURCE, openGeneric.ConcreteType),

                      { IsConcrete: true, ConcreteType: { Length: > 0 } } register
                        => string.Format(Constants.GENERATE_SCOPED_SOURCE, register.ConcreteType),

                      { IsConcrete: false, ForType: { Length: > 0 }, ConcreteType: { Length: > 0 }, IsOpenGeneric: true } openGeneric
                        => string.Format(Constants.GENERATE_SCOPED_INTERFACE_OPEN_GENERIC_SOURCE, openGeneric.ForType, openGeneric.ConcreteType),

                      { IsConcrete: false, ForType: { Length: > 0 }, ConcreteType: { Length: > 0 } } register
                        => string.Format(Constants.GENERATE_SCOPED_INTERFACE_SOURCE, register.ForType, register.ConcreteType),
                  },
            _ => string.Format(Constants.GENERATE_SCOPED_SOURCE, scoped.ConcreteType)
        };

    internal string BuildTransientRegistrationCode(RegistrationContext transient)
       => transient switch
       {
           { IsReplaceService: true } replaceCase =>
                replaceCase switch
                {
                    { IsReplaceService: true, IsConcrete: true } replace
                        => string.Format(Constants.GENERATE_REPLACE_TRANSIENT_SOURCE, replace.ConcreteType),

                    { IsReplaceService: true, IsConcrete: false } replace
                        => string.Format(Constants.GENERATE_REPLACE_TRANSIENT_INTERFACE_SOURCE, replace.ForType, replace.ConcreteType),
                },

           { IsReplaceService: false } normalCase =>
                normalCase switch
                {
                    { IsUseFactory: true, ImplementationFactoryCode: { Length: > 0 } } factory
                      => string.Format(Constants.GENERATE_TRANSIENT_IMPLEMENTATION_FACTORY, factory.ImplementationFactoryCode),

                    { IsConcrete: true, ConcreteType: { Length: > 0 }, IsOpenGeneric: true } openGeneric
                      => string.Format(Constants.GENERATE_TRANSIENT_OPEN_GENERIC_SOURCE, openGeneric.ConcreteType),

                    { IsConcrete: true, ConcreteType: { Length: > 0 } } register
                      => string.Format(Constants.GENERATE_TRANSIENT_SOURCE, register.ConcreteType),

                    { IsConcrete: false, ForType: { Length: > 0 }, ConcreteType: { Length: > 0 }, IsOpenGeneric: true } openGeneric
                      => string.Format(Constants.GENERATE_TRANSIENT_INTERFACE_OPEN_GENERIC_SOURCE, openGeneric.ForType, openGeneric.ConcreteType),

                    { IsConcrete: false, ForType: { Length: > 0 }, ConcreteType: { Length: > 0 } } register
                      => string.Format(Constants.GENERATE_TRANSIENT_INTERFACE_SOURCE, register.ForType, register.ConcreteType),
                },
           _ => string.Format(Constants.GENERATE_TRANSIENT_SOURCE, transient.ConcreteType)
       };

    internal void ParseRegistrationAttributes()
    {
        foreach (var registrationAttribute in _registrationSymbol.GetAttributes())
        {
            if (!TryGetRegistrationType(registrationAttribute, out var registrationType)) { continue; }

            if (registrationAttribute.NamedArguments is { Length: > 0 })
            {
                var registrationContext = new RegistrationContext { RegistrationType = registrationType };
                foreach (var namedArguments in registrationAttribute.NamedArguments)
                {
                    switch (namedArguments.Key)
                    {
                        case nameof(ContainerRegistration.Concrete):
                            if (namedArguments.Value.Value is bool isConcrete)
                            {
                                registrationContext.IsConcrete = isConcrete;

                                var concreteTypeName = _registrationSymbol.Name;

                                if (_registrationSymbol.TypeArguments.Any())
                                {
                                    var typeArgs = string.Join(",", _registrationSymbol.TypeArguments.Select(t => t.Name));
                                    concreteTypeName += $"<{typeArgs}>";
                                    registrationContext.IsOpenGeneric = true;
                                }

                                registrationContext.ConcreteType = concreteTypeName;
                                NameSpaces.Add(_registrationSymbol.ContainingNamespace.ToDisplayString());
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
                        default:
                            break;
                    }
                }
                RegistrationContexts.Add(registrationContext);

            }
            // If Registration attribute do not configured For type attribute we will use the fi 
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

    private bool TryGetRegistrationType(AttributeData registrationAttribute, out RegistrationType registrationType)
    {
        registrationType = RegistrationType.Singleton;
        if (registrationAttribute.AttributeClass is not { } attributeClass)
        {
            return false;
        }

        var attributeFullName = attributeClass.ToDisplayString();
        if (!Constants.RegistrationTypes.TryGetValue(attributeFullName, out registrationType))
        {
            return false;
        }
        return true;
    }
}


