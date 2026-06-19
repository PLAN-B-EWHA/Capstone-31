using DotNetClient;
using DotNetClient.Models;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        // 실행 인자가 있으면 우선 사용하고, 없으면 기본 URL/환경변수 값을 사용.
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

        // Spring API 호출을 담당하는 클라이언트. using으로 끝날 때 내부 HttpClient까지 정리.
        using var api = new MyExpressionFriendApiClient(baseUrl);

        try
        {
            Console.WriteLine();
            Console.WriteLine("1. Login");
            // 로그인 성공 시 access token이 api 내부에 저장되고, 이후 인증 요청에 자동 사용됨.
            var login = await api.LoginAsync(email, password);
            Console.WriteLine("Login succeeded.");
            Console.WriteLine($"Grant type: {login.GrantType}");
            Console.WriteLine($"Token expires in: {login.ExpiresIn} ms");

            Console.WriteLine();
            Console.WriteLine("2. Create OpenAI Realtime client secret with access token");
            // 백엔드가 OpenAI API key로 짧은 수명의 Realtime client secret을 발급해줌.
            var realtime = await api.CreateRealtimeClientSecretAsync();
            Console.WriteLine("Realtime client secret created.");
            Console.WriteLine($"Model: {realtime.Model}");
            Console.WriteLine($"Expires at: {realtime.ExpiresAt}");
            Console.WriteLine($"Client secret received: {!string.IsNullOrWhiteSpace(realtime.ClientSecret)}");

            Console.WriteLine();
            Console.WriteLine("2-1. Send text to Realtime and play spoken audio");
            // 읽어줄 텍스트는 환경변수로 바꿀 수 있고, 없으면 기본 테스트 문장을 사용.
            var realtimeText = Environment.GetEnvironmentVariable("MEF_REALTIME_TEXT")
                               ?? "안녕하세요. 오늘은 친구에게 반갑게 인사하는 연습을 해볼게요.";
            // Realtime audio delta를 받는 즉시 StreamingPcmAudioPlayer로 PC 스피커에 출력.
            await RealtimeAudioSmokeTest.PlayAudioAsync(realtime, realtimeText);
            Console.WriteLine("Realtime audio played.");

            Console.WriteLine();
            Console.WriteLine("3. Get selected child with access token");
            // Unity에서 현재 플레이 대상으로 선택된 child 정보 조회.
            var selectedChild = await api.GetSelectedChildAsync();
            Console.WriteLine($"Selected child id: {selectedChild.ChildId}");
            Console.WriteLine($"Selected child name: {selectedChild.ChildName}");

            Console.WriteLine();
            Console.WriteLine("4. Get published Unity scenarios");
            // Unity 클라이언트가 내려받을 published scenario 목록 조회.
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

            // 결과 저장 API 테스트용 샘플 DTO. 실제 Unity에서는 게임 종료 데이터로 채우면 됨.
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
            // dialogue 게임 플레이 결과를 백엔드에 저장하고 session id를 받음.
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
