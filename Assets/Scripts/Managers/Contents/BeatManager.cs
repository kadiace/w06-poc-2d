using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct BeatClockTick
{
    public double TotalBeats { get; }
    public long BeatCount => (long)Math.Floor(TotalBeats);
    public double BeatProgress => TotalBeats - BeatCount;
    public float BaseBpm { get; }
    public float TempoMultiplier { get; }
    public float Bpm => BaseBpm * TempoMultiplier;

    public BeatClockTick(double totalBeats, float baseBpm, float multiplier)
    {
        TotalBeats = totalBeats;
        BaseBpm = baseBpm;
        TempoMultiplier = multiplier;
    }
}

public class BeatManager
{
    private float _baseBpm;
    private float _observedBaseBpm;
    private float _tempoMultiplier = 1f;
    private double _startedAt;
    private double _beatsAtStart;
    private readonly SortedDictionary<long, float> _tempoChanges = new SortedDictionary<long, float>();
    private long? _baseChangeBeat;
    private float _pendingBaseBpm;

    public GameInfo GameInfo { get; private set; }

    public float Bpm => GetClockTick().Bpm;
    public float BeatInterval => 60f / Bpm;
    public double TotalBeats => GetClockTick().TotalBeats;
    public long BeatCount => (long)Math.Floor(TotalBeats);
    public float BeatProgress => (float)(TotalBeats % 1d);

    public void Init()
    {
        GameInfo = Resources.Load<GameInfo>("Datas/GameInfo");
        Clear();
    }

    public void Clear()
    {
        _baseBpm = _observedBaseBpm = GameInfo.Bpm;
        _tempoMultiplier = 1f;
        _tempoChanges.Clear();
        _baseChangeBeat = null;
        _startedAt = Time.timeAsDouble;
        _beatsAtStart = 0d;
    }

    public BeatClockTick GetClockTick()
    {
        double now = Time.timeAsDouble;
        AdvanceTo(now);
        float requestedBpm = GameInfo.Bpm;
        ValidatePositive(requestedBpm, nameof(GameInfo.Bpm));
        if (requestedBpm != _observedBaseBpm)
        {
            _observedBaseBpm = _pendingBaseBpm = requestedBpm;
            _baseChangeBeat = (long)Math.Floor(_beatsAtStart) + 1;
        }
        return new BeatClockTick(_beatsAtStart, _baseBpm, _tempoMultiplier);
    }

    public long RequestTempoMultiplier(float multiplier)
    {
        long boundary = GetClockTick().BeatCount + 1;
        ScheduleTempoMultiplier(multiplier, boundary);
        return boundary;
    }

    public void ScheduleTempoMultiplier(float multiplier, long boundaryBeat)
    {
        ValidatePositive(multiplier, nameof(multiplier));
        if (boundaryBeat <= GetClockTick().TotalBeats)
            throw new ArgumentOutOfRangeException(nameof(boundaryBeat), "Tempo changes must be reserved on a future beat.");
        _tempoChanges[boundaryBeat] = multiplier;
    }

    public long ReleaseTempoOverride()
    {
        long boundary = GetClockTick().BeatCount + 1;
        _tempoChanges.Clear();
        _tempoChanges[boundary] = 1f;
        return boundary;
    }

    private void AdvanceTo(double now)
    {
        while (true)
        {
            long? boundary = _baseChangeBeat;
            foreach (long beat in _tempoChanges.Keys)
            {
                if (!boundary.HasValue || beat < boundary.Value) boundary = beat;
                break;
            }
            if (!boundary.HasValue) break;
            double boundaryTime = _startedAt + (boundary.Value - _beatsAtStart) * 60d / (_baseBpm * _tempoMultiplier);
            if (boundaryTime > now) break;
            _startedAt = boundaryTime;
            _beatsAtStart = boundary.Value;
            if (_baseChangeBeat == boundary)
            {
                _baseBpm = _pendingBaseBpm;
                _baseChangeBeat = null;
            }
            if (_tempoChanges.TryGetValue(boundary.Value, out float multiplier))
            {
                _tempoMultiplier = multiplier;
                _tempoChanges.Remove(boundary.Value);
            }
        }
        _beatsAtStart += (now - _startedAt) * (_baseBpm * _tempoMultiplier) / 60d;
        _startedAt = now;
    }

    private static void ValidatePositive(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(name, "Tempo must be finite and greater than zero.");
    }
}
