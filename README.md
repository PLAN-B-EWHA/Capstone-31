- ApiClient는 씬에 1개만 둔다
- ScenarioRawJsonLoader의 apiClient 필드에 ApiClient를 연결한다.
- 호출하는 기존 스크립트의 loader 필드에도 같은 ScenarioRawJsonLoader를 연결한다.
- baseUrl은 https://api.myexpressionfriend.site


# 스크립트 안내 (HTTP + Raw JSON 전용)

현재 `Assets/scripts`는 의도적으로 아래 2개 런타임 스크립트만 사용하는 구조입니다.

- `ApiClient.cs`
- `ScenarioRawJsonLoader.cs`

로그인/아동선택/세션/디버그 뷰 같은 나머지 흐름은 헷갈리지 않도록 제거했습니다.

## 1) 각 스크립트 역할

### `ApiClient.cs`

`ApiClient`는 공통 HTTP 통신 유틸입니다.

주요 역할:

- 백엔드 기본 주소(`baseUrl`) 보관
- `baseUrl + path`로 최종 URL 생성
- `GET` 요청 전송
- JSON 바디를 담은 `POST` 요청 전송
- 응답 본문을 문자열 그대로 콜백으로 반환

중요: `ApiClient`는 도메인(JSON 구조) 파싱을 하지 않습니다.

### `ScenarioRawJsonLoader.cs`

`ScenarioRawJsonLoader`는 시나리오 엔드포인트를 호출하는 전용 로더입니다.

주요 역할:

- `GET /api/unity/scenarios?week={week}` 호출
- 응답 본문을 Raw 문자열로 받음
- 받은 문자열을
  - 콘솔(`Debug.Log`)
  - 선택적 TMP 텍스트(`outputText`)
  에 출력

중요: DTO 역직렬화 없이 Raw JSON 그대로 다룹니다.

## 2) 전체 호출 흐름

`loadOnStart == true`일 때 실행 순서:

1. Unity가 `ScenarioRawJsonLoader.Start()` 호출
2. `StartCoroutine(LoadWeekJson())` 실행
3. `LoadWeekJson()`에서 `apiClient` 연결 여부 확인
4. `apiClient.Get($"/api/unity/scenarios?week={week}", callback)` 호출
5. `ApiClient.Get()` 내부에서:
   - `BuildUrl()`로 최종 URL 생성
   - `UnityWebRequest.Get(url)` 요청 전송
   - 완료 후 `responseCode`, `downloadHandler.text` 반환
6. `ScenarioRawJsonLoader` 콜백에서:
   - `code`(HTTP 상태코드)
   - `body`(응답 본문 문자열) 수신
7. 성공(2xx) 시 `responseText = body`
8. `SetOutput(responseText)`로 UI 텍스트 출력(선택)

## 3) 메서드 상세 설명

## `ApiClient.cs`

### `Awake()`

- `ValidateBaseUrl()` 실행
- URL이 비었거나 `http://`, `https://`가 아니면 경고 출력

### `Get(string path, Action<long, string> onDone)`

- `BuildUrl(path)`로 URL 생성
- `UnityWebRequest.Get(url)` 전송
- 완료 후 `onDone(responseCode, responseBodyText)` 호출
- HTTP 에러여도 콜백은 호출됩니다

### `PostJson<TReq>(string path, TReq body, Action<long, string> onDone)`

- `JsonUtility.ToJson(body)`로 바디 직렬화
- `Content-Type: application/json`으로 POST 전송
- 완료 후 `responseCode`, 응답 본문 문자열 반환

### `BuildUrl(string path)`

- `baseUrl` 유효성 확인
- `CombineUrl()`로 안전하게 조합

### `CombineUrl(string root, string path)`

- `root` 끝의 `/` 제거
- `path` 시작의 `/` 제거
- `root/path` 형태로 결합

### `ValidateBaseUrl()`

- 실행 차단은 하지 않고 경고만 출력
- Inspector 설정 실수 조기 발견용

## `ScenarioRawJsonLoader.cs`

### `Start()`

- `loadOnStart == true`이면 시작 시 자동 1회 로드

### `Reload()`

- 수동 재호출 메서드
- 버튼 `OnClick`에 연결하기 좋음

### `LoadWeekJson()`

- 핵심 시나리오 호출 코루틴
- `apiClient == null`이면 즉시 실패 처리
- `/api/unity/scenarios?week={week}` 호출
- 2xx가 아니면:
  - `Request failed: code=...` 메시지 생성
  - 에러 로그 출력
- 2xx면:
  - `body`를 그대로 `responseText`에 저장
  - 콘솔에 출력
- 마지막에 `SetOutput()`으로 TMP 출력

### `SetOutput(string text)`

- `outputText`가 연결되어 있을 때만 텍스트 표시
- 비어 있으면 조용히 종료

## 4) Inspector 최소 연결 방법

예시 오브젝트 2개를 만듭니다.

1. `NetworkManager`
2. `ScenarioLoader`

컴포넌트 연결:

- `NetworkManager` -> `ApiClient`
- `ScenarioLoader` -> `ScenarioRawJsonLoader`

필드 설정:

- `ScenarioRawJsonLoader.apiClient`에 `NetworkManager(ApiClient)` 드래그
- `ApiClient.baseUrl` 입력
  - 로컬: `http://localhost:8080`
  - 서버: `https://...`
- `ScenarioRawJsonLoader.week` 입력 (예: `1`)
- `outputText`는 필요할 때만 연결

## 5) 여기서 JSON은 정확히 무엇인가?

핵심 코드:

```csharp
responseText = body;
```

여기서 `body`는 `ApiClient`의 `req.downloadHandler.text`입니다.

즉:

- `body` = 서버 응답 본문 문자열
- 엔드포인트가 JSON을 반환하면 `body`는 JSON 문자열
- `responseText`는 그 JSON 문자열을 그대로 담는 변수

## 6) 자주 나는 문제

### `ApiClient is not assigned`

원인:

- `ScenarioRawJsonLoader.apiClient` 미연결

해결:

- `ApiClient`가 붙은 오브젝트를 해당 필드에 연결

### `ApiClient baseUrl is empty`

원인:

- `ApiClient.baseUrl` 공란

해결:

- `http://` 또는 `https://` 포함한 전체 주소 입력

### 2xx가 아닌 응답 코드 발생

원인:

- 경로 오타
- 서버 미기동
- 네트워크/CORS/프록시 이슈

해결:

- URL, 서버 상태 점검
- Postman/curl로 동일 요청 테스트

## 7) 현재 범위 (포함/제외)

현재 포함:

- 공통 HTTP `GET`/`POST`
- 주차 기준 시나리오 Raw JSON 조회

현재 제외:

- 로그인
- 아동 선택
- PIN 검증
- 세션 토큰 처리
- DTO 기반 파싱
- 게임 결과 전송 플로우

지금은 통신 최소 검증 목적이라 의도적으로 단순화한 상태입니다.
