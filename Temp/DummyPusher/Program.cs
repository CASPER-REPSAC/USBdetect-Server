using Microsoft.AspNetCore.SignalR.Client;

var defaultUrl = "http://blatter.kro.kr:5009/chathub";
var defaultFile = "../test.json";
var defaultUser = "dummy-pusher";

var url = GetArg("--url") ?? defaultUrl;
var file = GetArg("--file") ?? defaultFile;
var username = GetArg("--user") ?? defaultUser;

if (!File.Exists(file))
{
    Console.Error.WriteLine($"File not found: {file}");
    return 1;
}

string payload;
try
{
    payload = await File.ReadAllTextAsync(file);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to read file: {ex.Message}");
    return 1;
}

Console.WriteLine($"Connecting to {url} as {username}...");

var connection = new HubConnectionBuilder()
    .WithUrl($"{url}?username={Uri.EscapeDataString(username)}")
    .Build();

connection.Closed += async error =>
{
    Console.WriteLine($"Connection closed: {error?.Message}");
    await Task.Delay(1000);
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Connected. Sending payload...");
    await connection.InvokeAsync("SendDeviceList", payload);
    Console.WriteLine("SendDeviceList invoked successfully.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error while sending: {ex.Message}");
    return 1;
}
finally
{
    await connection.DisposeAsync();
}

return 0;

string? GetArg(string name)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return args[i + 1];
        }
    }
    return null;
}
