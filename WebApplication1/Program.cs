using Logica.Models;
using Logica.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuraci�n de CORS para permitir cualquier origen
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Configuraci�n de la conexi�n a la base de datos Oracle
builder.Services.AddDbContext<AdminContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDbConnection")));

// Registra el servicio ControlTablespaces
builder.Services.AddScoped<ControlTablespaces>();

// Registra el servicio ControlSeguridadUsuarios
builder.Services.AddScoped<ControlSeguridadUsuarios>();

// Registra el servicio ControlRespaldos
builder.Services.AddScoped<ControlRespaldos>();

// Registra el servicio FileService
builder.Services.AddScoped<FileService>();

// Registra el servicio ControlTunning
builder.Services.AddScoped<ControlTunning>();

// Registra el servicio ControlTunning
builder.Services.AddScoped<ControlAuditoria>();

// Registra el servicio ControlPerformance
builder.Services.AddScoped<ControlPerformance>();

// Agregar servicios al contenedor.
builder.Services.AddControllers();

// Configuraci�n de Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();


// Configuraci�n del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Puedes habilitar Swagger tambi�n en producci�n si lo necesitas
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilitar CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
