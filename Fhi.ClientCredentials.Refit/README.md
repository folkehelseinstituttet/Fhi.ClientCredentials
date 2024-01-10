# Fhi.ClientCredentials.Refit

This package contains code to simplify working with Refit and HelseId. 

This default setup will add a token handler to your Refit Interface in addition to letting you add multiple delegates if needed (f.ex. logging).

## Usage

Include thhis code in your WebApi startup builder:

```
builder.AddClientCredentialsRefitBuilder()
    .AddRefitClient<IMyRefitClient>();
```

If you want to add additional loggers add them before "AddRefitClient": 

```
builder.AddClientCredentialsRefitBuilder()
    .AddHandler<MyLoggingDelegationHandler>()
    .AddRefitClient<IMyRefitClient>();
```
The code loads your configuration from IConfiguration using the section "ClientCredentialsConfiguration".
If you want to override which section to use you can pass the correct section to AddClientCredentialsKeypairs:

```
builder.AddClientCredentialsRefitBuilder("CustomClientCredentialsConfiguration")
    .AddRefitClient<IMyRefitClient>();
```

The default RefitSettings we are using use SystemTextJsonContentSerializer, is case insensitive and use camelCasing.
If you want to override the default RefitSettings to use you can pass the settings to AddClientCredentialsKeypairs:

```
builder.AddClientCredentialsRefitBuilder(new RefitSettings())
    .AddRefitClient<IMyRefitClient>();
```

## Adding Correlation Id to all requests

Use "AddCorrelationId()" to add header propagation of the default FHI correlation id header. 

```
builder.AddClientCredentialsRefitBuilder()
    .AddCorrelationId()
    .AddRefitClient<IMyRefitClient>();
```

A new correlation ID will be given to each request and response that does not contain the header when invoked.
Remember to add usage of header propagation to your app startup code. It should be placed before any logging middleware:

```
app.UseCorrelationId();
```

## More!

Add the handler "LoggingDelegationHandler" to automatically log all Refit requets. The logger has to be availible trough dependency injection using Microsoft.Extensions.Logging (ILogger).

```
builder.AddClientCredentialsRefitBuilder()
    .AddHandler<LoggingDelegationHandler>();
```