
using amorphie.core.security.Extensions;
using amorphie.token.data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

await builder.Configuration.AddVaultSecrets(builder.Configuration["DAPR_SECRET_STORE_NAME"], new string[] { "ServiceConnections" });

var tt = builder.Configuration["DatabaseConnection"];

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
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IClientService, ClientServiceLocal>();
    builder.Services.AddScoped<IUserService, UserServiceLocal>();
    builder.Services.AddScoped<ITagService, TagServiceLocal>();

    builder.Services.AddHttpClient("Client", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["ClientBaseAddress"]);
    });
    builder.Services.AddHttpClient("User", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["UserBaseAddress"]);
    });
    builder.Services.AddHttpClient("Tag", httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["TagBaseAddress"]);
    });
}
else
{
    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ITagService, TagService>();


}

var app = builder.Build();

//Db Migrate
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
db.Database.Migrate();

app.MapCheckGrantTypesControlEndpoints();
app.MapValidateClientControlEndpoints();
app.MapCheckUserControlEndpoints();
app.MapCheckScopesControlEndpoints();
app.MapGenerateTokensControlEndpoints();
app.MapCheckUserStateControlEndpoints();
app.MapLoginOtpFlowControlEndpoints();
app.MapSetStateControlEndpoints();
app.MapDaprTestControlEndpoints();
app.MapCheckOtpControlEndpoints();
app.MapCheckPushControlEndpoints();
app.MapSetLoginTypeControlEndpoints();
app.MapLoginPushFlowControlEndpoints();

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
