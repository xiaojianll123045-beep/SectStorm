using Godot;
using System;
using System.Collections.Generic;

public static class ModEventBus
{
    public enum EventType
    {
        GameStart, GameLoad, GameSave,
        MonthBegin, MonthEnd,
        TerritoryChanged, DiplomaticChanged,
        DiscipleJoined, DiscipleLeft, DiscipleAdvanced,
        Custom
    }

    private static readonly Dictionary<EventType, List<Action>> _listeners = new();

    public static void Listen(EventType type, Action handler)
    {
        if (!_listeners.ContainsKey(type))
            _listeners[type] = new List<Action>();
        _listeners[type].Add(handler);
    }

    public static void Unlisten(EventType type, Action handler)
    {
        if (_listeners.TryGetValue(type, out var list))
            list.Remove(handler);
    }

    public static void Fire(EventType type)
    {
        if (_listeners.TryGetValue(type, out var list))
            foreach (var h in list)
                try { h(); } catch (Exception e) { GD.PrintErr($"[ModEventBus] Error in {type}: {e.Message}"); }
    }

    // Custom events
    private static readonly Dictionary<string, List<Action<Variant>>> _customListeners = new();

    public static void ListenCustom(string key, Action<Variant> handler)
    {
        if (!_customListeners.ContainsKey(key))
            _customListeners[key] = new List<Action<Variant>>();
        _customListeners[key].Add(handler);
    }

    public static void FireCustom(string key, Variant data = default)
    {
        if (_customListeners.TryGetValue(key, out var list))
            foreach (var h in list)
                try { h(data); } catch (Exception e) { GD.PrintErr($"[ModEventBus] Custom {key}: {e.Message}"); }
    }

    public static void Clear()
    {
        _listeners.Clear();
        _customListeners.Clear();
    }
}
