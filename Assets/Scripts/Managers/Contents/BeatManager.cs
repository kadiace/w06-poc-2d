using System;
using UnityEngine;

public class BeatManager
{
    private float _bpm;
    private double _startedAt;
    private double _beatsAtStart;

    public GameInfo GameInfo { get; private set; }

    public float Bpm
    {
        get
        {
            SyncBpm();
            return _bpm;
        }
    }

    public float BeatInterval => 60f / Bpm;
    public double TotalBeats
    {
        get
        {
            SyncBpm();
            return _beatsAtStart + (Time.timeAsDouble - _startedAt) / (60d / _bpm);
        }
    }
    public long BeatCount => (long)Math.Floor(TotalBeats);
    public float BeatProgress => (float)(TotalBeats % 1d);

    public void Init()
    {
        GameInfo = Resources.Load<GameInfo>("Datas/GameInfo");
        Clear();
    }

    public void Clear()
    {
        _bpm = GameInfo.Bpm;
        _startedAt = Time.timeAsDouble;
        _beatsAtStart = 0d;
    }

    private void SyncBpm()
    {
        float bpm = GameInfo.Bpm;
        if (float.IsNaN(bpm) || float.IsInfinity(bpm) || bpm <= 0f)
            throw new ArgumentOutOfRangeException(nameof(GameInfo.Bpm), "BPM must be finite and greater than zero.");
        if (bpm == _bpm)
            return;

        double now = Time.timeAsDouble;
        _beatsAtStart += (now - _startedAt) / (60d / _bpm);
        _startedAt = now;
        _bpm = bpm;
    }
}
