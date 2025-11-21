using Microsoft.EntityFrameworkCore;
using MixMeet.Reservas.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuração de Autenticação JWT
var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"] ?? "chave_super_secreta_mixmeet_2025_backup";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

// --- CORREÇÃO CORS: Política Aberta para Desenvolvimento ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin() // Permite qualquer origem (Frontend)
            .AllowAnyMethod() // Permite GET, POST, PUT, DELETE, OPTIONS
            .AllowAnyHeader()); // Permite Authorization, Content-Type, etc.
});
// ------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Criação de Schema
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try {
        dbContext.Database.EnsureCreated(); 
    } catch (Exception ex) {
        Console.WriteLine($"Erro na inicialização do DB: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); 

// app.UseHttpsRedirection(); // Desativado em contêiner HTTP simples para evitar loops

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();