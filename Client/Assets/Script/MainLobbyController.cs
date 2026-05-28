using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.UIElements;

public class MainLobbyController : MonoBehaviour
{
    // _contentArea의 첫번째 VisualElement
    private VisualElement _myLobbyRoot;

    private List<MissionData> _dailyMissions;

    private UIManager uiManager;
    private MissionSheetController missionSheetController;

    void Awake()
    {
        uiManager = GetComponent<UIManager>();
        missionSheetController = GetComponent<MissionSheetController>();

        // API 연동 전 테스트용 데이터
        CreateMockData();
    }

    private void CreateMockData()
    {
        // 중복 생성 방지 로직
        if (_dailyMissions != null)
            return;

        // "MockDat.json" 파일을 텍스트 에셋으로 불러옴
        TextAsset jsonFile = Resources.Load<TextAsset>("MockData");

        if(jsonFile == null)
        {
            Debug.LogError("Resource 폴더에서 MockData.json 파일을 찾을 수 없습니다!");
            return;
        }

        // 파일 안의 텍스트를 C# 객체로 파싱
        MissionResponse response = JsonUtility.FromJson<MissionResponse>(jsonFile.text);

        _dailyMissions = response.missions;
        Debug.Log($"외부 파일에서 {_dailyMissions.Count}개의 미션 데이터를 성공적으로 불러왔습니다.");
    }

    // UIManager가 화면을 로드한 직후 호출할 UI 초기화 함수
    public void InitializeUI(VisualElement root)
    {
        // 넘겨받은 VisualElement를 _myLobbyRoot에 저장
        _myLobbyRoot = root;

        // 버튼 찾고 기능 연결
        BindMissionButton();
        missionSheetController.InitBottomSheet(_myLobbyRoot);

    }

    // 버튼 바인드 함수
    private void BindMissionButton()
    {
        // 버튼과 MockData 바인드
        for (int i = 0; i < _dailyMissions.Count; i++)
        {
            // 현재 인덱스에 맞는 데이터와 버튼 요소 찾기
            var missionData = _dailyMissions[i];
            var btn = _myLobbyRoot.Q<Button>($"Button_Mission{i+1}");

            if (btn != null)
            {
                // 버튼 텍스트를 missionData의 missionName로 변경 
                btn.text = missionData.missionName;

                // 기존 콜백 치기화 후 새 콜백 등록
                btn.UnregisterCallback<ClickEvent>(evt => OnMissionButtonClicked(missionData));
                btn.RegisterCallback<ClickEvent>(evt => OnMissionButtonClicked(missionData));
            
            }
        }
    }

    // 버튼 클릭 시 실행될 동적 라우팅 함수
    private void OnMissionButtonClicked(MissionData data)
    {
        Debug.Log($"선택한 미션: {data.missionName}");

        if (data.ParsedMissionType == MissionType.Expression)
        {
            // 로드할 화면과 미션 데이터 파라미커로 전달  
            uiManager.LoadScreen(uiManager._expressionMissionAsset, data);
        }else if (data.ParsedMissionType == MissionType.Situation)
        {
            // TODO: situation 화면으로 넘어가기 전에 targetKeyword 데이터 전달해야함.
            uiManager.LoadScreen(uiManager._situationMissionAsset,data);
        }
    }

}
