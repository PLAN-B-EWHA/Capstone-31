using DotNetClient;
using DotNetClient.Models;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var baseUrl = args.Length > 0 ? args[0] : "http://localhost:8080";
        var email = args.Length > 1 ? args[1] : Environment.GetEnvironmentVariable("MEF_EMAIL");
        var password = args.Length > 2 ? args[2] : Environment.GetEnvironmentVariable("MEF_PASSWORD");

        Console.WriteLine($"Spring API Base URL: {baseUrl}");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Email and password are required.");
            Console.WriteLine("Run with arguments:");
            Console.WriteLine("dotnet run -- http://localhost:8080 user@example.com password");
            Console.WriteLine();
            Console.WriteLine("Or set environment variables first:");
            Console.WriteLine("$env:MEF_EMAIL=\"user@example.com\"");
            Console.WriteLine("$env:MEF_PASSWORD=\"password\"");
            Console.WriteLine("dotnet run -- http://localhost:8080");
            return;
        }

        using var api = new MyExpressionFriendApiClient(baseUrl);

        try
        {
            Console.WriteLine();
            Console.WriteLine("1. Login");
            var login = await api.LoginAsync(email, password);
            Console.WriteLine("Login succeeded.");
            Console.WriteLine($"Grant type: {login.GrantType}");
            Console.WriteLine($"Token expires in: {login.ExpiresIn} ms");

            Console.WriteLine();
            Console.WriteLine("2. Create OpenAI Realtime client secret with access token");
            var realtime = await api.CreateRealtimeClientSecretAsync();
            Console.WriteLine("Realtime client secret created.");
            Console.WriteLine($"Model: {realtime.Model}");
            Console.WriteLine($"Expires at: {realtime.ExpiresAt}");
            Console.WriteLine($"Client secret received: {!string.IsNullOrWhiteSpace(realtime.ClientSecret)}");

            Console.WriteLine();
            Console.WriteLine("2-1. Send text to Realtime and save spoken audio");
            var realtimeText = Environment.GetEnvironmentVariable("MEF_REALTIME_TEXT")
                               ?? "안녕하세요. 오늘은 친구에게 반갑게 인사하는 연습을 해볼게요.";
            var wavPath = await RealtimeAudioSmokeTest.CreateAudioAsync(realtime, realtimeText);
            Console.WriteLine("Realtime audio saved.");
            Console.WriteLine($"WAV path: {wavPath}");

            Console.WriteLine();
            Console.WriteLine("3. Get selected child with access token");
            var selectedChild = await api.GetSelectedChildAsync();
            Console.WriteLine($"Selected child id: {selectedChild.ChildId}");
            Console.WriteLine($"Selected child name: {selectedChild.ChildName}");

            Console.WriteLine();
            Console.WriteLine("4. Get published Unity scenarios");
            var serverScenarios = await api.GetPublishedScenariosAsync();
            Console.WriteLine($"Published scenario count: {serverScenarios.Count}");

            var firstScenario = serverScenarios.FirstOrDefault();
            if (firstScenario is not null)
            {
                Console.WriteLine($"First scenario id: {firstScenario.ScenarioId}");
                Console.WriteLine($"First scenario theme: {firstScenario.Metadata?.Theme}");
                Console.WriteLine($"Theme enum for save API: {PeersThemes.FromWeek(firstScenario.Metadata?.Week)}");
                Console.WriteLine($"Dialogue turn count: {firstScenario.DialogueFlow.Count}");
            }
            else
            {
                Console.WriteLine("No published server scenario was returned.");
                Console.WriteLine("The save example will use a local Unity scenario id.");
            }

            var dialogueResult = new DialogueResultSaveRequest
            {
                ScenarioId = firstScenario?.ScenarioId ?? "W01_SY_001",
                ScenarioSource = ScenarioSources.UnityLocal,
                Theme = PeersThemes.FromWeek(firstScenario?.Metadata?.Week),
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                EndedAt = DateTimeOffset.UtcNow,
                TotalScore = 2,
                MaxScore = 3,
                Turns =
                [
                    new DialogueTurnResult
                    {
                        TurnId = 1,
                        SelectedOptionOrder = 1,
                        SelectedScore = 2
                    }
                ]
            };

            Console.WriteLine();
            Console.WriteLine("5. Save dialogue game result with access token");
            var sessionId = await api.SaveDialogueResultAsync(dialogueResult);
            Console.WriteLine("Dialogue result saved.");
            Console.WriteLine($"Session id: {sessionId}");
        }
        catch (ApiClientException ex)
        {
            Console.WriteLine();
            Console.WriteLine("Spring API request failed.");
            Console.WriteLine($"HTTP status: {(int?)ex.StatusCode}");
            Console.WriteLine($"Message: {ex.Message}");

            if (!string.IsNullOrWhiteSpace(ex.ErrorCode))
            {
                Console.WriteLine($"ErrorCode: {ex.ErrorCode}");
            }

            if (!string.IsNullOrWhiteSpace(ex.ResponseBody))
            {
                Console.WriteLine($"Response body: {ex.ResponseBody}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Unexpected error.");
            Console.WriteLine(ex.Message);
        }
    }
}
