using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmzdmCheckin
{
    class Program
    {
        static Conf _conf;
        static HttpClient _scClient;

        static async Task Main()
        {
            _conf = Deserialize<Conf>(GetEnvValue("CONF"));
            if (!string.IsNullOrWhiteSpace(_conf.ScKey))
            {
                _scClient = new HttpClient();
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36");
            client.DefaultRequestHeaders.Referrer = new Uri("https://www.smzdm.com/");
            client.DefaultRequestHeaders.Add("Cookie", _conf.Cookie);

            string result = await client.GetStringAsync($"https://zhiyou.smzdm.com/user/checkin/jsonp_checkin?_={DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
            using JsonDocument doc = JsonDocument.Parse(result);
            JsonElement root = doc.RootElement;
            if (root.GetProperty("error_code").GetInt32() != 0)
            {//Cookie失效
                await Notify($"smzdm Cookie失效，请及时更新！", true);
                await Notify($"{result}");
            }
            else
            {
                await Notify($"smzdm 签到成功，签到天数: {root.GetProperty("data").GetProperty("checkin_num").GetRawText()}");
            }
        }

        static async Task Notify(string msg, bool isFailed = false)
        {
            Console.WriteLine(msg);
            if (_conf.ScType == "Always" || (isFailed && _conf.ScType == "Failed"))
            {
                await _scClient?.GetAsync($"https://sc.ftqq.com/{_conf.ScKey}.send?text={msg}");
            }
        }

        static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _options);

        static string GetEnvValue(string key) => Environment.GetEnvironmentVariable(key);
    }

    #region Conf

    public class Conf
    {
        public string Cookie { get; set; }
        public string ScKey { get; set; }
        public string ScType { get; set; }
    }

    #endregion
}
