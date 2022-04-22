using Newtonsoft.Json;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Serilog.Sinks.NewRelic.Logs
{
    internal class NewRelicLogsSink : IBatchedLogEventSink
    {
        public string EndpointUrl { get; }

        public string ApplicationName { get; }

        public string LicenseKey { get; }

        public string InsertKey { get; }

        private IFormatProvider FormatProvider { get; }

        private JsonSerializerSettings JsonSerializerSettings { get; }

        public NewRelicLogsSink(
            string endpointUrl, 
            string applicationName, 
            string licenseKey, 
            string insertKey,
            bool enforceCamelCase,
            IFormatProvider formatProvider = null)
        {
            this.EndpointUrl = endpointUrl;
            this.ApplicationName = applicationName;
            this.LicenseKey = licenseKey;
            this.InsertKey = insertKey;
            this.FormatProvider = formatProvider;
            this.JsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            if (enforceCamelCase)
            {
                var contractResolver = new CamelCasePropertyNamesContractResolver
                {
                    // Types which implements the ISerializable interface prevent the CamelCaseNamingStrategy
                    // from being applied, so all fields are serialized in PascalCase. The workaround is to
                    // ignore the ISerializable interface.
                    IgnoreSerializableInterface = true
                };

                this.JsonSerializerSettings.ContractResolver = contractResolver;
            }
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> eventsEnumerable)
        {
            var payload = new NewRelicLogPayload(this.ApplicationName);
            var events = eventsEnumerable.ToList();

            foreach (var _event in events)
            {
                try
                {
                    var item = new NewRelicLogItem(_event, this.FormatProvider);

                    payload.Logs.Add(item);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Log event could not be formatted and was dropped: {0} {1}", ex.Message, ex.StackTrace);
                }
            }

            var body = Serialize(new List<object> { payload }, events.Count);

            await this.SendToNewRelicLogsAsync(body).ConfigureAwait(false);
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        private Task SendToNewRelicLogsAsync(string body)
        {
            return Task.Run(() =>
            {
                try
                {
                    // add something like polly to try 3 times
                    this.SendToNewRelicLogs(body);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Event batch could not be sent to NewRelic Logs and was dropped: {0} {1}", ex.Message, ex.StackTrace);
                }
            });
        }

        private void SendToNewRelicLogs(string body)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            if (!(WebRequest.Create(this.EndpointUrl) is HttpWebRequest request))
            {
                return;
            }

            var apiKey = string.IsNullOrWhiteSpace(this.LicenseKey)
                ? this.InsertKey
                : this.LicenseKey;

            request.Headers.Add("Api-Key", apiKey);
            request.Headers.Add("Content-Encoding", "gzip");
            request.Timeout = 40000; //It's basically fire-and-forget
            request.Credentials = CredentialCache.DefaultCredentials;
            request.ContentType = "application/json";
            request.Accept = "*/*";
            request.Method = "POST";
            request.KeepAlive = false;

            var byteStream = Encoding.UTF8.GetBytes(body);

            try
            {
                using (var zippedRequestStream = new GZipStream(request.GetRequestStream(), CompressionMode.Compress))
                {
                    zippedRequestStream.Write(byteStream, 0, byteStream.Length);
                    zippedRequestStream.Flush();
                    zippedRequestStream.Close();
                }
            }
            catch (WebException ex)
            {
                SelfLog.WriteLine("Failed to create WebRequest to NewRelic Logs: {0} {1}", ex.Message, ex.StackTrace);
                return;
            }

            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response == null || response.StatusCode != HttpStatusCode.Accepted)
                    {
                        SelfLog.WriteLine("Self-log: Response from NewRelic Logs is missing or negative: {0}", response?.StatusCode);
                    }
                }
            }
            catch (WebException ex)
            {
                SelfLog.WriteLine("Failed to parse response from NewRelic Logs: {0} {1}", ex.Message, ex.StackTrace);
            }
        }

        private string Serialize(List<object> items, int count)
        {
            var serializer = JsonSerializer.Create(this.JsonSerializerSettings);

            //Stipulate 500 bytes per log entry on average
            var json = new StringBuilder(count * 500);

            using (var stringWriter = new StringWriter(json))
            {
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    serializer.Serialize(jsonWriter, items);
                }
            }

            return json.ToString();
        }
    }
}
