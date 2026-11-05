using System.Text;
using FcgCatalogApi.Data;
using FcgCatalogApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG Catalog API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseInMemoryDatabase("CatalogDb"));

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-super-secret-key-change-this-in-production-min-32-chars";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
builder.Services.AddHostedService<PaymentEventConsumer>();

var app = builder.Build();

// Seed some games
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    if (!db.Games.Any())
    {
        db.Games.AddRange(
            new FcgCatalogApi.Models.Game
            {
                Id = Guid.NewGuid(),
                Titulo = "The Legend of Zelda: Breath of the Wild",
                Descricao = "Aventura épica em mundo aberto",
                Genero = "Aventura",
                Desenvolvedora = "Nintendo",
                DataLancamento = new DateTime(2017, 3, 3),
                Preco = 299.90m,
                Status = "Publicado",
                CriadoEm = DateTime.UtcNow
            },
            new FcgCatalogApi.Models.Game
            {
                Id = Guid.NewGuid(),
                Titulo = "Red Dead Redemption 2",
                Descricao = "Faroeste de ação e aventura",
                Genero = "Ação",
                Desenvolvedora = "Rockstar Games",
                DataLancamento = new DateTime(2018, 10, 26),
                Preco = 249.90m,
                Status = "Publicado",
                CriadoEm = DateTime.UtcNow
            }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
