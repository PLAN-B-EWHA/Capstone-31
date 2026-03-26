using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    // Inspector에서 연결할 UXML 파일들
    [Header("UI Screen Assets")]
    public VisualTreeAsset _mainLobbyAsset; // 메인로비
    public VisualTreeAsset _expressionMissionAsset; // 표정 학습
    public VisualTreeAsset _situationMissionAsset; // 상황 학습


    [Header("Camera Anchors")]
    public Camera renderCamera; // Vroid를 비추고 있는 카메라
    public Transform lobbyAnchor; // 전신 카메라 pos
    public Transform faceAnchor; // 얼굴 카메라 pos

    // 화면이 교체될 빈 컨테이너
    private VisualElement _contentArea;

    // 공통 버튼 있는 탑바
    private VisualElement _topBar;

    // WebCamController
    private WebCamController webCamController;

    // 화면별 Controller
    private MainLobbyController mainLobbyController;
    private ExpressionMissionController expressionMissionController;
    private SituationMissionController situationMissionController;

    // 공통 버튼 Controller
    private CommonButton commonButton;

    void Awake()
    {
        // 화면별 Controller 객체 생성
        mainLobbyController = GetComponent<MainLobbyController>();
        expressionMissionController = GetComponent<ExpressionMissionController>();
        situationMissionController = GetComponent<SituationMissionController>();
        
        // 공통 버튼 Controller 객체 생성
        commonButton = GetComponent<CommonButton>();

        // WebCamController 객체 생성
        webCamController = GetComponent<WebCamController>();
    }
    void Start()
    { 
        // 빈 컨테이너
        var _root = GetComponent<UIDocument>().rootVisualElement;
        _contentArea = _root.Q<VisualElement>("ContentArea");
        _topBar = _root.Q<VisualElement>("TopBar");


        // 공통 버튼 초기화
        commonButton.InitCommonButton(_topBar);

        // 시작시 앱 시작 시 MainLobby 호출 및 카메라 위치 설정
        LoadScreen(_mainLobbyAsset);
        SetCameraTransform(lobbyAnchor);
    }

    // 카메라 위치 옮기는 함수
    private void SetCameraTransform(Transform anchor)
    {
        if (renderCamera == null || anchor == null)
        {
            Debug.Log("렌더링 카메라 또는 앵커 지점이 없습니다.");
            return;
        }
        
        // 카메라 pos 설정
        renderCamera.transform.position = anchor.position;
        renderCamera.transform.rotation = anchor.rotation;
    }



    // 화면 로드 함수
    public void LoadScreen(VisualTreeAsset screenAsset, MissionData missionData = null)
    {
        if (screenAsset == null)
        {
            Debug.Log("로드할 화면이 없습니다.");
            return;
        } 

        // 기존 ContentArea 내부의 모든 UI 요소 삭제
        _contentArea.Clear();

        // 새로운 UXML 에셋을 복제해 contentArea의 자식으로 추가
        screenAsset.CloneTree(_contentArea);
        
        // contentArea의 자식 요소 중 첫번째 인덱스
        var instantiatedScreen = _contentArea.ElementAt(0);

        // 생성된 최상위 화면이 빈 공간을 꽉 채우도록 강제 팽창
        instantiatedScreen.style.flexGrow = 1;

        // 로드된 화면에 따라 세팅 함수 호출 및 버튼 이벤트 연결
        if (screenAsset == _mainLobbyAsset)
        {
            SettingMainLobby();
            mainLobbyController.InitializeUI(instantiatedScreen);
        }
        else if (screenAsset == _expressionMissionAsset)
        {
            SettingExpressionMission();
            expressionMissionController.InitializeUI(instantiatedScreen, missionData);
        }
        else if (screenAsset == _situationMissionAsset)
        {
            SettingSituationMission();
            situationMissionController.InitializeUI(instantiatedScreen, missionData);
        }
    }

    // MainLobby 화면 세팅 함수
    private void SettingMainLobby()
    {
        // 카메라 위치 변경
        SetCameraTransform(lobbyAnchor);

        // 웹캠 끄기
        if (webCamController != null)
            webCamController.StopCamera();

        // 메인로비 바텀 시트(미션 리스트) 활성화
        gameObject.GetComponent<MissionSheetController>().enabled = true;
    }

    // ExpressionMission 화면 세팅 함수
    private void SettingExpressionMission()
    {
        // 카메라 위치 변경
        SetCameraTransform(faceAnchor);

        // 웹캠 활성화
        webCamController.StartCamera();
    }

    // SituationMisssion 화면 세팅 함수
    private void SettingSituationMission()
    {
        // 웹캠 끄기
        if (webCamController != null)
            webCamController.StopCamera();
    }
}