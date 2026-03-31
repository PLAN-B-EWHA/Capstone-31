# Unity 시나리오 불러오기 가이드


현재 기준 흐름은 아래와 같습니다.

`GameFlowController -> ScenarioService -> ApiClient -> Spring 서버`

서버 응답 JSON은 `ApiModels.cs`의 DTO로 파싱됩니다.

## 핵심 파일

### 1. [ApiModels.cs](/C:/Users/ttutti/myExpressionFriend_NW/Assets/scripts/ApiModels.cs)

백엔드 요청/응답 DTO 정의 파일입니다.

시나리오 조회에서 중요한 타입:

- `ScenarioListEnvelope`
- `ScenarioDetailEnvelope`
- `ScenarioPayload`
- `ScenarioMetadata`
- `DialogueTurnPayload`
- `DialogueOptionPayload`

백엔드 JSON 구조를 확인할 때 이 파일을 먼저 보면 됩니다.

### 2. [ApiClient.cs](/C:/Users/ttutti/myExpressionFriend_NW/Assets/scripts/ApiClient.cs)

공통 HTTP 클라이언트입니다.

역할:

- `baseUrl` 기준으로 실제 URL 생성
- GET / POST 요청 전송
- 로그인 후 access token 자동 헤더 부착
- 401 발생 시 재로그인 재시도

시나리오만 붙일 때는 내부 구현을 깊게 알 필요는 없고,
Inspector에서 `baseUrl`만 올바르게 넣으면 됩니다.

예:

- 로컬 개발: `http://localhost:8080`

### 3. [ScenarioService.cs](/C:/Users/ttutti/myExpressionFriend_NW/Assets/scripts/ScenarioService.cs)

시나리오 API 전용 서비스입니다.

중요 메서드:

- `FetchScenariosForWeek(int week, Action<ScenarioPayload[]> onDone)`
- `FetchScenario(string scenarioId, Action<ScenarioPayload> onDone)`

현재 Unity에서 주로 쓰는 API:

- `GET /api/unity/scenarios?week={week}`

즉, 주차별 시나리오 배열을 받아오는 책임은 이 파일에 있습니다.

### 4. [GameFlowController.cs](/C:/Users/ttutti/myExpressionFriend_NW/Assets/scripts/GameFlowController.cs)

언제 시나리오를 불러올지 결정하는 상위 흐름 컨트롤러입니다.

시나리오 확인만 급할 때는 이 옵션이 핵심입니다.

- `loadScenariosWithoutLogin = true`

이 값을 켜면:

- 로그인 생략
- 아동 선택 생략
- 바로 `ScenarioService.FetchScenariosForWeek()` 호출

즉 백엔드 시나리오 API만 살아 있으면 바로 Unity 화면에서 확인할 수 있습니다.

### 5. [ScenarioDebugView.cs](/C:/Users/ttutti/myExpressionFriend_NW/Assets/scripts/ScenarioDebugView.cs)

시나리오를 화면에 보여주는 디버그 UI입니다.

역할:

- 받아온 `ScenarioPayload[]`를 내부에 저장
- 첫 시나리오부터 표시
- `ShowPrev()` / `ShowNext()`로 시나리오 넘김
- `pageText`가 연결되어 있으면 `1 / N` 표시

버튼을 이용해 week 안의 여러 시나리오를 하나씩 확인할 때 사용합니다.

## 시나리오 불러오기 흐름

로그인 없이 시나리오만 확인할 때:

1. `GameFlowController.Start()`
2. `InitializeFlow()`
3. `loadScenariosWithoutLogin == true` 확인
4. `LoadScenariosOnly()` 진입
5. `ScenarioService.FetchScenariosForWeek(scenarioWeek, ...)` 호출
6. `ApiClient.Get("/api/unity/scenarios?week=...")`
7. 응답 JSON을 `ScenarioPayload[]`로 파싱
8. `ScenarioDebugView.ShowScenarios(scenarios)` 호출
9. 화면에 첫 시나리오 출력
10. `Prev` / `Next` 버튼으로 다른 시나리오 확인

## Unity Inspector 연결

### 필수 연결

#### ApiClient

- `Base Url`
  예: `http://localhost:8080`

#### ScenarioService

- `Api Client` 슬롯에 `ApiClient` 연결

#### GameFlowController

- `Scenario Service` 슬롯에 `ScenarioService` 연결
- `Scenario Debug View` 슬롯에 `ScenarioDebugView` 연결
- `Load Scenarios Without Login` 체크
- `Scenario Week`에 원하는 주차 입력

#### ScenarioDebugView

- `Scenario Text`에 본문 표시용 `TextMeshProUGUI` 연결
- `Scroll Rect`에 시나리오 스크롤뷰의 `ScrollRect` 연결
- `Page Text`는 선택사항

## ScrollView + 버튼 연결 예시

Hierarchy 예시:

- `Scenario`
- `Viewport`
- `Content`
- `Text (TMP)`
- `Next`
- `Prev`

권장 연결:

- `ScenarioDebugView`는 `Scenario` 오브젝트에 붙이기
- `scenarioText` = `Text (TMP)`
- `scrollRect` = `Scenario`의 `ScrollRect`
- `GameFlowController.scenarioDebugView` = `Scenario`

버튼 연결:

- `Next` 버튼 `OnClick` -> `Scenario` -> `ScenarioDebugView.ShowNext()`
- `Prev` 버튼 `OnClick` -> `Scenario` -> `ScenarioDebugView.ShowPrev()`

## 백엔드 API

현재 Unity 시나리오 조회용 엔드포인트:

- `GET /api/unity/scenarios?week=1`
- `GET /api/unity/scenarios/{scenarioId}`

현재 조회 API는 Unity 런타임용 공개 경로로 열려 있어서,
로그인 없는 디버그 확인이 가능합니다.

## 가장 자주 나는 문제

### 1. `ScenarioService ApiClient가 연결되지 않았습니다.`

원인:

- `ScenarioService`의 `Api Client` 슬롯이 비어 있음

### 2. `ScenarioDebugView scenarioText가 연결되지 않았습니다.`

원인:

- `ScenarioDebugView`의 `Scenario Text` 슬롯이 비어 있음

### 3. 로그인 시 `Invalid character found in method name`

원인:

- `https://localhost:8080`처럼 HTTPS로 호출했는데 서버는 HTTP로 떠 있음

해결:

- 로컬이면 보통 `http://localhost:8080` 사용

### 4. 한글이 네모로 나옴

원인:

- TMP 폰트 에셋에 한글 글리프 없음

해결:

- 한글 지원 TMP Font Asset으로 교체

## 최소로 보면 되는 파일

통합 관점에서는 아래 3개를 가장 먼저 보면 됩니다.

- [ApiModels.cs]
- [ScenarioService.cs]
- [GameFlowController.cs]
