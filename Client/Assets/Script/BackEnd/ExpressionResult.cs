using System;
using System.Collections.Generic;

[Serializable]
public class ExpressionResultSaveRequest
{
    // public string child_id; <-- [제거됨] 백엔드가 토큰으로 알아서 식별합니다.
    public string emotion_target;
    public string started_at;
    public string ended_at;
    public float final_accuracy;
    public bool is_success;
    public List<TryDTO> tries;
}

[Serializable]
public class TryDTO
{
    public int try_number;
    public int duration_ms;
    public float accuracy_score;
    public bool is_success;
}