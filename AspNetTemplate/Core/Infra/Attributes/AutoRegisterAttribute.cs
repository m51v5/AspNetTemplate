namespace AspNetTemplate.Core.Infra.Attributes
{
    /// <summary>
    /// Marks a class for automatic DI registration against the specified service type.
    /// Supports AllowMultiple so one class can register against multiple interfaces.
    /// </summary>
    /// <example>
    /// [AutoRegister(typeof(IUserService))]                             // Scoped (default)
    /// [AutoRegister(typeof(ICacheService), ServiceLifetime.Singleton)] // Singleton
    /// [AutoRegister(typeof(IReportJob),    ServiceLifetime.Transient)] // Transient
    /// public class UserService : IUserService { }
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AutoRegisterAttribute(
        Type? serviceType = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped) : Attribute
    {
        public Type? ServiceType { get; } = serviceType;
        public ServiceLifetime Lifetime { get; } = lifetime;
    }
}
