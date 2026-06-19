using System;
using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    // 전역 접근을 위한 싱글톤 인스턴스
    public static UserDataManager Instance;

    // 미션 및 친밀도 관리 변수
    public int dailyCompletedCount = 0; // 오늘 완료한 미션 개수 (0~5)
    public int totalCompletedCount = 0; // 지금까지 완료한 총 누적 미션 개수 (해금 로직에 사용)
    public int intimacyLevel = 0;       // 현재 친밀도 레벨

    // 현재 플레이 중인 아동 정보 변수
    public string currentChildId = "";
    public string currentChildName = "";

    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
            return;
        }

        // 앱 실행 시 날짜 초기화 여부 확인
        CheckDailyReset();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ResetAllData();
        }
    }

    public void ResetAllData()
    {
        // 1. 현재 메모리에 들고 있는 변수들을 0으로 초기화
        dailyCompletedCount = 0;
        totalCompletedCount = 0;
        intimacyLevel = 0;

        // 2. 기기에 저장된 기록(PlayerPrefs) 덮어쓰기
        PlayerPrefs.SetInt("TodayStarCount", 0);
        PlayerPrefs.SetInt("TotalCompletedCount", 0);
        PlayerPrefs.SetInt("IntimacyLevel", 0);
        PlayerPrefs.SetString("LastPlayDate", ""); // 접속 날짜 기록도 날림

        PlayerPrefs.Save(); // 기기에 즉시 저장

        Debug.LogWarning("[테스트용] 삐빅! F11이 눌렸습니다. 모든 데이터(별, 미션 개수, 친밀도)가 0으로 초기화되었습니다! (씬을 재시작하면 UI에 반영됩니다)");
    }

    /// <summary>
    /// 미션을 완료했을 때 호출하는 함수
    /// 일일 완료 개수와 누적 완료 개수를 모두 증가시킵니다.
    /// </summary>
    public void AddCompletedMission()
    {
        // 일일 미션은 최대 5개까지만 카운트
        if (dailyCompletedCount < 5)
        {
            dailyCompletedCount++;
            totalCompletedCount++;

            PlayerPrefs.SetInt("TodayStarCount", dailyCompletedCount);
            PlayerPrefs.SetInt("TotalCompletedCount", totalCompletedCount); // 누적 개수 저장
            Debug.Log($"현재 완료 미션: {dailyCompletedCount}/5 (누적: {totalCompletedCount}개)");

            // 하루 목표 5개 달성 시 친밀도 상승
            if (dailyCompletedCount == 5)
            {
                IncreaseIntimacy();
            }

            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 친밀도를 1 증가시키고 저장하는 함수
    /// </summary>
    private void IncreaseIntimacy()
    {
        intimacyLevel++;
        PlayerPrefs.SetInt("IntimacyLevel", intimacyLevel);
        Debug.Log($"오늘의 미션을 모두 완료하여 친밀도가 올랐습니다. 현재 친밀도: {intimacyLevel}");
    }

    /// <summary>
    /// 접속 날짜를 확인하여 하루가 지났으면 일일 미션 진행도를 초기화하는 함수
    /// </summary>
    private void CheckDailyReset()
    {
        string lastPlayDate = PlayerPrefs.GetString("LastPlayDate", "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // 마지막 접속일이 오늘이 아니라면 일일 카운트 초기화
        if (lastPlayDate != today)
        {
            dailyCompletedCount = 0;
            PlayerPrefs.SetInt("TodayStarCount", 0);
            PlayerPrefs.SetString("LastPlayDate", today);
            PlayerPrefs.Save();
        }
        else
        {
            // 오늘 접속한 기록이 있다면 기존 카운트 불러오기
            dailyCompletedCount = PlayerPrefs.GetInt("TodayStarCount", 0);
        }

        // 친밀도 및 누적 완료 개수 불러오기
        intimacyLevel = PlayerPrefs.GetInt("IntimacyLevel", 0);
        totalCompletedCount = PlayerPrefs.GetInt("TotalCompletedCount", 0);
    }
}