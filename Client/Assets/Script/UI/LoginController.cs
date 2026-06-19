using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; // 씬 전환용
using System.Collections; // 코루틴용

public class LoginController : MonoBehaviour
{
    // ==========================================
    // UI 컴포넌트 변수
    // ==========================================
    private TextField _idInput;
    private TextField _pwInput;
    private Button _loginButton;
    private Label _errorLabel;

    void Start()
    {
        // 1. 시작하자마자 UI 요소들을 먼저 세팅해둡니다.
        InitializeUI();

        // 2. 기기에 저장된 토큰이 있는지 확인하고 자동 로그인을 시도합니다.
        CheckAndProcessAutoLogin();
    }

    // ==========================================
    // 기능 1: 자동 로그인 및 토큰 검증 로직
    // ==========================================
    /// <summary>
    /// 저장된 토큰으로 서버에 유효성 검사(/api/unity/selected-child)를 요청하는 함수
    /// </summary>
    private void CheckAndProcessAutoLogin()
    {
        string savedToken = PlayerPrefs.GetString("AuthToken", "");

        // 저장된 토큰이 없다면 바로 수동 로그인 대기 상태로 유지
        if (string.IsNullOrEmpty(savedToken))
        {
            return;
        }

        Debug.Log("저장된 토큰 발견! 서버에 유효성 검사를 요청합니다...");

        // NetworkManager에 토큰 저장
        NetworkManager.Instance.SetToken(savedToken);

        // 검사하는 동안 사용자가 버튼을 누르지 못하도록 UI 상태 변경
        _loginButton.text = "자동 로그인 중...";
        _loginButton.SetEnabled(false);

        // 유효성 검사 및 아동 정보 조회 API 호출
        StartCoroutine(NetworkManager.Instance.GetRequest("/api/unity/selected-child",
            onComplete: (jsonResponse) =>
            {
                // [검증 성공] 토큰이 유효함
                Debug.Log("토큰 유효성 검사 성공! 응답: " + jsonResponse);

                // 1. JSON 데이터를 C# 객체로 파싱
                var response = JsonUtility.FromJson<SelectedChildApiResponse>(jsonResponse);

                // 2. 파싱 성공 및 데이터가 정상적으로 있다면 UserDataManager에 저장
                if (response != null && response.success && response.data != null)
                {
                    UserDataManager.Instance.currentChildId = response.data.childId;
                    UserDataManager.Instance.currentChildName = response.data.childName;
                    Debug.Log($"플레이 아동 세팅 완료: 이름({response.data.childName}), ID({response.data.childId})");
                }
                else
                {
                    Debug.LogWarning("아동 정보를 파싱하지 못했거나 서버에 선택된 아동이 없습니다.");
                }

                // 3. 데이터 저장이 끝났으니 메인 씬으로 이동
                GoToMainScene();
            },
            onError: (errorMsg) =>
            {
                // [검증 실패] 토큰이 만료되었거나 권한(403) 에러 등
                Debug.LogWarning("토큰이 만료되었거나 유효하지 않습니다. 다시 로그인해주세요.");

                // 쓸모없는 토큰 폐기
                PlayerPrefs.DeleteKey("AuthToken");
                NetworkManager.Instance.SetToken("");

                // UI를 다시 수동 로그인 모드로 복구
                _errorLabel.text = "로그인이 만료되었습니다. 다시 로그인해주세요.";
                _loginButton.text = "로그인 하기";
                _loginButton.SetEnabled(true);
            }
        ));
    }

    // ==========================================
    // 기능 2: UI 초기화 및 세팅
    // ==========================================
    private void InitializeUI()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _idInput = root.Q<TextField>("IdInput");
        _pwInput = root.Q<TextField>("PwInput");
        _loginButton = root.Q<Button>("LoginButton");

        _errorLabel = new Label("");
        _errorLabel.style.color = new StyleColor(Color.red);
        _errorLabel.style.alignSelf = Align.Center;
        _errorLabel.style.marginBottom = 10;
        _errorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        _loginButton.parent.Insert(_loginButton.parent.IndexOf(_loginButton), _errorLabel);

        _loginButton.clicked += OnLoginButtonClicked;
    }

    // ==========================================
    // 기능 3: 수동 로그인 버튼 클릭 처리
    // ==========================================
    private void OnLoginButtonClicked()
    {
        _errorLabel.text = "";
        _loginButton.text = "로그인 중...";
        _loginButton.SetEnabled(false);

        NetworkManager.Instance.Login(_idInput.value, _pwInput.value, (isSuccess) =>
        {
            if (isSuccess)
            {
                // 로그인 성공 시 토큰 저장 후 바로 아동 정보 조회로 넘겨서 검증 진행
                PlayerPrefs.SetString("AuthToken", NetworkManager.Instance.GetToken());

                // 씬을 바로 넘기지 않고, 방금 만든 자동 로그인 로직을 재활용해서 아동 정보를 가져오고 넘어감
                CheckAndProcessAutoLogin();
            }
            else
            {
                _errorLabel.text = "아이디 또는 비밀번호를 다시 확인해주세요.";
                _pwInput.value = "";
                _loginButton.text = "로그인 하기";
                _loginButton.SetEnabled(true);
            }
        });
    }

    // ==========================================
    // 기능 4: 씬 전환
    // ==========================================
    private void GoToMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }
}