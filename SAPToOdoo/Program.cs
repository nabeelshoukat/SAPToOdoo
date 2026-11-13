using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SAPToOdoo.Application.Interfaces;
using SAPToOdoo.Common;
using SAPToOdoo.Infrastructure.Sap;
using SAPToOdoo.Middleware;

var builder = WebApplication.CreateBuilder(args);

// No-ops unless launched by the Windows Service Control Manager, so `dotnet run`
// and a console-mode exe both keep working; lets the published exe be registered
// as a persistent Windows Service on the client's server.
builder.Host.UseWindowsService(options => options.ServiceName = "SAPToOdoo Gateway");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SAP-Odoo Integration Gateway", Version = "v1" });

    var apiKeyScheme = new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API key required for all endpoints except /health and /swagger",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
    };
    options.AddSecurityDefinition("ApiKey", apiKeyScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { apiKeyScheme, Array.Empty<string>() } });
});

builder.Services.Configure<SapOptions>(builder.Configuration.GetSection("Sap"));
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"));

builder.Services.AddSingleton<ISapConnectionService, SapConnectionService>();
builder.Services.AddScoped<ISapBusinessPartnerService, SapBusinessPartnerService>();
builder.Services.AddScoped<ISapItemService, SapItemService>();
builder.Services.AddScoped<ISapWarehouseService, SapWarehouseService>();
builder.Services.AddScoped<ISapTaxService, SapTaxService>();
builder.Services.AddScoped<ISapTableExplorerService,SapTableExplorerService>();
builder.Services.AddScoped<ISapDiagnosticsService, SapDiagnosticsService>();
builder.Services.AddScoped<ISapInvoiceService, SapInvoiceService>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var correlationId = context.HttpContext.GetCorrelationId();
        var message = string.Join(" ", context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => error.ErrorMessage)));

        var response = ApiResponse<object>.Fail(
            new ApiError { Code = ErrorCodes.ValidationError, Message = message },
            correlationId);

        return new BadRequestObjectResult(response);
    };
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
