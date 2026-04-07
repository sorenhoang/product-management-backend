using ProductManagement.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// [SERVICES: Serilog]

// [SERVICES: Controllers]
builder.Services.AddControllers();

// [SERVICES: Scalar/Swagger]
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// [SERVICES: Database — EF Core + PostgreSQL]
builder.Services.AddInfrastructure(builder.Configuration);

// [SERVICES: Redis]

// [SERVICES: FluentValidation]

// [SERVICES: Mapster]

// [SERVICES: Application services / repositories]

var app = builder.Build();

// [MIDDLEWARE: Exception handler]

// [MIDDLEWARE: Scalar UI]
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
    app.MapScalarApiReference();
}

// [MIDDLEWARE: HTTPS + Auth placeholder]
app.UseHttpsRedirection();
app.UseAuthorization();

// [ENDPOINTS]
app.MapControllers();

app.Run();
