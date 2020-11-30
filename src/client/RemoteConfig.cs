namespace ivy.remote
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Flurl.Http;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public static class RemoteConfig
    {
        public static bool IsInitialized { get; private set; }
        public static RuntimeConfig AppConfig { get; private set; }
        internal static string _endpoint { get; set; }
        internal static string _appID { get; set; }
        internal static ILogger<RuntimeConfig> _logger { get; set; }

        public static void Init(string appID, ILogger<RuntimeConfig> logger, string endpoint = 
            #if DEBUG
            "https://localhost:8080"
            #else
            "https://remote-config.ivy.run"
            #endif
        )
        {
            if (!IsInitialized)
            {
                AppConfig = new RuntimeConfig();
                _logger = logger;
                _endpoint = endpoint;
                _appID = appID;
                IsInitialized = true;
            }
        }

        public static T Get<T>(string key, T defaultValue = default)
        {
            var strValue = AppConfig.Get(key, "");
            if (strValue is "")
                return defaultValue;

            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter.CanConvertFrom(typeof(string)))
                return (T) converter.ConvertFromInvariantString(strValue);
            return strValue.Try(JsonConvert.DeserializeObject<T>, () => defaultValue);
        }

        public static async Task FetchAsync(CancellationToken cancellationToken)
        {
            var result = await $"{_endpoint}/project/{_appID}"
                .WithTimeout(4000)
                .AllowAnyHttpStatus()
                .GetAsync(cancellationToken);

            if (result is {StatusCode: 200})
            {
                AppConfig.SetData(await result.GetJsonAsync<Dictionary<string, string>>());
            }
            else
            {
                _logger.LogError($"Failed to fetch config data. [{result.ResponseMessage}]");
            }
        }
    }

    public class RuntimeConfig
    {
        private readonly IDictionary<string, string> keys = new Dictionary<string, string>();

        public void SetData(Dictionary<string, string> data) => 
            keys.Merge(data);

        public string Get(string key, string defaultValue = default) => 
            keys.ContainsKey(key) ? keys[key] : defaultValue;
    }

    public static class DictEx
    {
        public static void SetOrUpdate<K, V>(this IDictionary<K, V> self, K key, V value)
        {
            if (self.ContainsKey(key))
                self[key] = value;
            else
                self.Add(key, value);
        }

        public static void Merge<K, V>(this IDictionary<K, V> self, IDictionary<K, V> another)
        {
            foreach (var (key, value) in another)
                self.SetOrUpdate(key, value);
        }

        public static X Try<T, X>(this T obj, Func<T, X> functor, Func<X> fail)
        {
            try
            {
                return functor(obj);
            }
            catch
            {
                return fail();
            }
        }
    }
}