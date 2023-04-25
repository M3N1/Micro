using ServiceWorker;

IHost host = Host.CreateDefaultBuilder(args)
.ConfigureServices(services =>
{
    services.AddHostedService<WorkerPlanning>();
    services.AddHostedService<WorkerMaintenance>();
})
.ConfigureLogging(logging =>
{
    logging.ClearProviders();
}).UseNLog()
.Build();



