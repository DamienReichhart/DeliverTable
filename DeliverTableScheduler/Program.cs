using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Payments;
using DeliverTableInfrastructure.Repositories;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableScheduler.Configuration;
using DeliverTableScheduler.Jobs;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();
var env = SchedulerEnvironment.Load();

var builder = Host.CreateApplicationBuilder(args);
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

await builder.Build().RunAsync();
