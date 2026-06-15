using Amazon.S3;
using DeliverTableInfrastructure.Configuration;
using DeliverTableInfrastructure.Data;
using DeliverTableInfrastructure.Messaging;
using DeliverTableInfrastructure.Repositories;
using DeliverTableInfrastructure.Repositories.Interfaces;
using DeliverTableInfrastructure.Services;
using DeliverTableInfrastructure.Services.Interfaces;
using DeliverTableWorker.Configuration;
using DeliverTableWorker.Consumers;
using DeliverTableWorker.Services;
using Microsoft.EntityFrameworkCore;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

DotNetEnv.Env.Load();
WorkerEnvironment env = WorkerEnvironment.Load();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(env);

// Database
builder.Services.AddDbContext<DeliverTableContext>(options =>
    options.UseNpgsql(env.ConnectionStringDatabase)
);

// RabbitMQ
RabbitMqConfig rabbitMqConfig = new RabbitMqConfig
{
    Host = env.RabbitMqHost,
    Port = env.RabbitMqPort,
    User = env.RabbitMqUser,
    Password = env.RabbitMqPassword,
};
builder.Services.AddSingleton(rabbitMqConfig);
builder.Services.AddSingleton<IMessagePublisher>(sp =>
    RabbitMqPublisher.CreateAsync(sp.GetRequiredService<RabbitMqConfig>()).GetAwaiter().GetResult()
);

// Object storage
ObjectStorageConfig osConfig = env.ObjectStorage;
builder.Services.AddSingleton(osConfig);
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    AmazonS3Config s3Config = new AmazonS3Config
    {
        ServiceURL = osConfig.ServiceUrl,
        ForcePathStyle = osConfig.ForcePathStyle,
        AuthenticationRegion = osConfig.Region,
    };
    return new AmazonS3Client(osConfig.AccessKey, osConfig.SecretKey, s3Config);
});
builder.Services.AddScoped<IObjectStorageService, ObjectStorageService>();

// Repositories
builder.Services.AddScoped<IEmailJobRepository, EmailJobRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<ICommissionStatementRepository, CommissionStatementRepository>();

// Worker services
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IEmailTemplateRenderer, RazorEmailTemplateRenderer>();
builder.Services.AddSingleton<IInvoicePdfRenderer, InvoicePdfRenderer>();
builder.Services.AddSingleton<ICommissionStatementPdfRenderer, CommissionStatementPdfRenderer>();

// Background services
builder.Services.AddHostedService<EmailJobConsumer>();
builder.Services.AddHostedService<InvoiceJobConsumer>();
builder.Services.AddHostedService<CommissionStatementJobConsumer>();
builder.Services.AddHostedService<JobSweepService>();

IHost host = builder.Build();
host.Run();
