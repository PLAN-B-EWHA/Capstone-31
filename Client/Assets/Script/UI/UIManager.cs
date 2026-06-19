using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 게임의 전체 화면(UI Document) 전환을 관리하고, 
/// 화면에 따른 카메라 위치 지정 및 백그라운드 리소스(MediaPipe)를 제어하는 중앙 관리자
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Screen Assets")]
    public VisualTreeAsset _mainLobbyAsset;
    public VisualTreeAsset _situationMissionAsset;
    public VisualTreeAsset _expressionMissionAsset;

    [Header("Camera Anchors")]
    public Camera renderCamera;
    public Transform lobbyAnchor;
    public Transform faceAnchor;
    public Transform situationAnchor;

    [Header("MediaPipe System")]
    [Tooltip("MediaPipe 전체 시스템(활성화 시 CPU 점유). 로비나 상황극에서 끄기 위해 캐싱합니다.")]
    public GameObject mediaPipeRoot;

    private VisualElement _contentArea;
    private VisualElement _topBar;

    // 각 컨트롤러 캐싱
    private MainLobbyController mainLobbyController;
    private SituationMissionController situationMissionController;
    private ExpressionMissionController expressionMissionController;
    private CommonButton commonButton;

    void Awake()
    {
        // 컨트롤러 초기화. 한 번만 Get하여 성능 확보
        mainLobbyController = GetComponent<MainLobbyController>();
        situationMissionController = GetComponent<SituationMissionController>();
        expressionMissionController = GetComponent<ExpressionMissionController>();
        commonButton = GetComponent<CommonButton>();
    }

    void Start()
    {
        var _root = GetComponent<UIDocument>().rootVisualElement;
        _contentArea = _root.Q<VisualElement>("ContentArea");
        _topBar = _root.Q<VisualElement>("TopBar");

        if (commonButton != null)
            commonButton.InitCommonButton(_topBar);

        // 앱 시작 시 메인 로비 로드
        LoadScreen(_mainLobbyAsset);
    }

    /// <summary>
    /// 3D 렌더링 카메라의 위치를 각 씬(Anchor)에 맞게 이동
    /// </summary>
    private void SetCameraTransform(Transform anchor)
    {
        if (renderCamera == null || anchor == null) return;
        renderCamera.transform.position = anchor.position;
        renderCamera.transform.rotation = anchor.rotation;
    }

    /// <summary>
    /// UXML 에셋을 읽어 화면을 교체하고, 해당 씬에 맞는 컨트롤러를 초기화
    /// </summary>
    public void LoadScreen(VisualTreeAsset screenAsset, ScenarioData scenarioData = null, ExpressionMission expressionData = null)
    {
        if (screenAsset == null || _contentArea == null) return;

        // 화면 덮어씌우기
        _contentArea.Clear();
        screenAsset.CloneTree(_contentArea);

        // 만약 Clone 과정에서 요소가 생성되었다면 Flex 비율 1로 꽉 채움
        if (_contentArea.childCount > 0)
        {
            var instantiatedScreen = _contentArea.ElementAt(0);
            instantiatedScreen.style.flexGrow = 1;

            if (commonButton != null) commonButton.UpdateStarUI();

            // 분기에 따른 화면별 초기화 세팅
            if (screenAsset == _mainLobbyAsset)
            {
                SettingMainLobby();
                mainLobbyController.InitializeUI(instantiatedScreen);
            }
            else if (screenAsset == _situationMissionAsset)
            {
                SettingSituationMission();
                situationMissionController.InitializeUI(instantiatedScreen, scenarioData);
            }
            else if (screenAsset == _expressionMissionAsset)
            {
                SettingExpressionMission();
                expressionMissionController.InitializeUI(instantiatedScreen, expressionData);
            }
        }
        else
        {
            Debug.LogError("[UIManager] UXML 로드에 실패했습니다. 파일이 비어있는지 확인하세요.");
        }
    }

    /// <summary>
    /// 메인 로비 입장 시 세팅
    /// </summary>
    private void SettingMainLobby()
    {
        SetCameraTransform(lobbyAnchor);

        // 바텀 시트 활성화
        var sheetController = GetComponent<MissionSheetController>();
        if (sheetController != null) sheetController.enabled = true;
    }

    /// <summary>
    /// 상황극 미션 입장 시 세팅
    /// </summary>
    private void SettingSituationMission()
    {
        SetCameraTransform(situationAnchor);

        // 훈련 중에는 바텀 시트를 끌어올릴 수 없도록 비활성화
        var sheetController = GetComponent<MissionSheetController>();
        if (sheetController != null) sheetController.enabled = false;
    }

    /// <summary>
    /// 표정 미션 입장 시 세팅
    /// </summary>
    private void SettingExpressionMission()
    {
        SetCameraTransform(faceAnchor);

        // [중요] 여기서는 mediaPipeRoot를 끄지 않습니다!
        // 카메라를 켜고 끄는 상세 제어권은 ExpressionMissionController의 내부 State가 통제합니다.

        var sheetController = GetComponent<MissionSheetController>();
        if (sheetController != null) sheetController.enabled = false;
    }
}