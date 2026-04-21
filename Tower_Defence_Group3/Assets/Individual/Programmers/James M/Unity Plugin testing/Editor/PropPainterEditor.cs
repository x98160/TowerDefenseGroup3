using UnityEditor;
using UnityEngine;

public class PropPainterEditor : EditorWindow
{
    private PainterPalette activePalette;
    private bool paintMode = false;

    // Box Selection Variables
    private Vector3 boxStartPos;
    private bool isDrawingBox = false;
    private int fillDensity = 10;

    // Safety radius to prevent overlapping
    private float safetyRadius = 0.5f;
    [MenuItem("Tools/Prop Painter")]
    public static void ShowWindow() => GetWindow<PropPainterEditor>("Prop Painter");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        GUILayout.Label("Painter Settings", EditorStyles.boldLabel);
        activePalette = (PainterPalette)EditorGUILayout.ObjectField("Palette", activePalette, typeof(PainterPalette), false);

        fillDensity = EditorGUILayout.IntSlider("Box Fill Density", fillDensity, 1, 100);
        safetyRadius = EditorGUILayout.Slider("Proximity Safety", safetyRadius, 0.1f, 5f);

        paintMode = GUILayout.Toggle(paintMode, "Enable Paint Mode", "Button", GUILayout.Height(40));

        EditorGUILayout.HelpBox("Shift + Left Click: Paint\nShift + Right Click: Erase\nCtrl + Left Click Drag: Box Fill", MessageType.Info);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!paintMode || activePalette == null) return;

        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        // --- 1. BOX SELECTION LOGIC (Ctrl + Left Click) ---
        if (e.control && e.button == 0)
        {
            HandleUtility.AddDefaultControl(controlID);

            if (e.type == EventType.MouseDown)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    boxStartPos = hit.point;
                    isDrawingBox = true;
                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp && isDrawingBox)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    FillArea(boxStartPos, hit.point);
                    isDrawingBox = false;
                    e.Use();
                }
            }
        }

        // Visual feedback for the box
        if (isDrawingBox)
        {
            Handles.color = Color.cyan;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                DrawVisualBox(boxStartPos, hit.point);
            }
            sceneView.Repaint();
        }

        // --- 2. EXISTING PAINTER LOGIC (Shift + Click) ---
        if (e.shift)
        {
            HandleUtility.AddDefaultControl(controlID);

            // Paint
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    PaintObject(hit.point);
                    e.Use();
                }
            }

            // Erase
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    EraseObject(hit.collider.gameObject);
                    e.Use();
                }
            }
        }
    }

    private void FillArea(Vector3 start, Vector3 end)
    {
        float minX = Mathf.Min(start.x, end.x);
        float maxX = Mathf.Max(start.x, end.x);
        float minZ = Mathf.Min(start.z, end.z);
        float maxZ = Mathf.Max(start.z, end.z);

        Undo.IncrementCurrentGroup();

        int placedCount = 0;
        int maxAttempts = fillDensity * 5;
        int attempts = 0;

        while (placedCount < fillDensity && attempts < maxAttempts)
        {
            attempts++;
            float randX = Random.Range(minX, maxX);
            float randZ = Random.Range(minZ, maxZ);

            Vector3 rayStart = new Vector3(randX, Mathf.Max(start.y, end.y) + 10f, randZ);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit))
            {
                if (PaintObject(hit.point))
                {
                    placedCount++;
                }
            }
        }
    }

    private void DrawVisualBox(Vector3 start, Vector3 end)
    {
        Vector3 v1 = new Vector3(start.x, start.y, end.z);
        Vector3 v2 = new Vector3(end.x, start.y, start.z);

        Handles.DrawLine(start, v1);
        Handles.DrawLine(v1, end);
        Handles.DrawLine(end, v2);
        Handles.DrawLine(v2, start);
    }

    private void EraseObject(GameObject targetObj)
    {
        GameObject objectToDelete = targetObj;
        if (targetObj.transform.parent != null)
        {
            objectToDelete = targetObj.transform.root.gameObject;
        }

        // Safety: Don't delete essentials
        if (objectToDelete.name.Contains("Floor") ||
            objectToDelete.name.Contains("Main Camera") ||
            objectToDelete.name.Contains("Terrain"))
        {
            return;
        }

        Undo.DestroyObjectImmediate(objectToDelete);
    }

    private bool PaintObject(Vector3 position)
    {
        if (activePalette.TerrainPrefabs.Count == 0) return false;

        // Check for existing props in a small radius
        // We use a LayerMask to ensure the 'Ground' doesn't block the spawn
        // This assumes your props are on the 'Default' layer.
        int propMask = LayerMask.GetMask("Environment");

        if (Physics.CheckSphere(position, safetyRadius, propMask))
        {
            return false;
        }

        GameObject prefab = activePalette.TerrainPrefabs[Random.Range(0, activePalette.TerrainPrefabs.Count)];
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        newObj.transform.position = position;
        newObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        float scale = Random.Range(activePalette.minScale, activePalette.maxScale);
        newObj.transform.localScale = Vector3.one * scale;

        Undo.RegisterCreatedObjectUndo(newObj, "Paint Prop");
        return true;
    }
}