using Fhi.ClientCredentialsKeypairs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;
using System.Text.Json;

try
{
	var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

	var validFiles = new List<string>();

	var inputFiles = args.Where(x => x.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase)).ToList();
	inputFiles.AddRange(inputFiles.Select(x => x.Replace(".json", $".{env}.json", StringComparison.InvariantCultureIgnoreCase)).ToList());

	validFiles = inputFiles.Where(x => File.Exists(x)).ToList();
	if (inputFiles.Count > 0 && validFiles.Count == 0)
	{
		throw new Exception("No valid json files found. Tried: " + string.Join(", ", inputFiles));
	}

	if (args.Length > 0 && Directory.Exists(args[0]))
	{
		validFiles = GetDefaultFiles(args[0]);
	}

	if (validFiles.Count == 0)
	{
		validFiles = GetDefaultFiles(".");
	}

	if (validFiles.Count == 0)
	{
		throw new Exception(@"No valid json file found.

Usage: gettoken [appsettings.json] [ConfigSectionName]
Usage: gettoken [directory-to-search] [ConfigSectionName]");
	}

	var section = args.LastOrDefault(x => !x.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase) && !Directory.Exists(x))
		?? nameof(ClientCredentialsConfiguration);

	var config = GetConfig(validFiles, section);

	var token = await GetToken(config);

	if (string.IsNullOrEmpty(token))
	{
		config.privateJwk = Shorten(config.privateJwk);
		config.rsaPrivateKey = Shorten(config.rsaPrivateKey);
		var json = JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true });
		throw new Exception("Unable to retrieve token for configuration from files " + string.Join(", ", validFiles) + "\n\n" + json);
	}

	Console.WriteLine(token);
	Console.ReadKey();
}
catch (Exception ex)
{
	Console.WriteLine(ex.Message);
	Environment.Exit(1);
}

ClientCredentialsConfiguration GetConfig(List<string> files, string section)
{
	var builder = new ConfigurationBuilder();
	foreach (var file in files)
	{
		if (File.Exists(file))
		{
			builder.AddJsonFile(file);
		}
	}
	var config = builder.Build().GetSection(section).Get<ClientCredentialsConfiguration>();
	if (config?.ClientId == null)
	{
		config = builder.Build().Get<ClientCredentialsConfiguration>();
	}

	if (config?.ClientId == null)
	{
		throw new Exception($"Unable to find ClientCredentialsConfiguration in json files: " + string.Join(", ", files));
	}

	return config;
}

string? Shorten(string? text, int maxLength = 30)
{
	if (text == null || text.Length <= maxLength) return text;
	return text?.Substring(0, maxLength) + "..";
}

List<string> GetDefaultFiles(string path)
{
	var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

	var matcher = new Matcher(StringComparison.InvariantCultureIgnoreCase);
	matcher.AddInclude("**/appsettings.json");
	matcher.AddInclude($"**/appsettings.{env}.json");
	matcher.AddInclude("**/HelseID Configuration *.json");

	var found = matcher.GetResultsInFullPath(path).ToList();

	if (found.Any(x => x.Contains("HelseID Configuration")))
		found = found.Where(x => x.Contains("HelseID Configuration")).Take(1).ToList();

	if (found.Any(x => x.Contains("appsettings.json")))
		found = found.Where(x => x.Contains("appsettings.json")).Take(1).ToList();

	found.AddRange(found.Select(x => x.Replace(".json", $".{env}.json", StringComparison.InvariantCultureIgnoreCase)).ToList());
	return found.Where(x => File.Exists(x)).ToList();
}

Task<string> GetToken(ClientCredentialsConfiguration config)
{
	var store = new AuthenticationService(config);
	var tokenProvider = new AuthenticationStore(store, Options.Create(config));
	return tokenProvider.GetToken();
}