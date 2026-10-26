using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services;
using ReReviewer.Data;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Interfaces;
using ReReviewer.Middleware;
using ReReviewer.Security;


namespace ReReviewer
{
    public class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddMvc();
            services.AddCors(options =>
            {
                options.AddPolicy("_origins",
                    policy =>
                    {
                        //Local server and front-end
                        policy.WithOrigins("http://localhost:5173",
                            "https://localhost:7107").AllowAnyHeader().AllowAnyMethod();
                    });
            });

            GetConfigs(services);
            AddServices(services);
            ConfigureDatabase(services);
            
            // Обязательность полей проверяют AppService'ы (ValidationException → 400). Неявный [Required]
            // для не-nullable строк отключён: иначе, например, жанр во вложенном списке игры без Title,
            // где нужен только Id, отклонялся бы ещё до сервиса.
            services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
            // api/auth/login, а не api/Auth/login: и в Swagger, и в ссылках из CreatedAtAction.
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo());
                // Кнопка Authorize в Swagger UI: токен вставляется без префикса, "Bearer " Swagger добавит сам.
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT from POST /api/auth/login. Paste the token only, Swagger adds the Bearer prefix.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            ConfigureAuthentication(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Первым звеном, чтобы ловить исключения из всех следующих, включая контроллеры.
            // Заменяет и страницу ошибок для разработчика: в Development подробности уходят в ответ.
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // CORS — между маршрутизацией и авторизацией, как требует ASP.NET Core.
            app.UseRouting();
            app.UseCors("_origins");
            app.UseAuthentication();
            app.UseAuthorization();

            // Маршруты задаются атрибутами контроллеров (см. BaseController).
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private static void AddServices(IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IReviewerService, ReviewerService>();
            services.AddScoped<IOutletService, OutletService>();
            services.AddScoped<IBadgeService, BadgeService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IMaterialService, MaterialService>();
            services.AddScoped<ICriterionService, CriterionService>();
            services.AddScoped<IReviewService, ReviewService>();

            services.AddScoped<IUserAppService, UserAppService>();
            services.AddScoped<IRoleAppService, RoleAppService>();
            services.AddScoped<IReviewerAppService, ReviewerAppService>();
            services.AddScoped<IOutletAppService, OutletAppService>();
            services.AddScoped<IBadgeAppService, BadgeAppService>();
            services.AddScoped<IGenreAppService, GenreAppService>();
            services.AddScoped<IGameAppService, GameAppService>();
            services.AddScoped<IMaterialAppService, MaterialAppService>();
            services.AddScoped<ICriterionAppService, CriterionAppService>();
            services.AddScoped<IReviewAppService, ReviewAppService>();
            services.AddScoped<IAuthAppService, AuthAppService>();

            services.AddSingleton<IJwtTokenService, JwtTokenService>();
        }

        // Без корректных настроек JWT сервер не стартует, а не падает на первом входе.
        private void GetConfigs(IServiceCollection services) =>
            services.AddOptions<JwtOptions>()
                .Bind(Configuration.GetSection(JwtOptions.SectionName))
                .Validate(jwt => !string.IsNullOrWhiteSpace(jwt.Issuer) && !string.IsNullOrWhiteSpace(jwt.Audience),
                    "Jwt:Issuer and Jwt:Audience must be set.")
                .Validate(jwt => Encoding.UTF8.GetByteCount(jwt.SigningKey) >= JwtOptions.MinSigningKeyBytes,
                    $"Jwt:SigningKey must be at least {JwtOptions.MinSigningKeyBytes} bytes. "
                    + "Set it with dotnet user-secrets or the Jwt__SigningKey environment variable.")
                .Validate(jwt => jwt.LifetimeMinutes > 0, "Jwt:LifetimeMinutes must be positive.")
                .ValidateOnStart();

        private static void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            // Параметры проверки берутся из JwtOptions при первом запросе, а не при сборке сервисов,
            // поэтому dotnet ef и другие инструменты, собирающие хост без ключа, не падают.
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;

                    // Claims не переименовываются: sub, name и role остаются как в токене (см. JwtTokenService).
                    bearer.MapInboundClaims = false;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = jwt.CreateSigningKey(),
                        NameClaimType = JwtRegisteredClaimNames.Name,
                        RoleClaimType = "role"
                    };
                });
        }

        private void ConfigureDatabase(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("ReReviewerDB");
            services.AddDbContext<ReReviewerDbContext>(options => options
                .UseLazyLoadingProxies()
                .UseSqlServer(connectionString));
        }
    }
}