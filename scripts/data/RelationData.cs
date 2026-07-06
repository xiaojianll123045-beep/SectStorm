using System;

public enum RelationState { Neutral, Ally, Subordinate, Hostile, War }

public class RelationData
{
    public int SectA;
    public int SectB;
    public int Favor; // -100~100
    public int Fear;  // 0~100
    public int Trust; // 0~100
    public RelationState State = RelationState.Neutral;
    public int TruceTurnsLeft;

    private static Random _rng = new();

    public RelationData(int a, int b)
    {
        SectA = a; SectB = b;
        Favor = _rng.Next(-10, 21);
        Fear = _rng.Next(0, 21);
        Trust = _rng.Next(10, 41);
    }
}
