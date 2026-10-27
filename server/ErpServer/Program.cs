using ErpServer;
using ErpServer.Data;
using ErpServer.Endpoints;
using ErpServer.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.AddErpDb();
builder.AddErpCors();
builder.AddErpAuth();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHttpClient("SupabaseStorage");
builder.Services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapAuthEndpoints();
app.MapUsersEndpoints();
app.MapRolesEndpoints();
app.MapJobTitlesEndpoints();
app.MapTasksEndpoints();
app.MapCarsEndpoints();
app.MapStocksEndpoints();

// Migrate database and seed initial data

await app.MigrateAndSeedDbAsync();

app.Run();
