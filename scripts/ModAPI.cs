using Godot;
using System;
using System.Collections.Generic;

public static class ModAPI
{
    public static bool Initialized { get; private set; }

    public static void Init()
    {
        if (Initialized) return;
        Initialized = true;
        GD.Print("[ModAPI] initialized");
    }

    // ===== Hook System =====
    public enum GameHook
    {
        BeforeMonthEnd, AfterMonthEnd,
        BeforeTerritoryUpdate, AfterTerritoryUpdate,
        OnGameStart, OnGameLoad, OnGameSave,
    }

    private static readonly Dictionary<GameHook, List<Func<bool>>> _cancelHooks = new();
    private static readonly Dictionary<GameHook, List<Action>> _actionHooks = new();

    public static void RegisterHook(GameHook hook, Action handler)
    {
        if (!_actionHooks.ContainsKey(hook)) _actionHooks[hook] = new();
        _actionHooks[hook].Add(handler);
    }

    public static void RegisterCancellableHook(GameHook hook, Func<bool> handler)
    {
        if (!_cancelHooks.ContainsKey(hook)) _cancelHooks[hook] = new();
        _cancelHooks[hook].Add(handler);
    }

    public static bool FireHooks(GameHook hook)
    {
        if (_cancelHooks.TryGetValue(hook, out var cancel))
            foreach (var h in cancel)
                if (h()) return true; // cancelled
        if (_actionHooks.TryGetValue(hook, out var actions))
            foreach (var h in actions)
                try { h(); } catch (Exception e) { GD.PrintErr($"[ModAPI] hook {hook}: {e.Message}"); }
        return false;
    }

    // ===== Feature Gating =====
    private static readonly HashSet<string> _disabledFeatures = new();

    public static void DisableFeature(string key) => _disabledFeatures.Add(key);
    public static void EnableFeature(string key) => _disabledFeatures.Remove(key);
    public static bool IsFeatureEnabled(string key) => !_disabledFeatures.Contains(key);

    // ===== Monthly Callbacks =====
    private static readonly List<Action> _monthlyCallbacks = new();

    public static void RegisterMonthlyCallback(Action cb) => _monthlyCallbacks.Add(cb);
    public static void ProcessMonthlyCallbacks()
    {
        foreach (var cb in _monthlyCallbacks)
            try { cb(); } catch (Exception e) { GD.PrintErr($"[ModAPI] monthly cb: {e.Message}"); }
    }
}
