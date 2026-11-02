using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FcgCatalogApi.Data;
using FcgCatalogApi.Repositories;
using FcgCatalogApi.Services;
using FcgCatalogApi.Middlewares;
using FcgCatalogApi.Events;

var builder = WebApplication.CreateBuilder(args);

// Configuração do DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Para desenvolvimento, usando InMemory. Em produção, usar SQL Server ou PostgreSQL
    options.UseInMemoryDatabase("FcgCatalogDb");
    
    // Para SQL Server, descomentar:
    // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Configuração JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Em produção, usar true
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("Usuario", "Administrador"));
});

// Injeção de dependências - Repositories
builder.Services.AddScoped<IJogoRepository, JogoRepository>();
builder.Services.AddScoped<IPromocaoRepository, PromocaoRepository>();
builder.Services.AddScoped<IBibliotecaRepository, BibliotecaRepository>();

// Injeção de dependências - Services
builder.Services.AddScoped<JogoService>();
builder.Services.AddScoped<PromocaoService>();
builder.Services.AddScoped<BibliotecaService>();

// Configuração de mensageria (RabbitMQ)
builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();

// Consumer de eventos de pagamento (Background Service)
builder.Services.AddHostedService<PaymentEventConsumer>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMQHealthCheck>("rabbitmq");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FCG Catalog API",
        Version = "v1",
        Description = "API de Catálogo de Jogos - Microsserviço com gerenciamento de jogos e promoções"
    });

    // Configuração para JWT no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. \r\n\r\n" +
                      "Digite 'Bearer' [espaço] e então seu token no campo abaixo.\r\n\r\n" +
                      "Exemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware de tratamento de exceções
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Redirecionar raiz para Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Health Check endpoint
app.MapHealthChecks("/health");

app.MapControllers();
app.Run();
