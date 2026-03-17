# myExpressionFriend — Unity 서버 연동 스크립트

  ## Unity PIN 연동 변경 사항

Unity에서 부모 로그인 후 아동 선택 -> PIN 확인 -> 게임 세션 생성 흐름으로 변경했습니다.

변경된 파일
- `Assets/scripts/ApiModels.cs`
- `Assets/scripts/ChildSessionService.cs`
- `Assets/scripts/GameFlowController.cs`

주요 변경 내용
1. `ApiModels.cs`
- `PinVerificationRequest` DTO 추가
- `/api/children/{childId}/pin/verify-and-start` 요청 바디에 사용

2. `ChildSessionService.cs`
- `VerifyPinAndCreateSession(string childId, string pin, Action<string> onDone)` 추가
- 선택한 아동에 대해 PIN 확인 후 세션 생성 API 호출
- PIN이 비어 있어도 요청을 전송하도록 수정
- PIN 없는 아동은 백엔드에서 바로 세션 생성 가능

3. `GameFlowController.cs`
- Inspector에서 사용할 `pin` 필드 추가
- 세션 생성 시 `CreateGameSession()` 대신 `VerifyPinAndCreateSession()` 호출
- 아동 선택 / PIN 확인 시작 / 성공 / 실패 로그 추가

현재 동작
- 부모 로그인
- 접근 가능한 아동 목록 조회
- 아동 선택
- `/api/children/{childId}/pin/verify-and-start` 호출
- `pinEnabled=true` 아동: PIN 검증 후 세션 생성
- `pinEnabled=false` 아동: PIN 없이 바로 세션 생성
- 세션 생성 후 미션 로드

로그 예시
- `GAME FLOW: login ok=True`
- `GAME FLOW: children loaded count=2`
- `GAME FLOW: child selected index=0, childId=..., name=...`
- `GAME FLOW: pin verify start childId=...`
- `GAME FLOW: pin verify success childId=..., childName=...`
- `GAME FLOW: missions loaded count=2`

  ## 파일 목록

  | 파일 | 역할 |
  |------|------|
  | `ApiClient.cs` | HTTP 통신 기반 (수정 불필요) |
  | `ApiModels.cs` | 요청/응답 데이터 구조 (수정 불필요) |
  | `AuthService.cs` | 로그인 + 토큰 관리 (수정 불필요) |
  | `ChildSessionService.cs` | 아동 목록 조회 + 게임 세션 생성 (수정 불필요) |
  | `MissionService.cs` | 미션 목록 조회 (수정 불필요) |
  | `GameResultService.cs` | 게임 결과 저장 (수정 불필요) |
  | `GameFlowController.cs` | **참고용 샘플** — 기존 게임 매니저에 통합하세요 |
  | `StatusTextView.cs` | 상태 텍스트 UI 헬퍼 (선택) |
  | `MissionDebugView.cs` | 미션 데이터 확인용 디버그 UI (선택) |

  ## Inspector 연결

  1. 빈 GameObject에 위 스크립트를 모두 추가
  2. `AuthService` / `ChildSessionService` / `MissionService` / `GameResultService` 의 **Api Client** 슬롯에 동일한 `ApiClient`
  컴포넌트 연결
  3. `ApiClient` → **Base Url** 에 서버 주소 입력
     - 운영: `(https://api.myexpressionfriend.site)`
  4. `GameFlowController` → **Email / Password** 에 테스트 계정 입력

  ## 통합 포인트 (2곳)

  **1. 게임 시작 시** — 기존 게임 매니저 `Start()` 또는 씬 진입 시점에 추가
  ```csharp
  StartCoroutine(InitializeFlow());

  2. 게임 종료 시 — 미션 결과가 확정된 시점에 호출
  gameFlowController.SaveResult(
      success: true,
      score: 90,
      durationSeconds: 35f,
      retryCount: 0
  );

  미션 데이터 접근

  InitializeFlow() 완료 후 missions[] 배열 사용 가능

  foreach (var mission in missions)
  {
      if (mission.missionTypeString == "EXPRESSION")
      {
          // mission.expression_data.characterDialogue[]
          // mission.expression_data.successFeedback[]
      }
      else // SITUATION
      {
          // mission.situation_data.situationDescription[]
          // mission.situation_data.question
          // mission.situation_data.options[] (id / text / isCorrect / feedback[])
      }
  }

  테스트

  Inspector에서 GameFlowController 우클릭 → Save Mock Result 로 결과 저장 테스트 가능
