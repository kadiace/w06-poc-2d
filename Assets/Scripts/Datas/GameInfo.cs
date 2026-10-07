using UnityEngine;

[CreateAssetMenu(fileName = "GameInfo", menuName = "Scriptable Objects/GameInfo")]
public class GameInfo : ScriptableObject
{
    [SerializeField, Min(1f)]
    private float _bpm = 180f;
    [SerializeField, Min(0f), Tooltip("Allowed time before and after the timing center, in seconds.")]
    private float _timingToleranceSeconds = 0.1f;
    [SerializeField, Min(0f)]
    private float _parryToleranceStepSeconds = 0.01f;
    [SerializeField, Min(0f)]
    private float _maxParryToleranceBonusSeconds = 0.05f;

    public float Bpm => _bpm;
    public float TimingToleranceSeconds => _timingToleranceSeconds;
    public float ParryToleranceStepSeconds => _parryToleranceStepSeconds;
    public float MaxParryToleranceBonusSeconds => _maxParryToleranceBonusSeconds;
}
