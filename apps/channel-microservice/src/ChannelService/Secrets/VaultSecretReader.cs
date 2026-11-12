using System.Net;
using System.Text.Json;

namespace ChannelService.Secrets;

// Reads the messaging credentials from HashiCorp Vault. This runs once at
// startup, before the host exists, so it is deliberately synchronous.
public static class VaultSecretReader
{
    public static MessagingCredentials ReadMessagingCredentials(IConfiguration configuration)
    {
        var address = configuration["Vault:Address"]
            ?? throw new InvalidOperationException("Vault:Address is not configured.");

        var secretPath = configuration["Vault:SecretPath"]
            ?? throw new InvalidOperationException("Vault:SecretPath is not configured.");

        var token = Environment.GetEnvironmentVariable("VAULT_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "VAULT_TOKEN is not set. This service reads its messaging credentials "
                + "from Vault and cannot start without a token.");
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri(address.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/{secretPath}");
        request.Headers.Add("X-Vault-Token", token);

        HttpResponseMessage response;

        try
        {
            response = client.Send(request);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Vault at {address} could not be reached.", exception);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    "Vault denied access to the messaging credentials. "
                    + "Check the token, its time to live and its policy.");
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                throw new InvalidOperationException(
                    "Vault is sealed. Unseal it with two of the unseal keys and start the service again.");
            }

            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            using var document = JsonDocument.Parse(stream);

            var data = document.RootElement.GetProperty("data").GetProperty("data");

            var username = data.GetProperty("Username").GetString();
            var password = data.GetProperty("Password").GetString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "The Vault secret is missing Username or Password.");
            }

            return new MessagingCredentials(username, password);
        }
    }
}

public sealed record MessagingCredentials(string Username, string Password);