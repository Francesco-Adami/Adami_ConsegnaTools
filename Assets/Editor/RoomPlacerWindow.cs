using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RoomPlacerWindow : EditorWindow
{
    [MenuItem("Tools/RoomPlacer")]
    public static void CreateWindow()
    {
        GetWindow<RoomPlacerWindow>("Room Placer");
    }

    private class FolderData
    {
        public string name;
        public string[] prefabPaths; // path relativi a Unity
        public string[] prefabNames;
        public GameObject[] prefabAssets; // riferimenti ai prefab in memoria
    }

    private static FolderData folderData = new FolderData();
    private const string RootFolder = "Assets/Prefabs";
    private const string OneDoorFolder = "OneDoor";
    private const string TwoDoorsFolder = "TwoDoors";
    private const string ThreeDoorsFolder = "ThreeDoors";
    private const string FourDoorsFolder = "FourDoors";

    private static GameObject selectedPrefab;
    private static GameObject previewObject;
    private static Vector3 previewPosition;
    private static float previewRotation; // Y rotation in degrees

    bool isRangeVisible = true;
    float range = 20f;

    // SNAP CONFIG
    private bool isSnapped;
    private List<Collider> lastSnappedColliders = new List<Collider>();

    private void OnEnable()
    {
        ScanFolders(OneDoorFolder);

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

        #region PARTE GRAFICA
        Handles.BeginGUI();

        // area in alto a sinistra
        GUILayout.BeginArea(new Rect(20, 50, 150, 500));

        // GUI CONTENT combina immagine + tooltip in un unico oggetto
        GUIContent content = new GUIContent();

        // prendo il content del primo elemento 
        content = GetContent(0);
        if (GUILayout.Button(content, GUILayout.Height(80)))
        {
            Debug.Log("Selezionato elemento 1");

            SelectPrefab(folderData.prefabAssets[0]);

            e.Use();
        }

        GUILayout.Space(10);

        // prendo il content del secondo elemento 
        content = GetContent(1);
        if (GUILayout.Button(content, GUILayout.Height(80)))
        {
            Debug.Log("Selezionato elemento 2");

            SelectPrefab(folderData.prefabAssets[1]);

            e.Use();
        }

        GUILayout.Space(10);

        // prendo il content del terzo elemento 
        content = GetContent(2);
        if (GUILayout.Button(content, GUILayout.Height(80)))
        {
            Debug.Log("Selezionato elemento 3");

            SelectPrefab(folderData.prefabAssets[2]);

            e.Use();
        }

        GUILayout.Space(10);

        // UNDO
        if (GUILayout.Button("Undo", GUILayout.Height(80)))
        {
            Undo.PerformUndo();
            Debug.Log("Undo eseguito");

            e.Use();
        }

        GUILayout.EndArea();
        Handles.EndGUI();
        #endregion

        // -- SE HO SELEZIONATO UN PREFAB --
        #region PARTE LOGICA
        if (selectedPrefab == null) return;

        #region DRAW MESH
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore))
        {
            // creare la preview e aggiornarla in base alla posizione e alla normale del cursore
            UpdatePreview(hit.point);
        }
        #endregion

        // Shift+Q = rotate 90° counter-clockwise, Shift+E = rotate 90° clockwise
        if (e.type == EventType.KeyDown && e.shift)
        {
            if (e.keyCode == KeyCode.Q)
            {
                previewRotation -= 90f;
                e.Use();
            }
            else if (e.keyCode == KeyCode.E)
            {
                previewRotation += 90f;
                e.Use();
            }
        }

        // ESC per annullare
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            ClearSelection();
            e.Use();
        }

        #region ISTANZIO LA STANZA
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (isSnapped)
            {
                GameObject spawned = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;

                spawned.transform.position = previewPosition;
                spawned.transform.rotation = Quaternion.Euler(0, previewRotation, 0);

                // Tag le porte usate come UsedDoor (sulla stanza piazzata e su quella già in scena)
                if (lastSnappedColliders.Count == 2)
                {
                    // [0] = collider della preview, [1] = collider della stanza già piazzata
                    // Trovo il collider corrispondente sullo spawned tramite il path nella gerarchia
                    Collider previewDoor = lastSnappedColliders[0];
                    Collider placedDoor = lastSnappedColliders[1];

                    // Tag la porta della stanza già in scena
                    if (placedDoor != null)
                        placedDoor.gameObject.tag = "UsedDoor";

                    // Trovo la stessa porta sullo spawned object
                    if (previewDoor != null)
                    {
                        string relativePath = GetRelativePath(previewObject.transform, previewDoor.transform);
                        Transform spawnedDoor = spawned.transform.Find(relativePath);
                        if (spawnedDoor != null)
                            spawnedDoor.gameObject.tag = "UsedDoor";
                    }
                }
                lastSnappedColliders.Clear();

                Undo.RegisterCreatedObjectUndo(spawned, "Spawned Gameobject");

            }

            e.Use();
        }
        #endregion

        #endregion PARTE LOGICA

        sceneView.Repaint();
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
            ScanFolders(OneDoorFolder);
        }

        if (GUILayout.Button("2 Doors", GUILayout.MinHeight(30)))
        {
            ScanFolders(TwoDoorsFolder);
        }

        if (GUILayout.Button("3 Doors", GUILayout.MinHeight(30)))
        {
            ScanFolders(ThreeDoorsFolder);
        }

        if (GUILayout.Button("4 Doors", GUILayout.MinHeight(30)))
        {
            ScanFolders(FourDoorsFolder);
        }
        #endregion
    }

    // --- TOOL HELPERS ---
    private GUIContent GetContent(int index)
    {
        GUIContent content = new GUIContent();

        GameObject prefab = folderData.prefabAssets[index];

        // GetAssetPreview è asincrono
        Texture2D preview = AssetPreview.GetAssetPreview(prefab);

        if (preview != null)
        {
            content = new GUIContent(preview, folderData.prefabNames[index]);
        }
        else
        {
            content = new GUIContent(folderData.prefabNames[index]);
        }

        return content;
    }

    private void UpdatePreview(Vector3 position)
    {
        if (previewObject == null)
        {
            // istanzio prefab
            previewObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        }

        position.y = 0;

        // applico la rotazione alla preview
        previewObject.transform.rotation = Quaternion.Euler(0, previewRotation, 0);

        // sistemo la posizione della preview (incollata al plane -> y = 0)
        previewObject.transform.position = position;

        Handles.color = Color.black;
        Handles.DrawWireDisc(position, Vector3.up, range);

        // SNAP
        lastSnappedColliders = SnapToClosestCompatibleDoor();

        previewPosition = previewObject.transform.position;
    }

    /// <summary>
    /// Trova il collider FreeDoor più vicino nel raggio d'azione, quindi la FreeDoor che si adatta 
    /// meglio nella preview. 
    /// Snappa l'anteprima in modo che le due porte si sovrappongano.
    /// Restituisce la coppia di collider che sono stati agganciati (prima la preview, poi quello già piazzato).
    /// </summary>
    private List<Collider> SnapToClosestCompatibleDoor()
    {
        if (previewObject == null) return new List<Collider>();

        HashSet<Collider> previewColliders = previewObject.GetComponentsInChildren<Collider>().ToHashSet();

        // trovo tutti i collider nel range, tranne quelli della preview
        HashSet<Collider> collidersInRange = Physics.OverlapSphere(
            previewObject.transform.position, range).ToHashSet();
        collidersInRange.ExceptWith(previewColliders);

        Collider bestPreviewDoor = null;
        Collider bestPlacedDoor = null;
        float bestDist = float.MaxValue;

        foreach (Collider otherCol in collidersInRange)
        {
            // controllo solamente le porte Libere di quelle già piazzate
            if (!otherCol.gameObject.CompareTag("FreeDoor")) continue;

            foreach (Collider myCol in previewColliders)
            {
                // controllo solamente le porte Libere della mia preview
                if (!myCol.gameObject.CompareTag("FreeDoor")) continue;

                // le porte devono avere i forward inversi
                float dot = Vector3.Dot(myCol.transform.forward.normalized,
                                        otherCol.transform.forward.normalized);
                if (dot > -0.95f) continue;

                // controllo se ci sono porte più vicine (sono già opposte)
                float dist = Vector3.Distance(myCol.transform.position, otherCol.transform.position);
                if (dist < bestDist)
                {
                    // mi salvo il miglior risultato 
                    bestDist = dist;
                    bestPreviewDoor = myCol;
                    bestPlacedDoor = otherCol;
                }
            }
        }

        if (bestPreviewDoor == null)
        {
            isSnapped = false;
            return new List<Collider>();
        }

        // Snappo la posizione
        Vector3 offset = bestPlacedDoor.transform.position - bestPreviewDoor.transform.position;
        previewObject.transform.position += offset;

        isSnapped = true;

        return new List<Collider> { bestPreviewDoor, bestPlacedDoor };
    }

    private static void ScanFolders(string folder)
    {
        folderData = new FolderData();

        ClearSelection();

        if (!AssetDatabase.IsValidFolder(RootFolder)) return;


        string fullRootPath = Path.GetFullPath(RootFolder);

        // ricostruire il path relativo di Unity
        string assetFolderPath = RootFolder + "/" + folder;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { assetFolderPath });

        // cartella vuota --> return
        if (guids.Length == 0) return;

        var data = new FolderData()
        {
            name = folder,
            prefabPaths = new string[guids.Length],
            prefabNames = new string[guids.Length],
            prefabAssets = new GameObject[guids.Length],
        };

        for (int i = 0; i < guids.Length; i++)
        {
            // conversione GUID --> PATH LEGGIBILE
            data.prefabPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

            data.prefabNames[i] = Path.GetFileNameWithoutExtension(data.prefabPaths[i]);

            // load asset path carica il riferimento in memoria (NON istanzia)
            data.prefabAssets[i] = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPaths[i]);
        }

        folderData = data;
    }

    private static void SelectPrefab(GameObject prefab)
    {
        ClearSelection();

        selectedPrefab = prefab;
    }

    private static void ClearSelection()
    {
        if (selectedPrefab == null) return;

        selectedPrefab = null;
        previewRotation = 0f;
        DestroyImmediate(previewObject);
    }


    /// <summary>
    /// Restituisce il path relativo di un child rispetto al root (es. "Doors/DoorLeft"),
    /// compatibile con Transform.Find().
    /// </summary>
    private static string GetRelativePath(Transform root, Transform child)
    {
        var parts = new System.Collections.Generic.List<string>();
        Transform current = child;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
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
