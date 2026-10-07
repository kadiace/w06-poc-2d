using UnityEngine;

[CreateAssetMenu(fileName = "GameInfo", menuName = "Scriptable Objects/GameInfo")]
public class GameInfo : ScriptableObject
{
    [SerializeField, Min(1f)]
    private float _bpm = 180f;
    [SerializeField, Min(0f), Tooltip("Allowed time before and after the timing center, in seconds.")]
    private float _timingToleranceSeconds = 0.1f;

    public float Bpm => _bpm;
    public float TimingToleranceSeconds => _timingToleranceSeconds;
}
