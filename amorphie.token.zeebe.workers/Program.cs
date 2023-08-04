
using amorphie.core.security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
await builder.Configuration.AddVaultSecrets(builder.Configuration["DAPR_SECRET_STORE_NAME"],new string[]{"ServiceConnections"});

builder.Services.AddControllers();
builder.Services.AddScoped<IAuthorizationService,AuthorizationService>();
builder.Services.AddDaprClient();
builder.Services.AddHttpContextAccessor();

if(builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IClientService,ClientServiceLocal>();
    builder.Services.AddScoped<IUserService,UserServiceLocal>();
    builder.Services.AddScoped<ITagService,TagServiceLocal>();

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
    builder.Services.AddScoped<IClientService,ClientService>();
    builder.Services.AddScoped<IUserService,UserService>();
    builder.Services.AddScoped<ITagService,TagService>();

    
}
var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
