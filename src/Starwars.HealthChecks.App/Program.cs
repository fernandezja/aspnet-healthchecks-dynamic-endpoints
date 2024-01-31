using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Starwars.HealthChecks.App;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);


var healthChecksUIConnectionString = "Persist Security Info=True;Initial Catalog=StarwarsHealthChecksUI;Data Source=.; Application Name=App; Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";


builder.Services.AddHealthChecksUI(setup =>
    {
        // Set the maximum history entries by endpoint that will be served by the UI api middleware
        setup.MaximumHistoryEntriesPerEndpoint(100);

    })    
    .AddInMemoryStorage();
    //.AddInMemoryStorage(databaseName: "StarwarsHealthChecksUI");
    //.AddSqlServerStorage(healthChecksUIConnectionString);

var healthChecksBuilder = builder.Services.AddHealthChecks();

//Only demo 
healthChecksBuilder.AddCheck<RandomHealthCheck>("random");

builder.Services.AddSingleton<IHealthChecksBuilder>(healthChecksBuilder);

//Add 
AddServicesToCheck(healthChecksBuilder);

builder.Services.AddControllers();
builder.Services.AddRazorPages();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.UseAuthorization();

app.MapRazorPages();

app.UseEndpoints(endpoints =>
   {

       endpoints.MapHealthChecks("/healthz", new HealthCheckOptions()
       {
           Predicate = _ => true,
           ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
       });


       endpoints.MapHealthChecksUI(setup =>
       {
           setup.UIPath = "/"; // this is ui path in your browser
           //setup.ApiPath = "/health-ui-api"; // the UI ( spa app )  use this path to get information from the store ( this is NOT the healthz path, is internal ui api )
           setup.PageTitle = "Starwars Health Checks UI"; // the page title in <head>
       });

       endpoints.MapDefaultControllerRoute();
   });

//

app.Run();



void AddServicesToCheck(IHealthChecksBuilder healthChecksBuilder)
{
    var tagsWeb = new[] { "Web" };
    var tagsApi = new[] { "API" };

    healthChecksBuilder.AddUrlGroup(
                       new Uri("https://www.starwars.com/"),
                       name: "Starwars Website",
                       tags: tagsWeb);

    healthChecksBuilder.AddUrlGroup(
                       new Uri("https://swapi.dev/"),
                       name: "Starwars API Home",
                       tags: tagsApi);

    healthChecksBuilder.AddUrlGroup(
                      new Uri("https://swapi.dev/api"),
                      name: "Starwars API",
                      tags: tagsApi);
}