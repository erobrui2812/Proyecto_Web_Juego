using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using hundir_la_flota.Repositories;
using hundir_la_flota.Services;
using hundir_la_flota.Models.Seeder;

namespace hundir_la_flota
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

            var builder = WebApplication.CreateBuilder(args);


            string dbPath = Path.Combine(AppContext.BaseDirectory, "hundir_la_flota.db");
            string connectionString = $"Data Source={dbPath};";


            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();


            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlite(connectionString));


            builder.Services.AddSingleton<IAuthService>(sp =>
                new AuthService(
                    builder.Configuration["JWT_KEY"] ?? throw new InvalidOperationException("JWT_KEY no configurada"),
                    sp.GetRequiredService<ILogger<AuthService>>()
                )
            );

            builder.Services.AddSingleton<IWebSocketService, WebSocketService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WebSocketService>>();
                var serviceProvider = sp;
                return new WebSocketService(logger, serviceProvider);
            });


            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<IFriendshipService, FriendshipService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<IStatsService, StatsService>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddScoped<IGameRepository, GameRepository>();
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IGameParticipantRepository, GameParticipantRepository>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IChatService, ChatService>();
            builder.Services.AddScoped<IBotService, BotService>();




            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                            builder.Configuration["JWT_KEY"] ?? throw new InvalidOperationException("JWT_KEY no configurada")
                        )),
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


            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

                bool created = dbContext.Database.EnsureCreated();

                if (!dbContext.Users.Any())
                {
                    var seeder = new SeederUsers(dbContext, authService);
                    await seeder.Seeder();
                }
            }



            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseWebSockets();
            app.UseMiddleware<WebSocketMiddleware>();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();


            app.Run();
        }
    }
}
