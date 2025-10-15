using InformesDinamicos.Data;
using InformesDinamicos.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// MongoDB
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<ShardingService>();
builder.Services.AddScoped<RabbitConsumerService>();

// RabbitMQ Service
builder.Services.AddHostedService<RabbitListener>();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapStaticAssets();

app.Run();
