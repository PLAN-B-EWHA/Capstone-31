# DotNetClient

Spring Boot API and Unity/.NET client integration test project.

This console app checks the same HTTP flow that Unity will use later:

1. Login with email and password.
2. Save the returned access token.
3. Request an OpenAI Realtime client secret with the same token.
4. Connect to OpenAI Realtime with that short-lived secret and play spoken audio through the PC speaker.
5. Request selected child data with `Authorization: Bearer`.
6. Request published Unity scenarios.
7. Send a dialogue game result with the same token.

## Run Spring API

Open a terminal in the Spring Boot project:

```powershell
cd C:\r2025\backend\myexpressionfriend-api
.\gradlew.bat bootRun
```

Keep this server running while testing the .NET client.

For the Realtime test, the Spring API also needs OpenAI Realtime enabled:

```powershell
$env:OPENAI_REALTIME_ENABLED="true"
$env:OPENAI_API_KEY="sk-..."
```

The spoken Realtime smoke test uses this text by default:

```text
안녕하세요. 오늘은 친구에게 반갑게 인사하는 연습을 해볼게요.
```

You can override it before running:

```powershell
$env:MEF_REALTIME_TEXT="여기에 읽어볼 문장을 넣으세요."
```

## Run .NET Client

Open another terminal:

```powershell
cd C:\r2025\backend\dNet\DotNetClient
dotnet run -- http://localhost:8080 user@example.com password
```

You can also use environment variables:

```powershell
cd C:\r2025\backend\dNet\DotNetClient
$env:MEF_EMAIL="user@example.com"
$env:MEF_PASSWORD="password"
dotnet run -- http://localhost:8080
```

## Expected Output

The output should follow this order:

```text
1. Login
2. Create OpenAI Realtime client secret with access token
2-1. Send text to Realtime and play spoken audio
3. Get selected child with access token
4. Get published Unity scenarios
5. Save dialogue game result with access token
```

If step 2 prints `Realtime client secret created.`, the .NET client successfully requested a short-lived OpenAI Realtime secret through the Spring API.
If step 2-1 prints `Realtime audio played.`, the model generated playable audio and the client streamed it to the PC speaker without writing a `.wav` file.

## Main Files

- `Program.cs`: Console test flow.
- `MyExpressionFriendApiClient.cs`: HTTP API client that stores the access token and adds the Bearer header.
- `Models/CommonModels.cs`: Common API response, login, and selected child DTOs.
- `Models/ScenarioModels.cs`: Unity scenario response DTOs.
- `Models/GameResultModels.cs`: Dialogue and expression game result request DTOs.
