using ContactManager.Data;
using ContactManager.Hubs;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using ContactManagerStarter;
using Microsoft.Extensions.Logging;
using ContactManager.Controllers;
using Org.BouncyCastle.Security;

var builder = WebApplication.CreateBuilder(args);

//To allow the use of Ilogger interface
var logFactory = new LoggerFactory();
var logger = logFactory.CreateLogger<Type>();

// Add services to the container.
builder.Services.AddRazorPages()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        logger.LogInformation("adding services to container");
    });

builder.Services.AddDbContext<ApplicationContext>(options =>
                options
                    .UseInMemoryDatabase(databaseName: "ContactManagerDb"));

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    logger.LogError("Connection error");
    app.UseDeveloperExceptionPage();
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    logger.LogInformation("Intializing server");
    var dataContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

    dataContext.Database.EnsureCreated();

    Seeder.Initialize(dataContext);
}
try
{
    logger.LogInformation("starting routes");
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();
}
catch(Exception ex)
{
    logger.LogCritical(ex, "failed to started routes");
}


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ContactHub>("/contacthub");

try
{
    logger.LogInformation("Starting web host");
    app.Run();
}
catch(Exception ex)
{
    logger.LogCritical(ex, "Starting we host failed");
}

