# Serilog.Sinks.NewRelic.Logs

A [Serilog](https://serilog.net/) sink that writes events to [New Relic Logs](https://docs.newrelic.com/docs/logs/new-relic-logs/get-started/introduction-new-relic-logs).

Events are submitted to the New Relic Logs API in batches, formatted using New Relic's [detailed JSON body](https://docs.newrelic.com/docs/logs/new-relic-logs/log-api/introduction-log-api#json-content) and transmitted GZip-compressed.

The package targets `.NET Framework 4.6.2`, `.NET Framework 4.8`, and `.NET Standard 2.0`.

## Getting started

```cs
Log.Logger = new LoggerConfiguration()
    .WriteTo.NewRelicLogs(
        endpointUrl: "https://log-api.newrelic.com/log/v1",
        applicationName: "Serilog.Sinks.NewRelic.Sample",
        licenseKey: "[Your API key]")
    .CreateLogger();
```

If you have the New Relic agent installed and use the environment variables `NEW_RELIC_APP_NAME` and `NEW_RELIC_LICENSE_KEY`, you can simply write:

```cs
Log.Logger = new LoggerConfiguration()
    .WriteTo.NewRelicLogs()
    .CreateLogger();
```

## Configuration

The available parameters are:

* `applicationName` — the name of the current application in New Relic. If omitted, the value of the `NewRelic.AppName` appSetting is used.
* `endpointUrl` — the ingestion URL of New Relic Logs. The US endpoint is used by default if this value is omitted.
* `licenseKey` — the New Relic License key, which is also used with the New Relic Agent.
* `insertKey` — the New Relic Insert API key. Either `licenseKey` or `insertKey` must be supplied.

The sink is derived from [PeriodicBatchingSink](https://github.com/serilog/serilog-sinks-periodicbatching) and therefore supports the following batching parameters:

* `batchSizeLimit` — the maximum number of events to include in a single batch. Default is `1000` entries.
* `period` — the time to wait between checking for event batches. It is a `TimeSpan` with a default value of 2 seconds. If provided from [AppSettings](https://github.com/serilog/serilog/wiki/AppSettings), the value should be given as an absolute time span, i.e. `"0.00:00:05"` standing for 5 seconds.

## JSON configuration

It is possible to configure the sink using [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) by specifying the license key and other desired parameters in `appsettings.json`:

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.NewRelic.Logs" ],
    "WriteTo": [
      {
        "Name": "NewRelicLogs",
        "Args": {
          "applicationName": "NewRelicLogTestSample",
          "licenseKey": "58e9892abd3f09d91b0db0d0e9e95628FFFFNRAL"
          //... other parameters
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId", "WithNewRelicLogsInContext" ]
  }
}
```

## Emitted properties

All properties along with the rendered message are emitted to New Relic Logs. This sink adds the following additional properties:

* `timestamp` — time in milliseconds since epoch.
* `application` — holds the value from `applicationName`.
* `level` — the actual log level of the event.
* `stack_trace` — holds the stack trace portion of an exception.
* `exception` — holds the `.ToString()` of an exception.

If the `newrelic.linkingmetadata` property is present in an event, it will be unrolled into individual New Relic properties used for "logs in context".

## Credits

This code is based on <https://github.com/stanisls/serilog-sinks-newreliclogs> and <https://github.com/ThiagoBarradas/serilog-sinks-newrelic-logs> projects.

Thanks [@stanisls](https://github.com/stanisls), [@johnkattenhorn](https://github.com/johnkattenhorn) and [@ThiagoBarradas](https://github.com/ThiagoBarradas).

## License

Licensed under the [MIT License](https://opensource.org/licenses/MIT).
