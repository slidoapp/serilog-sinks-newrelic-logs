# Slido.Serilog.Sinks.NewRelic.Logs

A [Serilog](https://serilog.net/) sink that writes events to [New Relic Logs](https://docs.newrelic.com/docs/logs/new-relic-logs/get-started/introduction-new-relic-logs).

Events are submitted to the New Relic Logs API in batches, formatted using New Relic's [detailed JSON body](https://docs.newrelic.com/docs/logs/new-relic-logs/log-api/introduction-log-api#json-content) and transmitted GZip-compressed.

The package targets `.NET Framework 4.6.2`, `.NET Framework 4.8`, and `.NET Standard 2.0`.

## Install via NuGet

```shell
dotnet add package Slido.Serilog.Sinks.NewRelic.Logs
```

In the Visual Studio Package Manager Console, run `Install-Package Slido.Serilog.Sinks.NewRelic.Logs` instead.

## Getting started

```cs
Log.Logger = new LoggerConfiguration()
    .WriteTo.NewRelicLogs(
        endpointUrl: "https://log-api.newrelic.com/log/v1",
        applicationName: "Serilog.Sinks.NewRelic.Sample",
        licenseKey: "[Your API key]")
    .CreateLogger();
```

Accounts in the EU data center must set `endpointUrl` to `NewRelicEndpoints.EU`, which is `https://log-api.eu.newrelic.com/log/v1`. The `NewRelicEndpoints` class is in the `Serilog.Sinks.NewRelic.Logs` namespace.

With `NEW_RELIC_APP_NAME` and `NEW_RELIC_LICENSE_KEY` set, you can omit the arguments. The New Relic agent is not required:

```cs
Log.Logger = new LoggerConfiguration()
    .WriteTo.NewRelicLogs()
    .CreateLogger();
```

## Configuration

The `NewRelicLogs(...)` parameters are:

| Parameter | Default | Behavior |
|---|---|---|
| `endpointUrl` | `https://log-api.newrelic.com/log/v1` | New Relic Logs ingestion URL. If explicitly blank, uses `NewRelic.EndpointUrl` appSettings on .NET Framework; otherwise throws if no endpoint is resolved. |
| `applicationName` | `null` | Required application name. Falls back to `NewRelic.AppName` appSettings on .NET Framework, then `NEW_RELIC_APP_NAME`; throws if none is resolved. |
| `licenseKey` | `null` | New Relic license key. Takes precedence over `insertKey` when both are resolved. |
| `insertKey` | `null` | Alternative API key. At least one key must be resolved. |
| `enforceCamelCase` | `false` | Enables Newtonsoft.Json camel-case serialization for property names, including dictionary keys. |
| `restrictedToMinimumLevel` | `LevelAlias.Minimum` (`Verbose`) | Minimum level accepted by this sink; the logger's own minimum level still applies. |

When both key arguments are blank, the sink reads `NewRelic.LicenseKey` and `NewRelic.InsertKey` appSettings on .NET Framework, then fills a missing license key from `NEW_RELIC_LICENSE_KEY`. It throws if neither key is resolved. Other targets do not read appSettings.

Events are buffered by [PeriodicBatchingSink](https://github.com/serilog/serilog-sinks-periodicbatching) using its default options: up to 1000 events per batch, a two-second batching period, a 100,000-event queue limit, and eager emission of the first event. `NewRelicLogs(...)` does not expose `batchSizeLimit`, `period`, or other batching options.

Call `Log.CloseAndFlush()` before the application exits, because the events in the current batch are lost otherwise. The sink does not retry failed requests. When a request fails or New Relic responds with a status other than `202 Accepted`, the sink writes the error to Serilog's `SelfLog` and discards the batch. Call `Serilog.Debugging.SelfLog.Enable(Console.Error)` to see these errors.

## JSON configuration

Install [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) separately and load the `Serilog` section with `ReadFrom.Configuration(configuration)`. The assembly name remains `Serilog.Sinks.NewRelic.Logs`, without the package's `Slido.` prefix.

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.NewRelic.Logs" ],
    "WriteTo": [
      {
        "Name": "NewRelicLogs",
        "Args": {
          "applicationName": "NewRelicLogTestSample",
          "licenseKey": "[Your API key]"
        }
      }
    ]
  }
}
```

This example requires no additional enrichers. Machine-name, thread-ID, and New Relic agent linking enrichers require their own packages and configuration.

## Emitted properties

The request body is a JSON array with one object, which contains shared `common.attributes` and a `logs` array. Each log contains its rendered `message`, a `timestamp`, and an `attributes` object holding event properties.

| Field | Location | Value |
|---|---|---|
| `application` | `common.attributes` | Resolved application name. |
| `timestamp` | Log item | Unix time in milliseconds. |
| `message` | Log item | Rendered Serilog message. String property values appear in double quotes, as in Serilog's default rendering. |
| `level` | Log attributes | `Info` for Serilog `Information`; otherwise the Serilog level name. |
| `iso8601Timestamp` | Log attributes | Event timestamp serialized with its UTC offset. |
| `exception` | Log attributes, when an exception exists | `Exception.ToString()`. |
| `stackTrace` | Log attributes, when an exception exists | `Exception.StackTrace`, which is `null` for an exception that was never thrown. |

If the `newrelic.linkingmetadata` property is present in an event, it will be unrolled into individual New Relic properties used for "logs in context". The [NewRelic.LogEnrichers.Serilog](https://www.nuget.org/packages/NewRelic.LogEnrichers.Serilog) package adds this property when you call `.Enrich.WithNewRelicLogsInContext()` and the New Relic .NET agent is running.

The sink drops an event when one of its properties is named `level`, `iso8601Timestamp`, `exception` or `stackTrace`, because these names collide with the attributes the sink adds. The sink reports the dropped event through Serilog's `SelfLog`.

## Changelog

See the [NuGet version history](https://www.nuget.org/packages/Slido.Serilog.Sinks.NewRelic.Logs/#versions-body-tab).

## Credits

This code is based on <https://github.com/stanisls/serilog-sinks-newreliclogs> and <https://github.com/ThiagoBarradas/serilog-sinks-newrelic-logs> projects.

Thanks [@stanisls](https://github.com/stanisls), [@johnkattenhorn](https://github.com/johnkattenhorn) and [@ThiagoBarradas](https://github.com/ThiagoBarradas).

## License

Licensed under the [MIT License](https://opensource.org/licenses/MIT).
