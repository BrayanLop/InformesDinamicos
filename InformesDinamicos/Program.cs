using InformesDinamicos.Data;
using InformesDinamicos.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("MongoDB");
    return new MongoClient(connectionString);
});
builder.Services.AddSingleton<ShardingService>();
builder.Services.AddScoped<RabbitConsumerService>();
builder.Services.AddScoped<ConsolidacionService>();
builder.Services.AddScoped<DatabaseService>();

// RabbitMQ Service - Solo para consumo manual
builder.Services.AddSingleton<RabbitListener>();
// builder.Services.AddHostedService<RabbitListener>(provider => provider.GetService<RabbitListener>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Nuevo}/{id?}");

app.MapStaticAssets();

app.Run();
