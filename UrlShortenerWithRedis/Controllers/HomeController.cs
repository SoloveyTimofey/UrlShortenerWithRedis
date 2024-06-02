using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Net;
using UrlShortenerWithRedis.Models;
using System.Text;

namespace UrlShortenerWithRedis.Controllers
{
    public class HomeController : Controller
    {
        private readonly ConnectionMultiplexer redis;
        public HomeController(ConnectionMultiplexer connectionMultiplexer)
        {
            this.redis = connectionMultiplexer;
        }

        public IActionResult Index()
        {
            Dictionary<string, string> kvps = GetUrlKvps();

            return View(new UrlShortenerViewModel
            {
                Url = null,
                UrlKvps = kvps.Count()==0 ? null : kvps
            });
        }

        [HttpGet("{shortenedUrl}")]
        public IActionResult Index(string shortenedUrl)
        {
            IDatabase db= redis.GetDatabase();

            if (db.KeyExists(shortenedUrl))
            {
                string? urlToRedirect = db.StringGet(shortenedUrl);
                if (urlToRedirect == null)
                {
                    throw new ArgumentException(nameof(shortenedUrl));
                }

                return Redirect(urlToRedirect);
            }
            else
            {
                throw new ArgumentException(nameof(shortenedUrl));
            }
        }

        [HttpPost]
        public async Task<IActionResult> ShortenURL(string url)
        {
            bool exists = await UrlExists(url);

            if (exists)
            {
                string baseUrl = GetBaseUrl();
                string shortenedPath = GenerateShortenedPath();

                IDatabase db = redis.GetDatabase();
                db.StringSet(shortenedPath, url);

                return View("Index",new UrlShortenerViewModel
                {
                    Url = url,
                    ShortenedUrl = $"{baseUrl}{shortenedPath}",
                    UrlKvps = GetUrlKvps()
                });
            }
            else
            {
                return View("Index", new UrlShortenerViewModel
                {
                    ErrorMessage = "Your URL is invalid",
                    UrlKvps = GetUrlKvps()
                });
            }
        }

        private Dictionary<string, string> GetUrlKvps()
        {
            IDatabase db = redis.GetDatabase();
            IServer server = redis.GetServer("localhost:6379");

            Dictionary<string, string> kvps = new Dictionary<string, string>();
            foreach (RedisKey key in server.Keys())
            {
                kvps.Add(key!, db.StringGet(key)!);
            }

            return kvps;
        }

        private string GenerateShortenedPath()
        {
            Random rnd = new Random();
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (rnd.Next(1,3)==1)
                {
                    stringBuilder.Append(Char.ConvertFromUtf32(rnd.Next(97, 122)));
                }
                else
                {
                    stringBuilder.Append(Char.ConvertFromUtf32(rnd.Next(65, 90)));
                }
            }

            return stringBuilder.ToString();
        }

        private string GetBaseUrl()
        {
            string scheme = HttpContext.Request.Scheme;
            string host = HttpContext.Request.Host.Host;
            int? port = HttpContext.Request.Host.Port;

            return $"{scheme}://{host}:{port}/";
        }

        private async Task<bool> UrlExists(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                   HttpResponseMessage httpResponseMessage=  await client.GetAsync(url);

                    if (httpResponseMessage != null && httpResponseMessage.IsSuccessStatusCode||httpResponseMessage?.StatusCode == HttpStatusCode.Found)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
