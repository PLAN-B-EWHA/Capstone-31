using System;
using System.Collections.Generic;

[Serializable]
public class DialogueResultSaveRequest
{
    // public string child_id; <-- [제거됨] 
    // public string scenario_source; <-- [제거됨] 담당자 요청으로 삭제
    // public int week_number; <-- [제거됨] Swagger 명세서에 없어 삭제

    public string scenario_id; // 어떤 시나리오를 깼는지 식별하기 위해 필수!
    public string theme;
    public string started_at;
    public string ended_at;
    public int total_score;
    public int max_score;
    public List<TurnDTO> turns;
}

[Serializable]
public class TurnDTO
{
    public int turn_id;
    public int selected_option_order;
    public int selected_score;
}