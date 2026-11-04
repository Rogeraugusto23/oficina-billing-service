using Amazon.DynamoDBv2;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using BillingService.Application.Common;
using BillingService.Application.UseCases;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Messaging;
using BillingService.Infrastructure.Payments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .Enrich.FromLogContext()
              .WriteTo.Console(new CompactJsonFormatter()));

    // 1. DynamoDB (banco próprio do Billing Service)
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddScoped<IOrcamentoRepository, DynamoDbOrcamentoRepository>();

    // 2. Mensageria — publica (SNS) e consome (SQS) eventos de integração
    builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
    builder.Services.AddAWSService<IAmazonSQS>();
    builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();
    builder.Services.AddHostedService<SqsEventConsumerBackgroundService>();

    // 3. Mercado Pago (sandbox)
    builder.Services.AddHttpClient<IMercadoPagoGateway, MercadoPagoGateway>();

    // 4. Use Cases
    builder.Services.AddScoped<CriarOrcamentoUseCase>();
    builder.Services.AddScoped<ConsultarOrcamentoUseCase>();
    builder.Services.AddScoped<RegistrarPagamentoUseCase>();
    builder.Services.AddScoped<CancelarOrcamentoUseCase>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Billing Service - Oficina Wagyu (Fase 4)", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\""
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    });

    // Mesmo segredo JWT compartilhado com OS Service / Lambda de auth (Fase 3)
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ChaveSecretaOficinaWagyu2026_MuitoLonga";
    var key = Encoding.ASCII.GetBytes(jwtSecret);
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "billing-service", timestamp = DateTime.UtcNow }))
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Billing Service terminou de forma inesperada durante a inicialização");
}
finally
{
    Log.CloseAndFlush();
}
