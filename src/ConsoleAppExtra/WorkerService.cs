using Microsoft.Extensions.Logging;

namespace ConsoleAppExtra;

public class WorkerService : IWorkerService
{
    protected readonly ILogger<WorkerService> _logger;

    public WorkerService(ILogger<WorkerService> logger) =>
        _logger = logger;

    public void DoWork1() => _logger.LogInformation("DoWork1()");
}

public interface IWorkerService
{
    void DoWork1();
}
