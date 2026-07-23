using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RoomPlacerWindow : EditorWindow
{
    [MenuItem("Tools/RoomPlacer")]
    public static void CreateWindow()
    {
        GetWindow<RoomPlacerWindow>("Room Placer");
    }

    private class CategoryData
    {
        public string name;
        public bool foldout; // stato del foldout: aperto/chiuso
        public string[] prefabPaths; // path relativi a Unity
        public string[] prefabNames;
        public GameObject[] prefabAssets; // riferimenti ai prefab in memoria
    }

    private static readonly List<CategoryData> categories = new List<CategoryData>();
    private const string RootFolder = "Assets/PrefabSpawner";

    private static GameObject selectedPrefab;
    private static Mesh previewMesh;
    private static Material[] previewMaterials;
    private static Vector3 previewPosition;
    private static Vector3 previewScale;

    bool isRangeVisible = true;
    float range = 20f;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // OVERLAY NELLA SCENE VIEW
    private void OnSceneGUI(SceneView sceneView)
    {
        // mi salvo l'evento per usare il tocco quando premo sui pulsanti,
        // per non toccare quello che c'è sotto ai pulsanti nella scena
        Event e = Event.current;

        Handles.BeginGUI();

        // area in alto a sinistra
        GUILayout.BeginArea(new Rect(20, 50, 150, 500));

        if (GUILayout.Button("Preview 1", GUILayout.Height(80)))
        {
            Debug.Log("Selezionato elemento 1");

            e.Use();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Preview 2", GUILayout.Height(80)))
        {
            Debug.Log("Selezionato elemento 2");

            e.Use();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Preview 3", GUILayout.Height(80)))
        {
            Debug.Log("Selezionato elemento 3");

            e.Use();
        }

        GUILayout.Space(10);

        // UNDO
        if (GUILayout.Button("Undo", GUILayout.Height(80)))
        {
            Debug.Log("Undo eseguito");

            e.Use();
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    // GUI DEL TOOL
    private void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle();
        titleStyle.normal.textColor = Color.black;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 18;

        GUIStyle inputStyle = new GUIStyle();
        inputStyle.normal.textColor = Color.white;
        inputStyle.alignment = TextAnchor.MiddleCenter;
        inputStyle.fontSize = 14;

        #region Set the placement range
        #region --------------------------------------------------------
        LineWithSpace(3f);
        #endregion

        GUILayout.Label("Set the placement range", titleStyle);

        #region --------------------------------------------------------
        LineWithSpace(3f);
        #endregion
        #endregion

        #region DATA INPUT
        isRangeVisible = EditorGUILayout.Toggle("is Range Visible", isRangeVisible);

        range = EditorGUILayout.FloatField("Range", range);
        #endregion

        #region Set the rooms by the number of doors
        #region --------------------------------------------------------
        LineWithSpace(3f);
        #endregion

        GUILayout.Label("Set the rooms by the number of doors", titleStyle);

        #region --------------------------------------------------------
        LineWithSpace(3f);
        #endregion
        #endregion

        #region DOORS BUTTONS
        if (GUILayout.Button("1 Door", GUILayout.MinHeight(30)))
        {
            SelectPrefabsWithDoors(1);
        }

        if (GUILayout.Button("2 Doors", GUILayout.MinHeight(30)))
        {
            SelectPrefabsWithDoors(1);
        }

        if (GUILayout.Button("3 Doors", GUILayout.MinHeight(30)))
        {
            SelectPrefabsWithDoors(1);
        }

        if (GUILayout.Button("4 Doors", GUILayout.MinHeight(30)))
        {
            SelectPrefabsWithDoors(1);
        }
        #endregion
    }

    // --- TOOL HELPERS ---
    public void SelectPrefabsWithDoors(int doors)
    {
        // TODO
    }

    private static void ScanFolders()
    {
        categories.Clear();

        ClearSelection();

        if (!AssetDatabase.IsValidFolder(RootFolder)) return;

        string fullRootPath = Path.GetFullPath(RootFolder);
        string[] subDirs = Directory.GetDirectories(fullRootPath); // Cubes, Cylinders, Spheres

        foreach (string dir in subDirs)
        {
            string folderName = Path.GetFileName(dir);

            // ricostruire il path relativo di Unity
            string assetFolderPath = RootFolder + "/" + folderName;

            string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { assetFolderPath });

            // cartella vuota skip
            if (guids.Length == 0) continue;

            var category = new CategoryData()
            {
                name = folderName,
                foldout = false,
                prefabPaths = new string[guids.Length],
                prefabNames = new string[guids.Length],
                prefabAssets = new GameObject[guids.Length],
            };

            for (int i = 0; i < guids.Length; i++)
            {
                // conversione GUID --> PATH LEGGIBILE
                category.prefabPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

                category.prefabNames[i] = Path.GetFileNameWithoutExtension(category.prefabPaths[i]);

                // load asset path carica il riferimento in memoria (NON istanzia)
                category.prefabAssets[i] = AssetDatabase.LoadAssetAtPath<GameObject>(category.prefabPaths[i]);
            }

            categories.Add(category);
        }
    }

    private static void SelectPrefab(GameObject prefab, string name)
    {
        selectedPrefab = prefab;

        var meshFilter = prefab.GetComponentInChildren<MeshFilter>();
        var meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

        if (meshFilter != null && meshRenderer != null)
        {
            previewMesh = meshFilter.sharedMesh;
            previewMaterials = meshRenderer.sharedMaterials;
            previewScale = prefab.transform.localScale;
        }
        else
        {
            Debug.LogWarning($"meshFilter == null || meshRenderer == null");
            ClearSelection();
        }
    }

    private static void ClearSelection()
    {
        if (selectedPrefab == null || previewMesh == null) return;

        selectedPrefab = null;
        previewMesh = null;
        previewMaterials = null;
    }

    // --- GUI HELPERS ---
    #region LINE -----------------------------------
    private void LineWithSpace(float spaceUpDown = 10f)
    {
        GUILayout.Space(spaceUpDown);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(spaceUpDown);
    }

    private void LineWithSpace(float spaceUp, float spaceDown)
    {
        GUILayout.Space(spaceUp);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(spaceDown);
    }
    #endregion
}
