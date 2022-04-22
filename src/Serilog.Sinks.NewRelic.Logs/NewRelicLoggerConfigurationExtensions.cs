using System;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.NewRelic.Logs;
using Serilog.Sinks.PeriodicBatching;

// ReSharper disable once CheckNamespace
namespace Serilog
{
    /// <summary>
    /// Extends Serilog configuration to write events to NewRelic Logs
    /// </summary>
    public static class NewRelicLoggerConfigurationExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="loggerSinkConfiguration">The logger configuration.</param>
        /// <param name="endpointUrl">The NewRelic Logs API endpoint URL. Default is set to https://log-api.newrelic.com/log/v1 located in the US.</param>
        /// <param name="applicationName">Application name in NewRelic. This can be either supplied here or through "NewRelic.AppName" appSettings</param>
        /// <param name="licenseKey">New Relic APM License key. Either "licenseKey" or "insertKey" must be provided.</param>
        /// <param name="insertKey">New Relic Insert API key. Either "licenseKey" or "insertKey" must be provided.</param>
        /// <param name="enforceCamelCase">Converts all logged property names to their camelCase representation.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required 
        ///     in order to write an event to the sink.</param>
        /// <returns></returns>
        public static LoggerConfiguration NewRelicLogs(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string endpointUrl = NewRelicEndpoints.US,
            string applicationName = null,
            string licenseKey = null,
            string insertKey = null,
            bool enforceCamelCase = false,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum
        )
        {
            if (loggerSinkConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerSinkConfiguration));
            }

            if (string.IsNullOrWhiteSpace(applicationName))
            {
                #if NETFRAMEWORK
                applicationName = ConfigurationManager.AppSettings["NewRelic.AppName"];
                #endif

                if (string.IsNullOrWhiteSpace(applicationName))
                {
                    applicationName = Environment.GetEnvironmentVariable("NEW_RELIC_APP_NAME");
                }

                if (string.IsNullOrWhiteSpace(applicationName))
                {
                    throw new ArgumentException("Must supply an application name either as a parameter or an appsetting", nameof(applicationName));
                }
            }

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                #if NETFRAMEWORK
                endpointUrl = ConfigurationManager.AppSettings["NewRelic.EndpointUrl"];
                #endif

                if (string.IsNullOrWhiteSpace(endpointUrl))
                {
                    throw new ArgumentException("NewRelic Logs API endpoint URL must be supplied");
                }
            }

            if (string.IsNullOrWhiteSpace(licenseKey) && string.IsNullOrWhiteSpace(insertKey))
            {
                #if NETFRAMEWORK
                licenseKey = ConfigurationManager.AppSettings["NewRelic.LicenseKey"];
                insertKey = ConfigurationManager.AppSettings["NewRelic.InsertKey"];
                #endif

                if (string.IsNullOrWhiteSpace(licenseKey))
                {
                    licenseKey = Environment.GetEnvironmentVariable("NEW_RELIC_LICENSE_KEY");
                }

                if (string.IsNullOrWhiteSpace(licenseKey) && string.IsNullOrWhiteSpace(insertKey))
                {
                    throw new ArgumentException("Either LicenseKey or InsertKey must be supplied");
                }
            }

            var sink = new NewRelicLogsSink(endpointUrl, applicationName, licenseKey, insertKey, enforceCamelCase);
            var batchingOptions = new PeriodicBatchingSinkOptions(); // Use the default options
            var batchingSink = new PeriodicBatchingSink(sink, batchingOptions);
            
            return loggerSinkConfiguration.Sink(batchingSink, restrictedToMinimumLevel);
        }
    }
}