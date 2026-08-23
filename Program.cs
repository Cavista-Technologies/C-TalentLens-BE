using System.Text;
using System.Text.Json.Serialization;
using C_TalentLens.Application;
using C_TalentLens.Application.Security;
using C_TalentLens.Domain;
using C_TalentLens.Infrastructure;
using C_TalentLens.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;

namespace C_TalentLens
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TalentLens API",
                    Version = "v1",
                    Description = "Backend API for requisition tracking, recruitment analytics, risk scoring, alerts, referrals, and leadership reporting."
                });

                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter a JWT bearer token from /api/auth/login. Example: Bearer eyJhbGciOi...",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document),
                        []
                    }
                });
            });
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddHealthChecks();
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
            builder.Services.AddCors(options =>
            {
                var allowedOrigins = builder.Configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [];

                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            builder.Services.AddDbContext<TalentLensDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("TalentLens")));

            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<TalentLensDbContext>()
                .AddDefaultTokenProviders();

            var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                ?? throw new InvalidOperationException("JWT configuration is required.");

            if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
            {
                throw new InvalidOperationException("JWT signing key must be at least 32 characters.");
            }

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RecruitmentWrite", policy =>
                    policy.RequireRole(UserRole.Recruiter, UserRole.TalentAcquisitionManager));
                options.AddPolicy("AlertsRead", policy =>
                    policy.RequireRole(UserRole.TalentAcquisitionManager, UserRole.Leadership));
            });

            builder.Services.AddSingleton<IClock, SystemClock>();
            builder.Services.AddSingleton<IRiskScoringEngine, RiskScoringEngine>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IRequisitionService, RequisitionService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IRiskService, RiskService>();
            builder.Services.AddScoped<IAlertService, AlertService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<ILeadershipSummaryService, LeadershipSummaryService>();
            builder.Services.AddScoped<IImportService, ImportService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TalentLens API v1");
                    options.RoutePrefix = "swagger";
                    options.DocumentTitle = "TalentLens API";
                });
            }
            else
            {
                app.UseHsts();
            }

            app.UseExceptionHandler();

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("Frontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health");

            await SeedDatabaseAsync(app);

            await app.RunAsync();
        }

        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            await DatabaseSeeder.SeedAsync(scope.ServiceProvider, CancellationToken.None);
        }
    }
}
