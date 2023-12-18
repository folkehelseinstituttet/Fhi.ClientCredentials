# Fhi.ClientCredentials.Refit

This package contains code to simplify working with Refit and HelseId. 

This default setup will add a token handler to your Refit Interface in addition to letting you add multiple delegates if needed (f.ex. logging).

## Usage

Include thhis code in your WebApi startup builder:

```
builder.AddClientCredentialsKeypairs()
    .AddRefitClient<ISysvakApiBackgroundClient>();
```

If you want to add additional loggers add them before "AddRefitClient": 

```
builder.AddClientCredentialsKeypairs()
    .AddHandler<MyLoggingDelegationHandler>()
    .AddRefitClient<ISysvakApiBackgroundClient>();
```
The code loads your configuration from IConfiguration using the section "ClientCredentialsConfiguration".
If you want to override which section to use you can pass the correct section to AddClientCredentialsKeypairs:

```
builder.AddClientCredentialsKeypairs("CustomClientCredentialsConfiguration")
    .AddRefitClient<ISysvakApiBackgroundClient>();
```

The default RefitSettings we are using use SystemTextJsonContentSerializer, is case insensitive and use camelCasing.
If you want to override the default RefitSettings to use you can pass the settings to AddClientCredentialsKeypairs:

```
builder.AddClientCredentialsKeypairs(new RefitSettings())
    .AddRefitClient<ISysvakApiBackgroundClient>();
```
