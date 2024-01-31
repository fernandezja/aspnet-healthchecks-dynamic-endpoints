using HealthChecks.UI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HealthChecks.UI.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.Uris;
using System.Threading;
using System.Net.Http;
using System;
using System.Text;


namespace Starwars.HealthChecks.App.Pages
{
    /// <summary>
    /// https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/blob/master/build/docker-images/HealthChecks.UI.Image/PushService/HealthChecksPushService.cs
    /// </summary>
    public class ManagerModel : PageModel
    {
        private readonly ILogger<ManagerModel> _logger;

        private readonly HealthChecksDb _db;
        private readonly IOptions<Settings> _settings;
        private IOptions<HealthCheckServiceOptions> _options;
        private IHttpClientFactory _httpClientFactory;


        public ManagerModel(HealthChecksDb db,
                            IOptions<Settings> settings,
                            IOptions<HealthCheckServiceOptions> options,
                            IHttpClientFactory httpClientFactory,
                            ILogger<ManagerModel> logger)
        {
            _db = db;
            _settings = settings;
            _options = options;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public void OnPostAddUrlRandom()
        {
            var tagsWebAndNew = new[] { "Web", "New" };

            var stringRandom = GenerateRandomString(8);

            var nameRandom = $"demo-{stringRandom}";
            var urlRandom = $"https://swapi.dev/api?__r={nameRandom}";

            var registration = new HealthCheckRegistration(
                name: nameRandom,
                sp =>
                {
                    var uri = new Uri(urlRandom);

                    var options = new UriHealthCheckOptions()
                                     .AddUri(uri);

                    options.AddUri(uri);

                    var uriHealthCheck = new UriHealthCheck(
                        options,
                        () => _httpClientFactory.CreateClient(nameRandom));

                    return uriHealthCheck;
                },
                failureStatus: null,
                tags: tagsWebAndNew,
                timeout: null);


            _options.Value.Registrations.Add(registration);

            TempData["Message"] = $"A UriHealthCheck was added ({nameRandom} | {urlRandom})";
        }


        public void OnPostAddUrl(string name, string url)
        {
            var demo = url;

            var tagsWebAndNew = new[] { "Web", "New" };

            var registration = new HealthCheckRegistration(
                name: name,
                sp =>
                {
                    var uri = new Uri(url);

                    var options = new UriHealthCheckOptions()
                                     .AddUri(uri);

                    options.AddUri(uri);

                    var uriHealthCheck = new UriHealthCheck(
                        options,
                        () => _httpClientFactory.CreateClient(name));

                    return uriHealthCheck;
                },
                failureStatus: null,
                tags: tagsWebAndNew,
                timeout: null);

           
            _options.Value.Registrations.Add(registration);

            TempData["Message"] = $"A UriHealthCheck was added ({name} | {url})";
        }


        public async void OnPostRemoveByName(string name)
        {
            var item = _options.Value.Registrations.FirstOrDefault(c => c.Name == name);

            if (item is not null)
            {
                _options.Value.Registrations.Remove(item);
                TempData["Message"] = $"A UriHealthCheck was removed ({name})";
            }
            else
            {
                TempData["Message"] = $"A UriHealthCheck not exists ({name})";
            }
            
        }


        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var stringBuilder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(chars.Length);
                stringBuilder.Append(chars[index]);
            }

            return stringBuilder.ToString();
        }


        #region TODO: _db.Configurations

        public async Task AddAsync(string name, string uri)
        {
            if (await Get(name).ConfigureAwait(false) == null)
            {
                var config = new HealthCheckConfiguration
                {
                    Name = name,
                    Uri = uri
                    //DiscoveryService = "kubernetes"
                };

                await _db.Configurations.AddAsync(config).ConfigureAwait(false);

                await _db.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("[Push] New service added: {name} with uri: {uri}", name, uri);
            }
        }

        public async Task<bool> RemoveAsync(string name)
        {
            var endpoint = await Get(name).ConfigureAwait(false);

            if (endpoint != null)
            {
                _db.Configurations.Remove(endpoint);
                await _db.SaveChangesAsync().ConfigureAwait(false);


                _logger.LogInformation("[Push] Service removed: {name}", name);


                return true;
            }

            return false;
        }

        public async Task UpdateAsync(string name, string uri)
        {
            var endpoint = await Get(name).ConfigureAwait(false);

            if (endpoint != null)
            {
                endpoint.Uri = uri;
                _db.Configurations.Update(endpoint);
                await _db.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("[Push] Service updated: {name} with uri {uri}", name, uri);
            }
        }

        private Task<HealthCheckConfiguration?> Get(string name)
        {
            return _db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        }


        #endregion
    }

}
