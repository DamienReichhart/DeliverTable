using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableScheduler.Configuration;
using DeliverTableScheduler.Jobs;
using DeliverTableServer.Configuration;
using DeliverTableServer.Services;
using DeliverTableServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;

DotNetEnv.Env.Load();
SchedulerEnvironment env = SchedulerEnvironment.Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(env);
builder.Services.AddDbContext<DeliverTableContext>(opts => opts.UseNpgsql(env.ConnectionStringDatabase));

Stripe.StripeConfiguration.ApiKey = env.StripeSecretKey;
builder.Services.AddSingleton<IStripeGateway, StripeGateway>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
builder.Services.AddScoped<IDiscountCodeRepository, DiscountCodeRepository>();
builder.Services.AddScoped<IPaymentLifecycleService, PaymentLifecycleService>();

builder.Services.AddHostedService<OrderAbandonmentSweep>();
builder.Services.AddHostedService<OrderRestaurantTimeoutSweep>();

// Repositories required by the commission statement service.
builder.Services.AddScoped<ICommissionStatementRepository, CommissionStatementRepository>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();

// AppEnvironment from the server project (loads commission rate, VAT, legal info from env vars).
AppEnvironment appEnv = AppEnvironment.Load();
builder.Services.AddSingleton(appEnv);
builder.Services.AddScoped<ICommissionStatementService, CommissionStatementService>();

// RabbitMQ publisher (same config wiring as DeliverTableWorker/Program.cs).
RabbitMqConfig rabbitConfig = new RabbitMqConfig
{
    Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
    User = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest",
};
builder.Services.AddSingleton(rabbitConfig);
builder.Services.AddSingleton<IMessagePublisher>(sp =>
    RabbitMqPublisher.CreateAsync(sp.GetRequiredService<RabbitMqConfig>()).GetAwaiter().GetResult());

builder.Services.AddQuartz(q =>
{
    JobKey jobKey = new JobKey(nameof(MonthlyCommissionStatementJob));
    q.AddJob<MonthlyCommissionStatementJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(t => t
        .ForJob(jobKey)
        .WithIdentity($"{nameof(MonthlyCommissionStatementJob)}-trigger")
        .WithCronSchedule("0 0 2 1 * ?", c =>
            c.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"))
             .WithMisfireHandlingInstructionFireAndProceed()));
});

builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

await builder.Build().RunAsync();
