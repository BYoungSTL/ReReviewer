using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ReReviewer.Application.Interfaces;
using ReReviewer.Application.Services;
using ReReviewer.Data;
using ReReviewer.Data.CRUD;
using ReReviewer.Data.Interfaces;


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
            
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo());
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                // c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                // {
                //     {
                //         new OpenApiSecurityScheme
                //         {
                //             Reference = new OpenApiReference
                //             {
                //                 Type = ReferenceType.SecurityScheme,
                //                 Id = "Bearer"
                //             },
                //             Scheme = "oauth2",
                //             Name = "Bearer",
                //             In = ParameterLocation.Header,
                //
                //         },
                //         new List<string>()
                //     }
                // });
            });

            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // ValidateIssuer = true,
                        // ValidIssuer = AuthOptions.ISSUER,
                        // ValidateAudience = true,
                        // ValidAudience = AuthOptions.AUDIENCE,
                        // ValidateLifetime = true,
                        // IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
                        // ValidateIssuerSigningKey = true,
                    };
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseCors("_origins");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "api/{controller=Home}");
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
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
        }

        private void GetConfigs(IServiceCollection services)
        {
            
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