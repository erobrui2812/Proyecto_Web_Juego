using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.SignalR;
using hundir_la_flota.Repositories;
using hundir_la_flota.Hubs;

namespace hundir_la_flota
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            string dbPath = Path.Combine(AppContext.BaseDirectory, "hundir_la_flota.db");
            string connectionString = $"Data Source={dbPath};";

            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddSingleton<AuthService>(new AuthService(builder.Configuration["JWT_KEY"]));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();


            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<GameSimulation>();

            // Configuración de autenticación con JWT
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
                            // Permite pasar el token en las conexiones SignalR
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


            // Configuración de Swagger
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

            // Configuración de CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", builder =>
                {
                    builder.WithOrigins("http://localhost:3000") // Especifica el origen permitido
                           .AllowAnyHeader()                   // Permite cualquier encabezado
                           .AllowAnyMethod()                   // Permite cualquier método HTTP
                           .AllowCredentials();                // Permite credenciales (cookies o tokens)
                });
            });




            builder.Services.AddSignalR();

            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                dbContext.Database.EnsureCreated();
            }


            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();


            app.UseCors("AllowReactApp");


            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Mapear hubs de SignalR
            app.MapHub<StatusHub>("/statusHub");
            app.MapHub<NotificationHub>("/notificationHub");


            using (var scope = app.Services.CreateScope())
            {
                var simulation = scope.ServiceProvider.GetRequiredService<GameSimulation>();
                // await simulation.RunSimulationAsync();  // Ejecutar la simulación
            }

            app.Run();
        }
    }
}
