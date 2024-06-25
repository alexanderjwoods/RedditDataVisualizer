using Microsoft.Extensions.Caching.Memory;
using SDV.Data;
using SDV.Data.Interfaces;
using SDV.Data.Reddit;
using SDV.Data.Reddit.Interfaces;
using SDV.Data.Reddit.Models;
using SDV.Data.Services.Interfaces;
using SDV.Data.Services.Services;
using SDV.ViewModels;

namespace SDV
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<WeatherForecastService>();
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache();
            builder.Services.AddLogging(builder =>
            {
                builder.AddFilter("System", LogLevel.Warning)
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("SDV", LogLevel.Information)
                    .AddConsole();
            });

            var dataStore = new LocalDataStore(builder.Services.BuildServiceProvider().GetService<ILogger<LocalDataStore>>());
            builder.Services.AddSingleton<IDataStore>(dataStore);

            var redditApiClient = new RedditApiClient(
                builder.Services.BuildServiceProvider().GetService<IHttpClientFactory>(),
                builder.Services.BuildServiceProvider().GetService<ILogger<RedditApiClient>>(),
                builder.Services.BuildServiceProvider().GetService<IConfiguration>());

            builder.Services.AddSingleton<IRedditApiClient>(redditApiClient);

            var redditDataService = new RedditDataService(
                builder.Configuration,
                builder.Services.BuildServiceProvider().GetService<IDataStore>(),
                builder.Services.BuildServiceProvider().GetService<IRedditApiClient>(),
                builder.Services.BuildServiceProvider().GetService<IMemoryCache>());
            builder.Services.AddSingleton<IDataService>(redditDataService);

            builder.Services.AddSingleton(new IndexViewModel(builder.Services.BuildServiceProvider().GetService<IDataService>()));


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

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}