namespace Agoda.IoC.Generator;

internal class RegistrationContext
{
    internal RegistrationType RegistrationType { get; set; }
    internal string ForType { get; set; }
    internal bool IsConcrete { get; set; }
    internal string ConcreteType { get; set; }
    public bool IsReplaceService { get; set; }
    public bool IsOpenGeneric { get; set; }
    public bool IsUseFactory { get; set; }
    public string ImplementationFactoryCode { get; set; }
}

internal enum RegistrationType
{
    Scoped,
    Singleton,
    Transient,
    HostedService
}

