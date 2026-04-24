using AspNetTemplate.Core.Infra.Attributes;

namespace AspNetTemplate.Core.Infra.Helpers
{
    public interface IErrorEventCollector
    {
        void Add(Func<Task> callback);
        Task InvokeAllAsync();
    }

    [AutoRegister(typeof(IErrorEventCollector))]
    public class ErrorEventCollector(ILogger<ErrorEventCollector> logger) : IErrorEventCollector
    {
        private readonly List<Func<Task>> _callbacks = [];

        public void Add(Func<Task> callback)
            => _callbacks.Add(callback);

        public async Task InvokeAllAsync()
        {
            foreach (var cb in _callbacks)
            {
                try { await cb(); }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ErrorEventCollector: a deferred callback threw an unhandled exception.");
                }
            }
        }
    }
}
