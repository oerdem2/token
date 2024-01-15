
using System.Dynamic;
using System.Text.Json;
using amorphie.core.security.Extensions;
using amorphie.token.data;
using amorphie.token.Middlewares;
using amorphie.token.Modules.Login;
using amorphie.token.Modules.OpenBankingFlows;
using amorphie.token.Modules.ZeebeJobs;
using amorphie.token.Services.ClaimHandler;
using amorphie.token.Services.Consent;
using amorphie.token.Services.FlowHandler;
using amorphie.token.Services.InternetBanking;
using amorphie.token.Services.MessagingGateway;
using amorphie.token.Services.Profile;
using amorphie.token.Services.TransactionHandler;
using Dapr.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

await builder.Configuration.AddVaultSecrets(builder.Configuration["DAPR_SECRET_STORE_NAME"], new string[] { "ServiceConnections" });

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        builder =>
        {
            builder.WithOrigins("*")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
builder.Services.AddHealthChecks();
builder.Services.AddControllersWithViews();
builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.Cookie.Name = ".amorphie.token";
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DatabaseContext>
    (options => options.UseNpgsql(builder.Configuration["DatabaseConnection"], b => b.MigrationsAssembly("amorphie.token.data")));
builder.Services.AddDbContext<IbDatabaseContext>(options => options.UseSqlServer(builder.Configuration["IbDatabaseConnection"]));
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IClientService, ClientServiceLocal>();
    builder.Services.AddScoped<IUserService, UserServiceLocal>();
    builder.Services.AddScoped<ITagService, TagServiceLocal>();
    builder.Services.AddScoped<IConsentService, ConsentServiceLocal>();

    builder.Services.AddHttpClient("Client", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["ClientBaseAddress"]!);
    });
    builder.Services.AddHttpClient("User", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["UserBaseAddress"]!);
    });
    builder.Services.AddHttpClient("Tag", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["TagBaseAddress"]!);
    });
    builder.Services.AddHttpClient("Consent", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["ConsentBaseAddress"]!);
    });
}
else
{
    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<IConsentService, ConsentService>();
}

builder.Services.AddScoped<IInternetBankingUserService, InternetBankingUserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IFlowHandler, FlowHandler>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IClaimHandlerService, ClaimHandlerService>();

builder.Services.AddRefitClient<IProfile>()
.ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.Configuration["ProfileBaseAddress"]!))
.ConfigurePrimaryHttpMessageHandler(() => { return new HttpClientHandler() { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; } }; });

builder.Services.AddRefitClient<ISimpleProfile>()
.ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.Configuration["SimpleProfileBaseAddress"]!))
.ConfigurePrimaryHttpMessageHandler(() => { return new HttpClientHandler() { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; } }; });


builder.Services.AddRefitClient<IMessagingGateway>()
.ConfigureHttpClient(c => c.BaseAddress = new Uri(builder.Configuration["MessagingGatewayBaseAddress"]!));
var app = builder.Build();
app.UseTransactionMiddleware();


//Db Migrate
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
db.Database.Migrate();

app.MapHealthChecks("/health");

app.MapLoginWorkflowEndpoints();

app.MapCheckGrantTypesControlEndpoints();
app.MapValidateClientControlEndpoints();
app.MapCheckUserControlEndpoints();
app.MapCheckScopesControlEndpoints();
app.MapGenerateTokensControlEndpoints();
app.MapCheckUserStateControlEndpoints();
app.MapLoginOtpFlowControlEndpoints();
app.MapDaprTestControlEndpoints();
app.MapCheckOtpControlEndpoints();
app.MapCheckPushControlEndpoints();
app.MapSetLoginTypeControlEndpoints();
app.MapLoginPushFlowControlEndpoints();

app.MapTokenLoginCheckDevice();
app.MapTokenLoginCheckSecondFactor();
app.MapTokenLoginCheckUser();
app.MapTokenLoginSendOtp();
app.MapTokenLoginSetTransaction();

app.MapAmorphieOauthCheckClientEndpoint();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseCors();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
