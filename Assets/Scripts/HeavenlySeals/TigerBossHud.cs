using UnityEngine;

public sealed class TigerBossHud : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.065f, 0.86f);
    private static readonly Color BarBackgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.95f);
    private static readonly Color AttackColor = new Color(1f, 0.25f, 0.12f);
    private static readonly Color WarningColor = new Color(1f, 0.76f, 0.2f);
    private static readonly Color RecoveryColor = new Color(0.3f, 0.9f, 1f);
    private static readonly Color VictoryColor = new Color(0.45f, 1f, 0.55f);

    private const float ReferenceHeight = 720f;
    private const float PanelWidth = 340f;
    private const float WideLayoutMinimumWidth = 1120f;

    private TigerBoss boss;
    private PlayerCombat player;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle statusStyle;
    private GUIStyle controlsStyle;

    public void Initialize(TigerBoss boss, PlayerCombat player)
    {
        this.boss = boss;
        this.player = player;
    }

    private void OnGUI()
    {
        if (boss == null || player == null)
            return;

        float scale = Screen.height / ReferenceHeight;
        float scaledWidth = Screen.width / scale;
        bool useWideLayout = scaledWidth >= WideLayoutMinimumWidth;
        float panelWidth = Mathf.Min(PanelWidth, scaledWidth - 32f);
        float panelX = useWideLayout ? scaledWidth - 376f : scaledWidth - panelWidth - 16f;
        float panelY = useWideLayout ? 16f : 270f;

        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        EnsureStyles();

        DrawSolid(new Rect(panelX - 8f, panelY - 8f, panelWidth + 16f, 192f), PanelColor);

        bool defeated = boss.Phase == TigerBossPhase.Dead;
        GUI.color = defeated ? VictoryColor : Color.white;
        GUI.Label(new Rect(panelX, panelY, panelWidth, 24f), defeated ? "TIGER DEFEATED" : "TIGER BOSS", titleStyle);

        GUI.color = Color.white;
        GUI.Label(new Rect(panelX, panelY + 26f, panelWidth, 22f),
            $"HP  {boss.CurrentHealth:0} / {boss.MaxHealth:0}", labelStyle);

        float healthFraction = Mathf.Clamp01(boss.CurrentHealth / boss.MaxHealth);
        Rect healthBar = new Rect(panelX, panelY + 48f, panelWidth, 16f);
        DrawSolid(healthBar, BarBackgroundColor);
        DrawSolid(new Rect(healthBar.x + 2f, healthBar.y + 2f,
            (healthBar.width - 4f) * healthFraction, healthBar.height - 4f),
            Color.Lerp(AttackColor, WarningColor, healthFraction));

        BeatClockTick clock = Managers.Beat.GetClockTick();
        double? nextHitBeat = boss.NextHitBeat;
        GUI.color = Color.white;
        GUI.Label(new Rect(panelX, panelY + 70f, panelWidth, 22f),
            $"Phase: {boss.Phase}  |  Transitions: {boss.TransitionCount}", labelStyle);
        GUI.Label(new Rect(panelX, panelY + 92f, panelWidth, 22f),
            $"Pattern: {boss.PatternName}", labelStyle);

        GetCombatState(nextHitBeat, out string state, out Color stateColor);
        GUI.color = stateColor;
        GUI.Label(new Rect(panelX, panelY + 114f, panelWidth, 22f), $"State: {state}", statusStyle);

        GUI.color = Color.white;
        GUI.Label(new Rect(panelX, panelY + 136f, panelWidth, 22f),
            $"BPM: {clock.Bpm:0}  (base {clock.BaseBpm:0} x{clock.TempoMultiplier:0.00})", labelStyle);
        string nextHit = nextHitBeat.HasValue
            ? $"{nextHitBeat.Value:0.00} ({nextHitBeat.Value - clock.TotalBeats:+0.00;-0.00;0.00})"
            : "--";
        GUI.Label(new Rect(panelX, panelY + 158f, panelWidth, 22f),
            $"Beat: {clock.TotalBeats:0.00}  |  Next hit: {nextHit}", labelStyle);

        if (!player.IsDead && !defeated && boss.Phase != TigerBossPhase.Stopped)
        {
            float controlsWidth = Mathf.Min(520f, scaledWidth - 32f);
            GUI.Label(new Rect(scaledWidth - controlsWidth - 16f, 664f, controlsWidth, 40f),
                boss.Phase == TigerBossPhase.Transition
                    ? "Aim tracks until launch; bright shot paths stay fixed - move to dodge"
                    : "S: attack | A: parry | arrows/Space/Shift: dodge", controlsStyle);
        }

        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
    }

    private void GetCombatState(double? nextHitBeat, out string state, out Color color)
    {
        if (boss.Phase == TigerBossPhase.Dead)
        {
            state = "DEFEATED";
            color = VictoryColor;
        }
        else if (boss.Phase == TigerBossPhase.Stopped)
        {
            state = "STOPPED";
            color = Color.gray;
        }
        else if (boss.Phase == TigerBossPhase.Waiting)
        {
            state = "WAITING";
            color = Color.gray;
        }
        else if (boss.IsAttackActive)
        {
            state = "ATTACK";
            color = AttackColor;
        }
        else if (nextHitBeat.HasValue)
        {
            state = "WARNING";
            color = WarningColor;
        }
        else
        {
            state = "RECOVERY";
            color = RecoveryColor;
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 13;

        statusStyle = new GUIStyle(labelStyle);
        statusStyle.fontStyle = FontStyle.Bold;

        controlsStyle = new GUIStyle(labelStyle);
        controlsStyle.fontStyle = FontStyle.Bold;
        controlsStyle.alignment = TextAnchor.MiddleRight;
        controlsStyle.wordWrap = true;
    }

    private static void DrawSolid(Rect bounds, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(bounds, Texture2D.whiteTexture);
    }
}
