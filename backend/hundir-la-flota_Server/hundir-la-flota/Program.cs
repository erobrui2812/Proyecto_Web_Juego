using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using hundir_la_flota.Websocket;
using Microsoft.AspNetCore.SignalR;
using hundir_la_flota.Repositories;

namespace hundir_la_flota
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuraci�n de la base de datos
            string dbPath = Path.Combine(AppContext.BaseDirectory, "hundir_la_flota.db");
            string connectionString = $"Data Source={dbPath};";

            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddSingleton<AuthService>(new AuthService(builder.Configuration["JWT_KEY"]));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Servicios personalizados
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<GameSimulation>();

            // Configuraci�n de autenticaci�n con JWT
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("6457984657981246597895234124615498")),
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Configuraci�n de Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hundir la Flota API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Introduce el token JWT"
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
                        new string[] {}
                    }
                });
            });

            // Configuraci�n de CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Inicializaci�n de la base de datos
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                dbContext.Database.EnsureCreated();
            }

            // Configuraci�n del entorno de desarrollo
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Middlewares
            app.UseHttpsRedirection();

            // Middleware para depuraci�n de solicitudes
            app.Use(async (context, next) =>
            {
                Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
                await next();
                Console.WriteLine($"Response: {context.Response.StatusCode}");
            });


            // Middleware de login
            app.Use(async (context, next) =>
            {
                Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
                Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
                await next();
                Console.WriteLine($"Response: {context.Response.StatusCode}");
            });


            app.UseRouting();

            // Middleware de CORS
            app.UseCors("AllowReactApp");

            // Middlewares de autenticaci�n y autorizaci�n
            app.UseAuthentication();
            app.UseAuthorization();

            // Middleware de WebSocket
            app.UseWebSockets();
            app.UseMiddleware<WebSocketMiddleware>();

            // Mapear controladores
            app.MapControllers();

            // Ejecutar simulaci�n del juego
            using (var scope = app.Services.CreateScope())
            {
                var simulation = scope.ServiceProvider.GetRequiredService<GameSimulation>();
                await simulation.RunSimulationAsync(); // Ejecutar la simulaci�n
            }

            app.Run();
        }
    }
}
