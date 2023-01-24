using CommunicationAppApi.Data;
using CommunicationAppApi.Helpers;
using CommunicationAppApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Azure.WebJobs;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

builder.Services.AddDbContext<DataContext>(x => x.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .ConfigureWarnings(warnings => warnings.Ignore())
            );
IdentityBuilder identityBuilder = services.AddIdentityCore<User>(opt => {
    opt.Password.RequireDigit = false;
    opt.Password.RequiredLength = 4;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireUppercase = false;
});

identityBuilder = new IdentityBuilder(identityBuilder.UserType, typeof(Role), builder.Services);

identityBuilder.AddEntityFrameworkStores<DataContext>();
identityBuilder.AddRoleValidator<RoleValidator<Role>>();
identityBuilder.AddRoleManager<RoleManager<Role>>();
identityBuilder.AddSignInManager<SignInManager<User>>();


services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(Options =>
{
    Options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration.GetSection("AppSettings:Token").Value)),
        ValidateIssuer = false,
        ValidateAudience = false

    };
});

builder.Services.AddAuthorization(
    options => {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
        options.AddPolicy("ModeratePhotoRole", policy => policy.RequireRole("Admin", "Moderator"));
        options.AddPolicy("VipOnly", policy => policy.RequireRole("VIP"));
    }
);

builder.Services.AddMvc(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    options.EnableEndpointRouting = false;
});

builder.Services.AddControllers().AddNewtonsoftJson(
    option =>
    {
        option.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

    }
);

//builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
builder.Services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
builder.Services.AddAutoMapper(typeof(CommunicationRepository).Assembly);
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ICommunicationRepository, CommunicationRepository>();
builder.Services.AddScoped<LogUserActivity>();


var app = builder.Build();

//Seeding Data
using var scope = app.Services.CreateScope();

var servicesProvider = scope.ServiceProvider;
var loggerFactory = servicesProvider.GetRequiredService<ILoggerProvider>();
var logger = loggerFactory.CreateLogger("app");
try
{
    var userManager = servicesProvider.GetRequiredService<UserManager<User>>();
    var roleManager = servicesProvider.GetRequiredService<RoleManager<Role>>();

    TrialData.TrialUsers(userManager, roleManager);

    logger.LogInformation("Data Seeded");
    logger.LogInformation("Application Started");
}
catch (Exception ex)
{
    logger.LogWarning(ex, "An Error Occurred While Seeding Data");
}

// Configure the HTTP request pipeline.

StripeConfiguration.SetApiKey(configuration.GetSection("Stripe:SecretKey").Value);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler(BuilderExtensions =>
    {
        BuilderExtensions.Run(async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var error = context.Features.Get<IExceptionHandlerFeature>();
            if (error != null)
            {
                context.Response.AddApplicationError(error.Error.Message);
                await context.Response.WriteAsync(error.Error.Message);
            }
        });
    });
}


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(x => x.SetIsOriginAllowed(Options => _ = true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());

app.UseEndpoints(routes => {
    routes.MapHub<ChatHub>("/chat");
});



app.UseDefaultFiles();

app.Use(async (context, next) => {
    await next();
    if (context.Response.StatusCode == 404)
    {
        context.Request.Path = "/index.html";
        await next();
    }

});

app.UseStaticFiles();
app.UseMvc();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
