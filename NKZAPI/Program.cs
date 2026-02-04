using Microsoft.EntityFrameworkCore;
using NKZAPI.Controllers;
using NKZAPI.Repositories;
using NKZAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

string? connectionString = builder.Configuration.GetConnectionString("ConnectionString");

if (connectionString is null)
{
    throw new InvalidOperationException("Connection string 'ConnectionString' not found.");
}
builder.Services.AddDbContext<NKZAPI.Data.NKZAPIContext>(options =>
    options.UseNpgsql(connectionString));



builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserServices>();

// Registre as implementações das interfaces de autenticação
builder.Services.AddScoped<NKZAPI.Services.AuthServices.IAuthInterface, NKZAPI.Services.AuthServices.AuthService>();
builder.Services.AddScoped<NKZAPI.Services..IPassInterface, NKZAPI.Services.AuthServices.PassService>();



builder.Services.AddOpenApi();

var app = builder.Build();