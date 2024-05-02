## gettoken command line tool

This tool create an access token based on a given JSON file containing a client credentials configuration.
If you are not using the standard heading "ClientCredentialsConfiguration" 
you can supply the section name as the last argument.

Usage:

```
gettoken [ConfigSectionName]

gettoken [appsettings.json ..] [ConfigSectionName]

gettoken [directory-to-search] [ConfigSectionName]
```

If no files are given it will search in the given directiory for any "appsettings*.json" files and
"HelseID Configuration *.json" files. If no directory is given it will search the working directory.

The tool will automatically try to add additional information from environment-specific files,
so if you supply the file "appsettings.json" it will also include "appsettings.Development.json".

You can change the default environment name ("Development") by setting the env var ASPNETCORE_ENVIRONMENT.

