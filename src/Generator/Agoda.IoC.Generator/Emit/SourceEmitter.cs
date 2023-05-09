using System.Text;

namespace Agoda.IoC.Generator.Emit;

internal static class SourceEmitter
{
    internal static string Build(IList<RegistrationContext> registrationContexts)
    {
        if( registrationContexts is { Count: 0 }) return string.Empty;

        var codes = new StringBuilder();
        foreach (var reg in registrationContexts)
        {
            var code = reg switch
            {
                { RegistrationType: RegistrationType.Singleton } singleton => BuildSingletonRegistrationCode(singleton),
                { RegistrationType: RegistrationType.Scoped } scopeRegister => BuildScopedRegistrationCode(scopeRegister),
                { RegistrationType: RegistrationType.Transient } transient => BuildTransientRegistrationCode(transient),
                { RegistrationType: RegistrationType.HostedService } hostedService => BuildHostedServiceRegistrationCode(hostedService),

                _ => string.Empty
            };

            if (code is { Length: > 0 })
            {
                codes.AppendLine($"\t\t\t{code}");
            }
        }

        return codes.ToString();
    }

    private static string BuildHostedServiceRegistrationCode(RegistrationContext hostedService)
    {
        return string.Format(Constants.GENERATE_HOSTED_SERVICE_SOURCE, hostedService.ConcreteType);
    }

    private static string BuildSingletonRegistrationCode(RegistrationContext singleton)
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

    private static string BuildScopedRegistrationCode(RegistrationContext scoped)
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

    private static string BuildTransientRegistrationCode(RegistrationContext transient)
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
}
