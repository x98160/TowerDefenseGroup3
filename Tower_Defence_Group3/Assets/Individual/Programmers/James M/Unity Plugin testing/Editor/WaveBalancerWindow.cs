using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class WaveBalancerWindow : EditorWindow
{
    private AiSpawner spawner;
    private TowerData towerData;
    private Vector2 scroll;

    private const int bossWaveInterval = 3;

    // -- Runtime playtest tracking ------------------------------------------
    private WavePerformanceTracker.LiveWaveData liveData;
    private readonly List<WavePerformanceTracker.WaveResult> waveHistory
        = new List<WavePerformanceTracker.WaveResult>();
    private bool showPlaytestPanel = true;

    private struct EnemyStats
    {
        public string prefabName;
        public float health;
        public float damage;
        public int value;
        public Buffs buffs;
    }

    [MenuItem("Tools/Wave Balancer")]
    public static void Open()
    {
        GetWindow<WaveBalancerWindow>("Wave Balancer");
    }

    private void OnEnable()
    {
        WavePerformanceTracker.OnWaveCompleted -= OnWaveCompleted;
        WavePerformanceTracker.OnLiveUpdate -= OnLiveUpdate;
        EditorApplication.update -= Repaint;

        WavePerformanceTracker.OnWaveCompleted += OnWaveCompleted;
        WavePerformanceTracker.OnLiveUpdate += OnLiveUpdate;
        EditorApplication.update += Repaint;

        Debug.Log("[WaveBalancer] Subscribed to WavePerformanceTracker events.");
    }

    private void OnDisable()
    {
        WavePerformanceTracker.OnWaveCompleted -= OnWaveCompleted;
        WavePerformanceTracker.OnLiveUpdate -= OnLiveUpdate;
        EditorApplication.update -= Repaint;
    }

    private void OnWaveCompleted(WavePerformanceTracker.WaveResult result)
    {
        Debug.Log($"[WaveBalancer] Received wave {result.waveIndex + 1} result. Rating: {result.rating}. History count will be: {waveHistory.Count + 1}");
        waveHistory.RemoveAll(r => r.waveIndex == result.waveIndex);
        waveHistory.Add(result);
        waveHistory.Sort((a, b) => a.waveIndex.CompareTo(b.waveIndex));
        Repaint();
    }

    private void OnLiveUpdate(WavePerformanceTracker.LiveWaveData data)
    {
        liveData = data;
    }

    private void OnGUI()
    {
        spawner = (AiSpawner)EditorGUILayout.ObjectField("Spawner", spawner, typeof(AiSpawner), true);

        if (spawner == null)
        {
            EditorGUILayout.HelpBox("Assign your AiSpawner from the scene.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawWaveTable();
        EditorGUILayout.Space(16);
        DrawAiStatsPanel();
        EditorGUILayout.Space(16);
        DrawTowerStatsPanel();
        EditorGUILayout.Space(16);
        DrawDifficultyCurve();
        EditorGUILayout.Space(16);
        DrawBalanceReport();
        EditorGUILayout.Space(16);
        DrawPlaytestPanel();

        EditorGUILayout.EndScrollView();
    }

    // -- Wave Table

    private void DrawWaveTable()
    {
        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);

        var headerRect = EditorGUILayout.GetControlRect(false, 18);
        float[] widths = GetColumnWidths(headerRect.width);
        DrawColumnHeaders(headerRect, widths);

        for (int i = 0; i < spawner.wave.Length; i++)
        {
            Waves w = spawner.wave[i];
            if (w == null) continue;

            var rowRect = EditorGUILayout.GetControlRect(false, 22);
            if (i % 2 == 0)
                EditorGUI.DrawRect(rowRect, new Color(0.5f, 0.5f, 0.5f, 0.05f));

            DrawWaveRow(rowRect, widths, i, w);
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("+ Add Wave", GUILayout.Width(100)))
            AddWave();
    }

    private float[] GetColumnWidths(float total)
    {
        return new float[]
        {
            55,
            (total - 55 - 48) * 0.28f,
            (total - 55 - 48) * 0.28f,
            (total - 55 - 48) * 0.28f,
            (total - 55 - 48) * 0.16f,
            48,
        };
    }

    private void DrawColumnHeaders(Rect r, float[] w)
    {
        float x = r.x;
        string[] headers = { "Wave", "Enemy count", "Spawn rate (s)", "Groups", "Difficulty", "" };
        var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        for (int i = 0; i < headers.Length; i++)
        {
            EditorGUI.LabelField(new Rect(x, r.y, w[i], r.height), headers[i], style);
            x += w[i];
        }
    }

    private void DrawWaveRow(Rect r, float[] w, int index, Waves wave)
    {
        float x = r.x;
        float h = r.height;

        bool isBoss = (index + 1) % bossWaveInterval == 0;
        string waveLabel = isBoss ? $"Wave {index + 1} [B]" : $"Wave {index + 1}";
        EditorGUI.LabelField(new Rect(x, r.y, w[0] + (isBoss ? 10f : 0f), h), waveLabel);
        x += w[0];

        int totalCount = 0;
        float avgRate = 0f;
        int groupCount = wave.aiGroups?.Length ?? 0;

        if (wave.aiGroups != null)
        {
            foreach (var g in wave.aiGroups) { totalCount += g.aiCount; avgRate += g.spawnRate; }
            if (groupCount > 0) avgRate /= groupCount;
        }

        EditorGUI.LabelField(new Rect(x, r.y, w[1], h), totalCount.ToString()); x += w[1];
        EditorGUI.LabelField(new Rect(x, r.y, w[2], h), avgRate.ToString("0.00") + "s"); x += w[2];
        EditorGUI.LabelField(new Rect(x, r.y, w[3], h), groupCount.ToString()); x += w[3];

        float score = groupCount > 0 && avgRate > 0 ? (totalCount / avgRate) * groupCount : 0f;
        DrawDifficultyPill(new Rect(x, r.y + 2, w[4] - 4, h - 4), score); x += w[4];

        if (GUI.Button(new Rect(x, r.y + 1, w[5] - 2, h - 2), "Edit"))
            Selection.activeObject = wave;
    }

    private void DrawDifficultyPill(Rect r, float score)
    {
        Color bg; string label;
        if (score < 15) { bg = new Color(0.57f, 0.85f, 0.46f, 0.3f); label = "Easy"; }
        else if (score < 30) { bg = new Color(0.98f, 0.78f, 0.46f, 0.3f); label = "Medium"; }
        else if (score < 50) { bg = new Color(0.98f, 0.58f, 0.36f, 0.3f); label = "Hard"; }
        else { bg = new Color(0.88f, 0.29f, 0.29f, 0.3f); label = "Brutal"; }

        EditorGUI.DrawRect(r, bg);
        GUI.Label(r, label, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
    }

    // -- AI Stats Panel

    private void DrawAiStatsPanel()
    {
        EditorGUILayout.LabelField("AI Stats (per wave)", EditorStyles.boldLabel);

        var ms = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };

        for (int i = 0; i < spawner.wave.Length; i++)
        {
            Waves w = spawner.wave[i];
            if (w?.aiGroups == null || w.aiGroups.Length == 0) continue;

            var seen = new HashSet<string>();
            var prefabControllers = new List<(string name, AIController ctrl)>();

            foreach (var g in w.aiGroups)
            {
                if (g.aiPrefab == null) continue;
                if (!seen.Add(g.aiPrefab.name)) continue;
                var ctrl = g.aiPrefab.GetComponent<AIController>();
                if (ctrl != null)
                    prefabControllers.Add((g.aiPrefab.name, ctrl));
            }

            if (prefabControllers.Count == 0) continue;

            EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.miniBoldLabel);

            var hRect = EditorGUILayout.GetControlRect(false, 16);
            float rem = hRect.width - 110f;
            float col = rem / 4f;

            void Header(float x, float cw, string label) =>
                EditorGUI.LabelField(new Rect(x, hRect.y, cw, 16), label, ms);

            Header(hRect.x, 110f, "Prefab");
            Header(hRect.x + 110f, col, "Health");
            Header(hRect.x + 110f + col, col, "Damage");
            Header(hRect.x + 110f + col * 2f, col, "Value");
            Header(hRect.x + 110f + col * 3f, col, "Buffs");

            foreach (var (prefabName, ctrl) in prefabControllers)
            {
                var row = EditorGUILayout.GetControlRect(false, 18);
                EditorGUI.LabelField(new Rect(row.x, row.y, 110f, 18), prefabName, ms);

                EditorGUI.BeginChangeCheck();
                float newHealth = EditorGUI.FloatField(new Rect(row.x + 110f, row.y, col - 4, 18), ctrl.health);
                int newDamage = EditorGUI.IntField(new Rect(row.x + 110f + col, row.y, col - 4, 18), ctrl.damage);
                int newValue = EditorGUI.IntField(new Rect(row.x + 110f + col * 2f, row.y, col - 4, 18), ctrl.value);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(ctrl, "Edit AI Stats");
                    ctrl.health = newHealth;
                    ctrl.damage = newDamage;
                    ctrl.value = newValue;
                    EditorUtility.SetDirty(ctrl);
                }

                string BuffLabel(Buffs b)
                {
                    if (b == Buffs.None) return "None";
                    var parts = new List<string>();
                    if (b.HasFlag(Buffs.Speed)) parts.Add("Spd");
                    if (b.HasFlag(Buffs.Commander)) parts.Add("Cmd");
                    if (b.HasFlag(Buffs.SecondChance)) parts.Add("2nd");
                    return string.Join("+", parts);
                }

                EditorGUI.LabelField(
                    new Rect(row.x + 110f + col * 3f, row.y, col, 18),
                    BuffLabel(ctrl.buff), ms);
            }

            EditorGUILayout.Space(4);
        }
    }

    // -- Tower Stats Panel

    private void DrawTowerStatsPanel()
    {
        EditorGUILayout.LabelField("Tower Stats", EditorStyles.boldLabel);

        towerData = (TowerData)EditorGUILayout.ObjectField(
            "Tower Data", towerData, typeof(TowerData), false);

        if (towerData == null)
        {
            EditorGUILayout.HelpBox("Assign your TowerData ScriptableObject above.", MessageType.Info);
            return;
        }

        if (towerData.objectsData == null || towerData.objectsData.Count == 0)
        {
            EditorGUILayout.HelpBox("No towers found in TowerData.", MessageType.Info);
            return;
        }

        var ms = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        var hRect = EditorGUILayout.GetControlRect(false, 16);
        float rem = hRect.width - 100f;
        float col = rem / 5f;

        void Header(float x, float cw, string label) =>
            EditorGUI.LabelField(new Rect(x, hRect.y, cw, 16), label, ms);

        Header(hRect.x, 100f, "Tower");
        Header(hRect.x + 100f, col, "Cost");
        Header(hRect.x + 100f + col, col, "Damage");
        Header(hRect.x + 100f + col * 2f, col, "Upgrade");
        Header(hRect.x + 100f + col * 3f, col, "Fire Rate");
        Header(hRect.x + 100f + col * 4f, col, "Range");

        for (int i = 0; i < towerData.objectsData.Count; i++)
        {
            var obj = towerData.objectsData[i];
            if (obj == null) continue;

            var row = EditorGUILayout.GetControlRect(false, 18);
            if (i % 2 == 0)
                EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.04f));

            EditorGUI.LabelField(new Rect(row.x, row.y, 100f, 18), obj.Name, ms);

            EditorGUI.BeginChangeCheck();
            int newCost = EditorGUI.IntField(new Rect(row.x + 100f, row.y, col - 4, 18), obj.Cost);
            int newDamage = EditorGUI.IntField(new Rect(row.x + 100f + col, row.y, col - 4, 18), obj.Damage);
            int newUpgrade = EditorGUI.IntField(new Rect(row.x + 100f + col * 2f, row.y, col - 4, 18), obj.Upgrade);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(towerData, "Edit Tower Data");
                obj.Cost = newCost;
                obj.Damage = newDamage;
                obj.Upgrade = newUpgrade;
                EditorUtility.SetDirty(towerData);
            }

            DrawPlayerTrackFields(row, col, obj.Prefab, ms);

            if (obj.UpgradePrefabs == null) continue;

            for (int u = 0; u < obj.UpgradePrefabs.Length; u++)
            {
                GameObject upgradePrefab = obj.UpgradePrefabs[u];
                if (upgradePrefab == null) continue;

                var uRow = EditorGUILayout.GetControlRect(false, 18);
                EditorGUI.DrawRect(uRow, new Color(0.5f, 0.5f, 0.5f, 0.02f));

                var indentStyle = new GUIStyle(ms) { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };
                EditorGUI.LabelField(new Rect(uRow.x + 10f, uRow.y, 90f, 18), $"   Upgrade {u + 1}", indentStyle);

                var dash = new GUIStyle(ms) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
                EditorGUI.LabelField(new Rect(uRow.x + 100f, uRow.y, col, 18), "-", dash);
                EditorGUI.LabelField(new Rect(uRow.x + 100f + col, uRow.y, col, 18), "-", dash);
                EditorGUI.LabelField(new Rect(uRow.x + 100f + col * 2f, uRow.y, col, 18), "-", dash);

                DrawPlayerTrackFields(uRow, col, upgradePrefab, ms);
            }
        }

        EditorGUILayout.Space(8);
    }

    private void DrawPlayerTrackFields(Rect row, float col, GameObject prefab, GUIStyle ms)
    {
        if (prefab == null) return;

        PlayerTrack[] pts = prefab.GetComponentsInChildren<PlayerTrack>();

        if (pts == null || pts.Length == 0)
        {
            var dash = new GUIStyle(ms) { normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
            EditorGUI.LabelField(new Rect(row.x + 100f + col * 3f, row.y, col, 18), "-", dash);
            EditorGUI.LabelField(new Rect(row.x + 100f + col * 4f, row.y, col, 18), "-", dash);
            return;
        }

        EditorGUI.BeginChangeCheck();
        float newFireRate = EditorGUI.FloatField(
            new Rect(row.x + 100f + col * 3f, row.y, col - 4, 18), pts[0].fireRate);
        float newRange = EditorGUI.FloatField(
            new Rect(row.x + 100f + col * 4f, row.y, col - 4, 18), pts[0].maxDistance);

        if (EditorGUI.EndChangeCheck())
        {
            foreach (PlayerTrack pt in pts)
            {
                Undo.RecordObject(pt, "Edit PlayerTrack");
                pt.fireRate = newFireRate;
                pt.maxDistance = newRange;
                EditorUtility.SetDirty(pt);
            }
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    // -- Difficulty Curve

    private void DrawDifficultyCurve()
    {
        EditorGUILayout.LabelField("Difficulty curve", EditorStyles.boldLabel);

        var scores = GetDifficultyScores();
        if (scores.Count == 0) return;

        Rect graphRect = GUILayoutUtility.GetRect(
            GUIContent.none, GUIStyle.none,
            GUILayout.Height(130), GUILayout.ExpandWidth(true));

        EditorGUI.DrawRect(graphRect, new Color(0.5f, 0.5f, 0.5f, 0.08f));

        float maxScore = Mathf.Max(1f, scores.Max() * 1.1f);

        float pad = 12f;

        var points = new List<Vector3>();
        for (int i = 0; i < scores.Count; i++)
        {
            float x = graphRect.x + pad + (graphRect.width - pad * 2) * i / Mathf.Max(scores.Count - 1, 1);
            float y = graphRect.yMax - pad - (graphRect.height - pad * 2) * (scores[i] / maxScore);
            points.Add(new Vector3(x, y, 0));
        }

        if (points.Count > 1)
        {
            var fill = new List<Vector3>(points);
            fill.Add(new Vector3(points[points.Count - 1].x, graphRect.yMax - pad, 0));
            fill.Add(new Vector3(points[0].x, graphRect.yMax - pad, 0));
            Handles.color = new Color(0.22f, 0.53f, 0.9f, 0.08f);
            Handles.DrawAAConvexPolygon(fill.ToArray());
            Handles.color = new Color(0.22f, 0.53f, 0.9f, 0.85f);
            Handles.DrawAAPolyLine(2.5f, points.ToArray());
        }

        for (int i = 0; i < points.Count; i++)
        {
            bool isBoss = (i + 1) % bossWaveInterval == 0;
            Handles.color = isBoss
                ? new Color(0.9f, 0.6f, 0.1f, 1f)
                : new Color(0.22f, 0.53f, 0.9f, 1f);
            Handles.DrawSolidDisc(points[i], Vector3.forward, isBoss ? 6f : 4f);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperCenter };
            GUI.Label(new Rect(points[i].x - 16, points[i].y - 17, 32, 16),
                isBoss ? $"B{i + 1}" : $"W{i + 1}", labelStyle);
        }

        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        Handles.DrawLine(
            new Vector3(graphRect.x + pad, graphRect.yMax - pad, 0),
            new Vector3(graphRect.xMax - pad, graphRect.yMax - pad, 0));
    }

    // -- Balance Report

    private void DrawBalanceReport()
    {
        EditorGUILayout.LabelField("Balance report", EditorStyles.boldLabel);

        var issues = AnalyseBalance();

        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues detected. Difficulty curve looks healthy.", MessageType.Info);
            return;
        }

        foreach (var issue in issues)
        {
            MessageType type = issue.severity == Severity.Warning ? MessageType.Warning : MessageType.Error;
            EditorGUILayout.HelpBox(issue.message, type);
        }
    }

    private enum Severity { Warning, Error }

    private struct BalanceIssue
    {
        public Severity severity;
        public string message;
        public BalanceIssue(Severity s, string m) { severity = s; message = m; }
    }

    private List<BalanceIssue> AnalyseBalance()
    {
        var issues = new List<BalanceIssue>();
        var scores = GetDifficultyScores();

        if (scores.Count < 2) return issues;

        float totalScore = 0f;
        foreach (var s in scores) totalScore += s;
        float avg = totalScore / scores.Count;

        for (int i = 0; i < scores.Count; i++)
        {
            bool isBoss = (i + 1) % bossWaveInterval == 0;
            if (isBoss) continue;

            if (scores[i] > avg * 2f)
                issues.Add(new BalanceIssue(Severity.Error,
                    $"Wave {i + 1} is an unintended difficulty spike (score {scores[i]:0} vs average {avg:0}). " +
                    "Try adjusting wave composition."));
        }

        for (int i = 1; i < scores.Count; i++)
        {
            bool prevIsBoss = i % bossWaveInterval == 0;
            if (scores[i] < scores[i - 1] * 0.6f && !prevIsBoss)
                issues.Add(new BalanceIssue(Severity.Warning,
                    $"Wave {i + 1} is easier than wave {i} (score drops from {scores[i - 1]:0} to {scores[i]:0}). " +
                    "Players may feel the tension suddenly release."));
        }

        for (int i = 0; i <= scores.Count - 3; i++)
        {
            bool anyBoss = false;
            for (int j = i; j < i + 3; j++) if ((j + 1) % bossWaveInterval == 0) anyBoss = true;
            if (anyBoss) continue;

            float lo = scores[i], hi = scores[i];
            for (int j = i + 1; j < i + 3; j++) { lo = Mathf.Min(lo, scores[j]); hi = Mathf.Max(hi, scores[j]); }
            if (hi - lo < avg * 0.1f)
                issues.Add(new BalanceIssue(Severity.Warning,
                    $"Waves {i + 1}-{i + 3} have nearly identical difficulty. " +
                    "This plateau may feel repetitive - try varying enemy count or spawn rate."));
        }

        if (spawner.wave.Length > 0 && spawner.wave[0]?.aiGroups != null)
        {
            foreach (var g in spawner.wave[0].aiGroups)
            {
                if (g.spawnRate < 0.5f)
                    issues.Add(new BalanceIssue(Severity.Warning,
                        $"Wave 1 has a spawn rate of {g.spawnRate}s - this may overwhelm new players."));
            }
        }

        if (spawner.wave.Length > 0 && spawner.wave[0]?.aiGroups != null)
        {
            foreach (var g in spawner.wave[0].aiGroups)
            {
                if (g.aiPrefab == null) continue;
                var ctrl = g.aiPrefab.GetComponent<AIController>();
                if (ctrl != null && ctrl.damage > 20f)
                    issues.Add(new BalanceIssue(Severity.Warning,
                        $"Wave 1 contains '{g.aiPrefab.name}' with high damage ({ctrl.damage:0}). " +
                        "Consider saving high-damage enemies for later waves."));
            }
        }

        if (scores.Count > 0 && scores[scores.Count - 1] < scores[0] * 1.5f)
            issues.Add(new BalanceIssue(Severity.Warning,
                $"The final wave (score {scores[scores.Count - 1]:0}) isn't much harder than wave 1 (score {scores[0]:0}). " +
                "Consider increasing difficulty progression."));

        return issues;
    }

    // -- Shared Helpers

    private List<float> GetDifficultyScores()
    {
        var scores = new List<float>();
        if (spawner?.wave == null) return scores;

        foreach (var w in spawner.wave)
        {
            if (w?.aiGroups == null) { scores.Add(0); continue; }
            float totalCount = 0, totalRate = 0;
            int groups = w.aiGroups.Length;
            foreach (var g in w.aiGroups) { totalCount += g.aiCount; totalRate += g.spawnRate; }
            float avgRate = groups > 0 ? totalRate / groups : 0f;
            scores.Add(avgRate > 0 ? totalCount / avgRate : 0);
        }
        return scores;
    }

    private EnemyStats ReadStats(GameObject prefab)
    {
        var s = new EnemyStats();
        var controller = prefab.GetComponent<AIController>();
        if (controller != null)
        {
            s.health = controller.health;
            s.damage = controller.damage;
            s.value = controller.value;
            s.buffs = controller.buff;
        }
        return s;
    }

    private void AddWave()
    {
        var list = new List<Waves>(spawner.wave);
        var asset = ScriptableObject.CreateInstance<Waves>();
        asset.aiGroups = new AIGroup[] { new AIGroup { aiCount = 5, spawnRate = 1.5f } };
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Wave Asset", $"Wave_{list.Count + 1}", "asset", "");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            list.Add(asset);
            Undo.RecordObject(spawner, "Add Wave");
            spawner.wave = list.ToArray();
            EditorUtility.SetDirty(spawner);
        }
    }

    // -- Playtest Feedback Panel --------------------------------------------

    private void DrawPlaytestPanel()
    {
        showPlaytestPanel = EditorGUILayout.Foldout(showPlaytestPanel, "Playtest Feedback", true, EditorStyles.boldLabel);
        if (!showPlaytestPanel) return;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see live feedback here.", MessageType.Info);

            if (waveHistory.Count == 0)
                return;

            EditorGUILayout.LabelField("Previous session results", EditorStyles.miniBoldLabel);
            DrawHistoryTable();
            if (GUILayout.Button("Clear Results", GUILayout.Width(110)))
                waveHistory.Clear();
            return;
        }

        // -- Live status box -----------------------------------------------
        EditorGUILayout.LabelField("Live", EditorStyles.miniBoldLabel);

        if (liveData != null)
        {
            var liveStyle = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical(liveStyle);

            var ms = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };

            DrawLiveRow("Wave", $"{liveData.waveIndex + 1}", ms);
            DrawLiveRow("Time elapsed", $"{liveData.elapsedTime:0.0}s", ms);
            DrawLiveRow("Player health", $"{liveData.currentHealth:0}%", ms,
                HealthColor(liveData.currentHealth));
            DrawLiveRow("Enemies alive", $"{liveData.enemiesAlive}", ms);
            DrawLiveRow("Leaked", $"{liveData.enemiesLeaked}", ms,
                liveData.enemiesLeaked > 0 ? new Color(0.9f, 0.4f, 0.2f) : new Color(0.5f, 0.9f, 0.4f));
            DrawLiveRow("Gold", $"{liveData.currentGold}", ms);

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Waiting for first wave to start...", MessageType.Info);
        }

        // -- History table -------------------------------------------------
        if (waveHistory.Count > 0)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Completed waves this session", EditorStyles.miniBoldLabel);
            DrawHistoryTable();
        }
    }

    private void DrawHistoryTable()
    {
        var ms = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft };
        var hRect = EditorGUILayout.GetControlRect(false, 16);
        float w = hRect.width / 6f;

        void H(int col, string label) =>
            EditorGUI.LabelField(new Rect(hRect.x + w * col, hRect.y, w, 16), label, ms);

        H(0, "Wave"); H(1, "Time"); H(2, "Lives lost"); H(3, "Leaked"); H(4, "Gold earned"); H(5, "Rating");

        foreach (var r in waveHistory)
        {
            var row = EditorGUILayout.GetControlRect(false, 18);
            int idx = waveHistory.IndexOf(r);
            if (idx % 2 == 0)
                EditorGUI.DrawRect(row, new Color(0.5f, 0.5f, 0.5f, 0.04f));

            EditorGUI.LabelField(new Rect(row.x + w * 0, row.y, w, 18), $"Wave {r.waveIndex + 1}", ms);
            EditorGUI.LabelField(new Rect(row.x + w * 1, row.y, w, 18), $"{r.timeToClear:0.0}s", ms);

            var livesStyle = new GUIStyle(ms)
            {
                normal = { textColor = HealthColor(100f - r.livesLost) }
            };
            EditorGUI.LabelField(new Rect(row.x + w * 2, row.y, w, 18), $"{r.livesLost:0}%", livesStyle);

            var leakStyle = new GUIStyle(ms)
            {
                normal = { textColor = r.enemiesLeaked > 0 ? new Color(0.9f, 0.4f, 0.2f) : new Color(0.5f, 0.9f, 0.4f) }
            };
            EditorGUI.LabelField(new Rect(row.x + w * 3, row.y, w, 18), r.enemiesLeaked.ToString(), leakStyle);

            EditorGUI.LabelField(new Rect(row.x + w * 4, row.y, w, 18), $"+{r.goldEarned}", ms);

            Color ratingCol = r.rating switch
            {
                "Easy" => new Color(0.4f, 0.85f, 0.4f),
                "Fair" => new Color(0.7f, 0.85f, 0.3f),
                "Hard" => new Color(0.9f, 0.6f, 0.2f),
                "Brutal" => new Color(0.9f, 0.3f, 0.3f),
                _ => Color.white
            };
            var ratingStyle = new GUIStyle(ms) { normal = { textColor = ratingCol } };
            EditorGUI.LabelField(new Rect(row.x + w * 5, row.y, w, 18), r.rating, ratingStyle);
        }

        EditorGUILayout.Space(4);
        string advice = BuildAdvice();
        if (!string.IsNullOrEmpty(advice))
            EditorGUILayout.HelpBox(advice, MessageType.Warning);
    }

    private void DrawLiveRow(string label, string value, GUIStyle ms, Color? valueColor = null)
    {
        var r = EditorGUILayout.GetControlRect(false, 16);
        float half = r.width * 0.45f;
        EditorGUI.LabelField(new Rect(r.x, r.y, half, 16), label, ms);
        var valStyle = valueColor.HasValue
            ? new GUIStyle(ms) { normal = { textColor = valueColor.Value } }
            : ms;
        EditorGUI.LabelField(new Rect(r.x + half, r.y, half, 16), value, valStyle);
    }

    private Color HealthColor(float health)
    {
        if (health > 60f) return new Color(0.4f, 0.85f, 0.4f);
        if (health > 30f) return new Color(0.9f, 0.75f, 0.2f);
        return new Color(0.9f, 0.3f, 0.3f);
    }

    private string BuildAdvice()
    {
        if (waveHistory.Count == 0) return null;

        var issues = new List<string>();

        foreach (var r in waveHistory)
        {
            if (r.livesLost >= 40f)
                issues.Add($"Wave {r.waveIndex + 1} caused heavy damage ({r.livesLost:0}% health lost). Consider reducing enemy health or damage.");
        }

        int leakyWaves = waveHistory.Count(r => r.enemiesLeaked > 0);
        if (leakyWaves >= 2)
            issues.Add($"{leakyWaves} waves had enemies leak through. Towers may need better range or fire rate.");

        int poorWaves = waveHistory.Count(r => r.goldEarned < 20);
        if (poorWaves >= 2)
            issues.Add($"Player earned very little kill gold in {poorWaves} waves (avg under 20). " +
                       "Consider increasing enemy value so players can afford upgrades.");

        var scores = GetDifficultyScores();
        foreach (var r in waveHistory)
        {
            if (r.timeToClear < 5f && r.livesLost == 0 && r.enemiesLeaked == 0)
                issues.Add($"Wave {r.waveIndex + 1} was cleared in {r.timeToClear:0.0}s with no damage taken - may be too easy.");
        }

        return issues.Count > 0 ? string.Join("\n\n", issues) : null;
    }
}