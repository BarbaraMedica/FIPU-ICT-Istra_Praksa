using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a NEW, separate scene (Assets/Scenes/Grade4Level.unity): a linear chain of medium-large
/// subject rooms. Does NOT touch the prototype builder.
///
/// Each room is an escape-room SEQUENCE. Tasks are single clickable cubes (later swappable for a
/// book on a shelf, a poster, etc.) that pop up their question when clicked - multiple choice opens
/// a choice popup, typed opens a text-input popup. Only the first task is visible; solving one shows
/// a thematic hint and reveals the next; solving the last reveals a key. The key is COLLECTED on
/// click (it is used up / disappears and unlocks the exit door) - it is not carried in the hands.
/// Extra clickable "clue" props around the room give thematic hints too.
///
/// EDIT the RoomConfigs array to set real subjects/questions, task positions and hints. Run via
/// menu: Tools > Escape Room > Build Grade 4 Level.
/// </summary>
public static class Grade4LevelBuilder
{
    private const string ScenePath = "Assets/Scenes/Grade4Level.unity";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    private const string GeneratedRoot = "Assets/Generated/Grade4Level";
    private const string MaterialsFolder = GeneratedRoot + "/Materials";
    private const string InputReferencesFolder = GeneratedRoot + "/InputReferences";

    private const float WallHeight = 4.2f;
    private const float WallThickness = 0.3f;
    private const float DoorWidth = 2.4f;
    private const float DoorHeight = 3f;

    private static readonly Vector2 VestibuleSize = new Vector2(8f, 7f);
    private static readonly Vector2 RoomSize = new Vector2(16f, 13f);
    private static readonly Vector2 AlcoveSize = new Vector2(8f, 5f);

    // Where the three tasks sit inside each room (scattered so finding them feels like searching).
    private static readonly Vector3 TaskPos1 = new Vector3(0f, 0f, -3.5f);
    private static readonly Vector3 TaskPos2 = new Vector3(4.5f, 0f, 1.0f);
    private static readonly Vector3 TaskPos3 = new Vector3(-4.5f, 0f, 3.5f);
    private static readonly Vector3 KeyRevealPos = new Vector3(0f, 1.1f, 2.6f); // hovers so it is easy to spot

    private enum PuzzleKind
    {
        MultipleChoice,
        Typed
    }

    private struct TaskConfig
    {
        public PuzzleKind kind;
        public string question;
        public string correctAnswer;
        public string[] choices;
        public string correctFeedback;
        public string wrongFeedback;
        public Vector3 localPosition;
        public string hintAfterSolving;
    }

    private struct RoomConfig
    {
        public string subjectLabel;
        public TaskConfig[] tasks;
        public string keyItemName;
        public string keyPickUpMessage;
        public Vector3 keyRevealLocalPos;
    }

    // -------------------------------------------------------------------------
    // EDIT HERE: subjects / questions / hints. Placeholder 4th-grade math. Hints
    // are thematic (no "left/right"); tasks are revealed one at a time, in order.
    // -------------------------------------------------------------------------
    private static readonly RoomConfig[] RoomConfigs =
    {
        new RoomConfig
        {
            subjectLabel = "Matematika 1 - Zbrajanje i oduzimanje",
            keyItemName = "Zlatni kljuc",
            keyPickUpMessage = "Uzeo si zlatni kljuc i njime otkljucao vrata!",
            keyRevealLocalPos = KeyRevealPos,
            tasks = new[]
            {
                MultipleChoice("Koliko je 6 + 7?", "13", new[] { "11", "13", "15" }, "Tocno! 6 + 7 = 13.", TaskPos1,
                    "Sljedeci zadatak skriva se ondje gdje mudrost stoji na policama s knjigama."),
                MultipleChoice("Koliko je 15 - 8?", "7", new[] { "6", "7", "9" }, "Tocno! 15 - 8 = 7.", TaskPos2,
                    "Iduci trag ceka u tihom, udaljenom kutu prostorije."),
                Typed("Upisi rezultat: 24 + 18 = ?", "42", "Tocno! 24 + 18 = 42.", TaskPos3,
                    "Sve je rijeseno! U sredini sobe zasjao je zlatni kljuc - uzmi ga!")
            }
        },
        new RoomConfig
        {
            subjectLabel = "Matematika 2 - Mnozenje",
            keyItemName = "Srebrni kljuc",
            keyPickUpMessage = "Uzeo si srebrni kljuc i njime otkljucao vrata!",
            keyRevealLocalPos = KeyRevealPos,
            tasks = new[]
            {
                MultipleChoice("Koliko je 7 x 6?", "42", new[] { "36", "42", "48" }, "Tocno! 7 x 6 = 42.", TaskPos1,
                    "Sljedeci zadatak skriva se ondje gdje mudrost stoji na policama s knjigama."),
                Typed("Upisi rezultat: 9 x 4 = ?", "36", "Tocno! 9 x 4 = 36.", TaskPos2,
                    "Iduci trag ceka u tihom, udaljenom kutu prostorije."),
                MultipleChoice("Koliko je 8 x 5?", "40", new[] { "35", "40", "45" }, "Tocno! 8 x 5 = 40.", TaskPos3,
                    "Sve je rijeseno! U sredini sobe zasjao je srebrni kljuc - uzmi ga!")
            }
        },
        new RoomConfig
        {
            subjectLabel = "Matematika 3 - Dijeljenje i zadaci",
            keyItemName = "Broncani kljuc",
            keyPickUpMessage = "Uzeo si broncani kljuc i njime otkljucao vrata!",
            keyRevealLocalPos = KeyRevealPos,
            tasks = new[]
            {
                Typed("Upisi rezultat: 56 : 7 = ?", "8", "Tocno! 56 : 7 = 8.", TaskPos1,
                    "Sljedeci zadatak skriva se ondje gdje mudrost stoji na policama s knjigama."),
                MultipleChoice("Koliko je 81 : 9?", "9", new[] { "7", "8", "9" }, "Tocno! 81 : 9 = 9.", TaskPos2,
                    "Iduci trag ceka u tihom, udaljenom kutu prostorije."),
                Typed("Ana ima 3 vrecice po 12 bombona. Koliko ukupno? Upisi broj.", "36", "Tocno! 3 x 12 = 36.", TaskPos3,
                    "Sve je rijeseno! U sredini sobe zasjao je broncani kljuc - uzmi ga!")
            }
        }
    };

    private static TaskConfig MultipleChoice(string question, string answer, string[] choices, string correctFeedback, Vector3 localPosition, string hintAfterSolving)
    {
        return new TaskConfig
        {
            kind = PuzzleKind.MultipleChoice,
            question = question,
            correctAnswer = answer,
            choices = choices,
            correctFeedback = correctFeedback,
            wrongFeedback = "Nije tocno. Pokusaj ponovno.",
            localPosition = localPosition,
            hintAfterSolving = hintAfterSolving
        };
    }

    private static TaskConfig Typed(string question, string answer, string correctFeedback, Vector3 localPosition, string hintAfterSolving)
    {
        return new TaskConfig
        {
            kind = PuzzleKind.Typed,
            question = question,
            correctAnswer = answer,
            choices = null,
            correctFeedback = correctFeedback,
            wrongFeedback = "Nije tocno. Pokusaj ponovno.",
            localPosition = localPosition,
            hintAfterSolving = hintAfterSolving
        };
    }

    private sealed class LevelMaterials
    {
        public Material Floor;
        public Material Wall;
        public Material Ceiling;
        public Material Door;
        public Material Accent;
        public Material Podium;
        public Material Button;
        public Material Marker;
        public Material Board;
        public Material Terminal;
        public Material Furniture;
        public Material Key;
    }

    private sealed class InputReferences
    {
        public InputActionReference Move;
        public InputActionReference Look;
        public InputActionReference Jump;
        public InputActionReference Interact;
    }

    [MenuItem("Tools/Escape Room/Build Grade 4 Level")]
    public static void BuildFromMenu()
    {
        BuildGrade4Level(true, true);
    }

    public static bool BuildGrade4Level(bool promptBeforeReplace = true, bool showCompletionDialog = true)
    {
        bool savedScenes = Application.isBatchMode
            ? EditorSceneManager.SaveOpenScenes()
            : EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        if (!savedScenes)
        {
            return false;
        }

        if (promptBeforeReplace && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Zamijeniti scenu razine?",
                "Ovo ce zamijeniti Assets/Scenes/Grade4Level.unity. Prototip i ostale scene ostaju netaknuti.",
                "Zamijeni",
                "Odustani");

            if (!replace)
            {
                return false;
            }
        }

        EnsureFolder("Assets/Scenes");
        EnsureFolder(MaterialsFolder);
        EnsureFolder(InputReferencesFolder);

        LevelMaterials materials = CreateMaterials();
        InputReferences inputReferences = CreateInputReferences();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f);

        GameObject roomsRoot = new GameObject("Grade4_Rooms");
        GameObject lightingRoot = new GameObject("Grade4_Lighting");
        GameObject uiRoot = new GameObject("Grade4_UI");
        GameObject playerRoot = new GameObject("Grade4_Player");

        CreateSun(lightingRoot.transform);
        GameUIController uiController = CreateUI(uiRoot.transform);
        BuildLevel(roomsRoot.transform, lightingRoot.transform, uiController, materials);
        GameObject player = CreatePlayer(playerRoot.transform, uiController, inputReferences);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.SetActiveScene(scene);
        RegisterSceneInBuildSettings(ScenePath);
        Selection.activeGameObject = player;

        if (showCompletionDialog)
        {
            EditorUtility.DisplayDialog(
                "Razina je izgradjena",
                "Scena Grade4Level je spremna. Pritisni Play. Klikni kocku-zadatak (E) da se otvori pitanje; rjesavanjem se otkriva sljedeci, a zadnji otkriva kljuc kojim otkljucas vrata.",
                "U redu");
        }

        return true;
    }

    // ---------------------------------------------------------------------
    // Level assembly
    // ---------------------------------------------------------------------

    private static void BuildLevel(Transform root, Transform lightingRoot, GameUIController uiController, LevelMaterials materials)
    {
        Vector3 vestibuleCenter = Vector3.zero;

        CreateRoom(root, "Vestibule", vestibuleCenter, VestibuleSize, materials.Floor, materials.Wall, materials.Ceiling,
            new RoomOpening(RoomSide.North, 0f));
        CreateRoomLight(lightingRoot, vestibuleCenter, VestibuleSize);

        CreateHintProp(root, "Vestibule Welcome Sign",
            vestibuleCenter + new Vector3(2.4f, 1.4f, -1.5f), new Vector3(0.8f, 0.9f, 0.2f), materials.Accent,
            "Procitaj natpis", "Dobrodosao! Svaku kocku-zadatak klikni (E) da se otvori pitanje. Rijesis li ga, otkrit ce se iduci trag.", uiController);

        Vector3 entryDoorPosition = GetDoorwayPosition(vestibuleCenter, VestibuleSize, RoomSide.North, 0f);
        EscapeRoomDoor entryDoor = CreateDoor(root, "Entry Door", entryDoorPosition, RoomSide.North, false, uiController, null, materials.Door, materials.Marker);
        entryDoor.interactionPrompt = "Otvori vrata i udji u sobu";
        entryDoor.closePrompt = "Zatvori vrata";
        entryDoor.openMessage = "Klikni kocku-zadatak da se otvori pitanje. Rjesavanjem se otkriva iduci, a na kraju kljuc koji otkljucava vrata.";

        Vector3 currentCenter = vestibuleCenter + new Vector3(0f, 0f, VestibuleSize.y * 0.5f + RoomSize.y * 0.5f);

        for (int i = 0; i < RoomConfigs.Length; i++)
        {
            RoomConfig config = RoomConfigs[i];
            bool isLastRoom = i == RoomConfigs.Length - 1;

            CreateRoom(root, config.subjectLabel, currentCenter, RoomSize, materials.Floor, materials.Wall, materials.Ceiling,
                new RoomOpening(RoomSide.South, 0f),
                new RoomOpening(RoomSide.North, 0f));
            CreateRoomLight(lightingRoot, currentCenter, RoomSize);

            CreateFurnitureForRoom(root, config.subjectLabel, currentCenter, materials, uiController);
            CreateHintsForRoom(root, config.subjectLabel, currentCenter, materials, uiController);

            // Build the three task cubes (hidden/revealed in order by the sequence controller).
            List<PuzzleBase> taskPuzzles = new List<PuzzleBase>();
            List<GameObject> taskObjects = new List<GameObject>();
            List<string> hints = new List<string>();

            for (int t = 0; t < config.tasks.Length; t++)
            {
                TaskConfig task = config.tasks[t];
                Vector3 stationPosition = currentCenter + task.localPosition;
                string taskLabel = config.subjectLabel + " - Zadatak " + (t + 1);

                GameObject taskCube = CreateTaskCube(root, taskLabel, stationPosition, task, uiController, materials, out PuzzleBase puzzle);

                taskPuzzles.Add(puzzle);
                taskObjects.Add(taskCube);
                hints.Add(task.hintAfterSolving);
            }

            // Hidden key that appears after the last task; created inactive. Collected on click.
            Vector3 keyPosition = currentCenter + config.keyRevealLocalPos;
            KeyPickup key = CreateHiddenKey(root, config.subjectLabel + " Kljuc", keyPosition, config.keyItemName, config.keyPickUpMessage, uiController, materials);
            key.gameObject.SetActive(false);

            // Exit door starts locked; the sequence controller unlocks it once the key is collected.
            Vector3 exitDoorPosition = GetDoorwayPosition(currentCenter, RoomSize, RoomSide.North, 0f);
            EscapeRoomDoor exitDoor = CreateDoor(root, config.subjectLabel + " Exit Door", exitDoorPosition, RoomSide.North, true, uiController, null, materials.Door, materials.Marker);
            exitDoor.interactionPrompt = "Otvori sljedeca vrata";
            exitDoor.closePrompt = "Zatvori vrata";
            exitDoor.lockedPrompt = "Zakljucano - treba ti kljuc";
            exitDoor.lockedMessage = "Vrata su zakljucana. Rijesi sve zadatke i uzmi kljuc koji se pojavi.";
            exitDoor.messageDuration = 5f;

            RoomSequenceController controller = CreateRoomSequenceController(root, config.subjectLabel, taskPuzzles, taskObjects, hints, key, exitDoor, uiController);
            CreateRoomObjectiveTrigger(root, config.subjectLabel, currentCenter, controller);

            float nextSpan = isLastRoom ? AlcoveSize.y : RoomSize.y;
            currentCenter += new Vector3(0f, 0f, RoomSize.y * 0.5f + nextSpan * 0.5f);
        }

        CreateRoom(root, "Completion Alcove", currentCenter, AlcoveSize, materials.Floor, materials.Accent, materials.Ceiling,
            new RoomOpening(RoomSide.South, 0f));
        CreateRoomLight(lightingRoot, currentCenter, AlcoveSize);

        CreateHintProp(root, "Level Complete Sign",
            currentCenter + new Vector3(0f, 1.4f, AlcoveSize.y * 0.25f), new Vector3(2.2f, 0.9f, 0.12f), materials.Accent,
            "Procitaj natpis", "Cestitamo! Rijesio si sve sobe ove razine.", uiController);
    }

    // ---------------------------------------------------------------------
    // Task cube (one clickable cube = one task; opens a popup on interact)
    // ---------------------------------------------------------------------

    private static GameObject CreateTaskCube(Transform root, string label, Vector3 stationPosition, TaskConfig task, GameUIController uiController, LevelMaterials materials, out PuzzleBase puzzle)
    {
        Vector3 cubeSize = new Vector3(0.9f, 0.9f, 0.9f);
        Vector3 cubeCenter = stationPosition + new Vector3(0f, 1.0f, 0f); // hovers at ~eye level, easy to click
        Material cubeMaterial = task.kind == PuzzleKind.MultipleChoice ? materials.Board : materials.Terminal;

        GameObject cube = CreateCube(root, label + " Task", cubeCenter, Quaternion.identity, cubeSize, cubeMaterial);

        TaskStation station = cube.AddComponent<TaskStation>();
        station.questionText = task.question;
        station.promptText = "Otvori zadatak";
        station.uiController = uiController;

        if (task.kind == PuzzleKind.MultipleChoice)
        {
            MultipleChoicePuzzle mc = cube.AddComponent<MultipleChoicePuzzle>();
            mc.puzzleName = label;
            mc.questionText = task.question;
            mc.correctAnswer = task.correctAnswer;
            mc.correctFeedback = task.correctFeedback;
            mc.wrongFeedback = task.wrongFeedback;
            mc.solvedFeedback = string.Empty;
            mc.uiController = uiController;

            station.mode = TaskStation.TaskMode.MultipleChoice;
            station.options = task.choices ?? new[] { task.correctAnswer };
            station.puzzle = mc;
            puzzle = mc;
        }
        else
        {
            TypedAnswerPuzzle tp = cube.AddComponent<TypedAnswerPuzzle>();
            tp.puzzleName = label;
            tp.questionText = task.question;
            tp.acceptedAnswers = new[] { task.correctAnswer };
            tp.treatAsNumeric = true;
            tp.correctFeedback = task.correctFeedback;
            tp.wrongFeedback = task.wrongFeedback;
            tp.solvedFeedback = string.Empty;
            tp.uiController = uiController;

            station.mode = TaskStation.TaskMode.Typed;
            station.puzzle = tp;
            puzzle = tp;
        }

        // Small glowing marker on top so the cube reads as interactive.
        GameObject marker = CreateCube(cube.transform, label + " Marker", cubeCenter + new Vector3(0f, cubeSize.y * 0.5f + 0.1f, 0f), Quaternion.identity, new Vector3(0.22f, 0.1f, 0.22f), materials.Marker);
        RemoveCollider(marker);

        return cube;
    }

    // ---------------------------------------------------------------------
    // Sequence controller + objective trigger
    // ---------------------------------------------------------------------

    private static RoomSequenceController CreateRoomSequenceController(Transform root, string label, List<PuzzleBase> taskPuzzles, List<GameObject> taskObjects, List<string> hints, KeyPickup key, EscapeRoomDoor exitDoor, GameUIController uiController)
    {
        GameObject controllerObject = new GameObject(label + " Sequence");
        controllerObject.transform.SetParent(root);
        controllerObject.transform.position = Vector3.zero;

        RoomSequenceController controller = controllerObject.AddComponent<RoomSequenceController>();
        controller.roomLabel = label;
        controller.uiController = uiController;
        controller.taskPuzzles = taskPuzzles;
        controller.taskObjects = taskObjects;
        controller.hints = hints;
        controller.keyPickup = key;
        controller.keyObject = key != null ? key.gameObject : null;
        controller.exitDoor = exitDoor;
        return controller;
    }

    private static void CreateRoomObjectiveTrigger(Transform root, string label, Vector3 center, RoomSequenceController controller)
    {
        GameObject triggerObject = new GameObject(label + " Objective Trigger");
        triggerObject.transform.SetParent(root);
        triggerObject.transform.position = center + new Vector3(0f, WallHeight * 0.5f, 0f);

        BoxCollider box = triggerObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(RoomSize.x - 2f, WallHeight, RoomSize.y - 2f);

        Rigidbody rigidbody = triggerObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        RoomObjectiveTrigger trigger = triggerObject.AddComponent<RoomObjectiveTrigger>();
        trigger.room = controller;
    }

    // ---------------------------------------------------------------------
    // Furniture placeholders + clickable clue props
    // ---------------------------------------------------------------------

    private static void CreateFurnitureForRoom(Transform root, string subject, Vector3 center, LevelMaterials materials, GameUIController uiController)
    {
        CreateFurniturePlaceholder(root, subject + " Ormar (zapad)", center + new Vector3(-7.2f, 1.1f, -2.5f), new Vector3(0.6f, 2.2f, 1.4f), materials.Furniture, "Mjesto za ormar - ovdje ce kasnije doci 3D model.", uiController);
        CreateFurniturePlaceholder(root, subject + " Ormar (istok)", center + new Vector3(7.2f, 1.1f, -2.5f), new Vector3(0.6f, 2.2f, 1.4f), materials.Furniture, "Mjesto za ormar - ovdje ce kasnije doci 3D model.", uiController);
        CreateFurniturePlaceholder(root, subject + " Polica (zapad)", center + new Vector3(-7.1f, 1.1f, 4.5f), new Vector3(0.5f, 2.2f, 2.2f), materials.Furniture, "Mjesto za policu s knjigama (placeholder).", uiController);
        CreateFurniturePlaceholder(root, subject + " Polica (istok)", center + new Vector3(7.1f, 1.1f, 4.5f), new Vector3(0.5f, 2.2f, 2.2f), materials.Furniture, "Mjesto za policu s knjigama (placeholder).", uiController);
        CreateFurniturePlaceholder(root, subject + " Stol (JZ)", center + new Vector3(-6.0f, 0.45f, -5.4f), new Vector3(1.8f, 0.9f, 0.9f), materials.Furniture, "Mjesto za ucenicki stol (placeholder).", uiController);
        CreateFurniturePlaceholder(root, subject + " Stol (JI)", center + new Vector3(6.0f, 0.45f, -5.4f), new Vector3(1.8f, 0.9f, 0.9f), materials.Furniture, "Mjesto za ucenicki stol (placeholder).", uiController);
        CreateFurniturePlaceholder(root, subject + " Kutija", center + new Vector3(2.5f, 0.45f, -5.6f), new Vector3(0.9f, 0.9f, 0.9f), materials.Furniture, "Mjesto za kutiju s priborom (placeholder).", uiController);
    }

    private static void CreateHintsForRoom(Transform root, string subject, Vector3 center, LevelMaterials materials, GameUIController uiController)
    {
        CreateHintProp(root, subject + " Trag (knjiga)", center + new Vector3(6.6f, 1.5f, 4.5f), new Vector3(0.35f, 0.3f, 0.16f), materials.Accent,
            "Prelistaj knjigu", "U knjizi je zabiljezeno: jedan od zadataka cuva se ondje gdje mudrost stoji na policama.", uiController);
        CreateHintProp(root, subject + " Trag (poruka)", center + new Vector3(-6.0f, 1.0f, -5.4f), new Vector3(0.4f, 0.06f, 0.3f), materials.Accent,
            "Procitaj poruku", "Na stolu pise: ne zaboravi tihe kutove prostorije - ondje ceka jos jedan zadatak.", uiController);
        CreateHintProp(root, subject + " Trag (natpis)", center + new Vector3(2.4f, 1.6f, 6.2f), new Vector3(0.8f, 0.7f, 0.12f), materials.Accent,
            "Pregledaj natpis", "Natpis kraj vrata: tek kad rijesis sve zadatke, u sredini sobe zasjat ce kljuc.", uiController);
    }

    private static void CreateFurniturePlaceholder(Transform root, string name, Vector3 position, Vector3 size, Material material, string message, GameUIController uiController)
    {
        GameObject item = CreateCube(root, name, position, Quaternion.identity, size, material);
        MessageInteractable interactable = item.AddComponent<MessageInteractable>();
        interactable.promptText = "Pregledaj";
        interactable.messageText = message;
        interactable.uiController = uiController;
    }

    private static void CreateHintProp(Transform root, string name, Vector3 position, Vector3 size, Material material, string prompt, string message, GameUIController uiController)
    {
        GameObject prop = CreateCube(root, name, position, Quaternion.identity, size, material);
        MessageInteractable interactable = prop.AddComponent<MessageInteractable>();
        interactable.promptText = prompt;
        interactable.messageText = message;
        interactable.uiController = uiController;
    }

    // ---------------------------------------------------------------------
    // Collectible key (used up on click, unlocks the door; not carried)
    // ---------------------------------------------------------------------

    private static KeyPickup CreateHiddenKey(Transform root, string name, Vector3 position, string keyName, string pickUpMessage, GameUIController uiController, LevelMaterials materials)
    {
        GameObject key = CreateCube(root, name, position, Quaternion.Euler(0f, 45f, 0f), new Vector3(0.5f, 0.15f, 0.32f), materials.Key);

        KeyPickup pickup = key.AddComponent<KeyPickup>();
        pickup.keyName = keyName;
        pickup.pickUpPrompt = "Uzmi kljuc";
        pickup.pickUpMessage = pickUpMessage;
        pickup.uiController = uiController;
        pickup.hideOnCollect = true;
        return pickup;
    }

    // ---------------------------------------------------------------------
    // Geometry helpers
    // ---------------------------------------------------------------------

    private enum RoomSide { North, South, East, West }

    private readonly struct RoomOpening
    {
        public RoomOpening(RoomSide side, float offset, float width = DoorWidth, float height = DoorHeight)
        {
            Side = side;
            Offset = offset;
            Width = width;
            Height = height;
        }

        public RoomSide Side { get; }
        public float Offset { get; }
        public float Width { get; }
        public float Height { get; }
    }

    private static void CreateRoom(Transform root, string name, Vector3 center, Vector2 size, Material floorMaterial, Material wallMaterial, Material ceilingMaterial, params RoomOpening[] openings)
    {
        GameObject roomRoot = new GameObject(name);
        roomRoot.transform.SetParent(root);

        CreateCube(roomRoot.transform, "Floor", center + new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(size.x, 0.2f, size.y), floorMaterial);
        CreateCube(roomRoot.transform, "Ceiling", center + new Vector3(0f, WallHeight + 0.1f, 0f), Quaternion.identity, new Vector3(size.x, 0.2f, size.y), ceilingMaterial);

        CreateWall(roomRoot.transform, "North Wall", center, size, RoomSide.North, wallMaterial, openings);
        CreateWall(roomRoot.transform, "South Wall", center, size, RoomSide.South, wallMaterial, openings);
        CreateWall(roomRoot.transform, "East Wall", center, size, RoomSide.East, wallMaterial, openings);
        CreateWall(roomRoot.transform, "West Wall", center, size, RoomSide.West, wallMaterial, openings);
    }

    private static void CreateWall(Transform root, string name, Vector3 center, Vector2 size, RoomSide side, Material material, IReadOnlyCollection<RoomOpening> openings)
    {
        RoomOpening? opening = openings.Where(o => o.Side == side).Select(o => (RoomOpening?)o).FirstOrDefault();
        float wallLength = IsNorthSouth(side) ? size.x : size.y;
        float halfLength = wallLength * 0.5f;

        if (opening == null)
        {
            CreateWallSegment(root, name, center, size, side, 0f, wallLength, WallHeight, WallHeight * 0.5f, material);
            return;
        }

        RoomOpening o = opening.Value;
        float start = Mathf.Clamp(o.Offset - o.Width * 0.5f, -halfLength, halfLength);
        float end = Mathf.Clamp(o.Offset + o.Width * 0.5f, -halfLength, halfLength);

        if (start > -halfLength)
        {
            float segmentLength = start - (-halfLength);
            CreateWallSegment(root, name + " Left", center, size, side, -halfLength + segmentLength * 0.5f, segmentLength, WallHeight, WallHeight * 0.5f, material);
        }

        if (end < halfLength)
        {
            float segmentLength = halfLength - end;
            CreateWallSegment(root, name + " Right", center, size, side, end + segmentLength * 0.5f, segmentLength, WallHeight, WallHeight * 0.5f, material);
        }

        float lintelHeight = Mathf.Max(0.05f, WallHeight - o.Height);
        CreateWallSegment(root, name + " Lintel", center, size, side, o.Offset, o.Width, lintelHeight, o.Height + lintelHeight * 0.5f, material);
    }

    private static void CreateWallSegment(Transform root, string name, Vector3 center, Vector2 size, RoomSide side, float offset, float length, float height, float yPosition, Material material)
    {
        if (length <= 0.01f)
        {
            return;
        }

        Vector3 position = center + Vector3.up * yPosition;
        Vector3 scale;

        switch (side)
        {
            case RoomSide.North:
                position += new Vector3(offset, 0f, 0f);
                position.z += size.y * 0.5f + WallThickness * 0.5f;
                scale = new Vector3(length, height, WallThickness);
                break;
            case RoomSide.South:
                position += new Vector3(offset, 0f, 0f);
                position.z -= size.y * 0.5f + WallThickness * 0.5f;
                scale = new Vector3(length, height, WallThickness);
                break;
            case RoomSide.East:
                position += new Vector3(0f, 0f, offset);
                position.x += size.x * 0.5f + WallThickness * 0.5f;
                scale = new Vector3(WallThickness, height, length);
                break;
            default:
                position += new Vector3(0f, 0f, offset);
                position.x -= size.x * 0.5f + WallThickness * 0.5f;
                scale = new Vector3(WallThickness, height, length);
                break;
        }

        CreateCube(root, name, position, Quaternion.identity, scale, material);
    }

    private static EscapeRoomDoor CreateDoor(Transform root, string name, Vector3 position, RoomSide side, bool startsLocked, GameUIController uiController, PuzzleBase requiredPuzzle, Material material, Material markerMaterial)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(root);

        Vector3 slabScale = new Vector3(DoorWidth, DoorHeight, 0.25f);
        Vector3 slabLocalPosition;
        Vector3 pivotOffset;
        Vector3 openOffset;

        if (side == RoomSide.North)
        {
            slabLocalPosition = new Vector3(DoorWidth * 0.5f, 0f, 0f);
            pivotOffset = Vector3.left * (DoorWidth * 0.5f);
            openOffset = new Vector3(0f, -100f, 0f);
        }
        else
        {
            slabLocalPosition = new Vector3(-DoorWidth * 0.5f, 0f, 0f);
            pivotOffset = Vector3.right * (DoorWidth * 0.5f);
            openOffset = new Vector3(0f, 100f, 0f);
        }

        pivot.transform.position = position + pivotOffset + Vector3.up * (DoorHeight * 0.5f);

        GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "Door Slab";
        slab.transform.SetParent(pivot.transform, false);
        slab.transform.localPosition = slabLocalPosition;
        slab.transform.localRotation = Quaternion.identity;
        slab.transform.localScale = slabScale;
        SetMaterial(slab, material);

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Marker";
        marker.transform.SetParent(pivot.transform, false);
        marker.transform.localPosition = slabLocalPosition + Vector3.up * 0.6f + Vector3.forward * 0.16f;
        marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.04f);
        SetMaterial(marker, markerMaterial);
        RemoveCollider(marker);

        EscapeRoomDoor door = pivot.AddComponent<EscapeRoomDoor>();
        door.startsLocked = startsLocked;
        door.uiController = uiController;
        door.requiredPuzzle = requiredPuzzle;
        door.openEulerOffset = openOffset;
        door.unlockWhenPuzzleSolved = true;
        door.openWhenPuzzleSolved = requiredPuzzle != null;
        return door;
    }

    // ---------------------------------------------------------------------
    // Player + UI
    // ---------------------------------------------------------------------

    private static GameObject CreatePlayer(Transform root, GameUIController uiController, InputReferences inputReferences)
    {
        GameObject player = new GameObject("Player");
        player.transform.SetParent(root);
        player.transform.position = new Vector3(0f, 0f, -VestibuleSize.y * 0.5f + 1.2f);

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.35f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.stepOffset = 0.35f;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 200f;
        cameraObject.AddComponent<AudioListener>();

        GameObject holdPoint = new GameObject("Hold Point");
        holdPoint.transform.SetParent(cameraObject.transform, false);
        holdPoint.transform.localPosition = new Vector3(0.35f, -0.35f, 0.9f);
        holdPoint.transform.localRotation = Quaternion.identity;

        FirstPersonController firstPersonController = player.AddComponent<FirstPersonController>();
        firstPersonController.playerCamera = camera;
        firstPersonController.moveAction = inputReferences.Move;
        firstPersonController.lookAction = inputReferences.Look;
        firstPersonController.jumpAction = inputReferences.Jump;

        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        interactor.playerCamera = camera;
        interactor.uiController = uiController;
        interactor.interactAction = inputReferences.Interact;
        interactor.interactKeyHint = "E";
        interactor.interactionRange = 3.2f;

        CarryController carryController = player.AddComponent<CarryController>();
        carryController.playerCamera = camera;
        carryController.holdPoint = holdPoint.transform;

        return player;
    }

    private static GameUIController CreateUI(Transform root)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        GameObject canvasObject = new GameObject("Canvas");
        canvasObject.transform.SetParent(root);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameUIController uiController = canvasObject.AddComponent<GameUIController>();

        Text crosshair = CreateUIText(canvasObject.transform, "Crosshair", "+", font, 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
        crosshair.raycastTarget = false;

        Text prompt = CreateUIText(canvasObject.transform, "Interaction Prompt", string.Empty, font, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(960f, 70f));
        uiController.interactionPromptText = prompt;

        Text objective = CreateUIText(canvasObject.transform, "Objective Text", "Cilj: udji u prvu sobu. Klikni kocku-zadatak (E) da se otvori pitanje; tragovi te vode do kljuca.", font, 26, Color.white, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(860f, 150f));
        uiController.objectiveText = objective;

        GameObject panel = CreateUIRect(canvasObject.transform, "Message Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(1100f, 150f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.74f);
        uiController.messagePanel = panel;

        Text message = CreateUIText(panel.transform, "Message Text", string.Empty, font, 26, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform messageRect = message.GetComponent<RectTransform>();
        messageRect.offsetMin = new Vector2(22f, 12f);
        messageRect.offsetMax = new Vector2(-22f, -12f);
        uiController.messageText = message;

        BuildAnswerInputPanel(canvasObject.transform, uiController, font);
        BuildMultipleChoicePanel(canvasObject.transform, uiController, font);

        GameObject pausePanel = CreateUIRect(canvasObject.transform, "Pause Panel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image pauseImage = pausePanel.AddComponent<Image>();
        pauseImage.color = new Color(0f, 0f, 0f, 0.8f);

        Text pauseText = CreateUIText(pausePanel.transform, "Pause Text", "Pauza\nPritisni Esc za nastavak", font, 64, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 200f));
        pauseText.raycastTarget = false;
        pausePanel.SetActive(false);

        GamePauseController pauseController = canvasObject.AddComponent<GamePauseController>();
        pauseController.pausePanel = pausePanel;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.transform.SetParent(root);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();

        return uiController;
    }

    private static void BuildAnswerInputPanel(Transform canvasRoot, GameUIController uiController, Font font)
    {
        GameObject panel = CreateUIRect(canvasRoot, "Answer Input Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 280f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.07f, 0.94f);
        panel.SetActive(false);
        uiController.answerInputPanel = panel;

        Text question = CreateUIText(panel.transform, "Question Text", string.Empty, font, 26, Color.white, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-44f, 120f));
        question.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiController.answerInputQuestionText = question;

        GameObject fieldBackground = CreateUIRect(panel.transform, "Input Field", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 78f), new Vector2(560f, 72f));
        Image fieldImage = fieldBackground.AddComponent<Image>();
        fieldImage.color = new Color(1f, 1f, 1f, 0.96f);

        Text fieldText = CreateUIText(fieldBackground.transform, "Text", string.Empty, font, 28, Color.black, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform fieldTextRect = fieldText.GetComponent<RectTransform>();
        fieldTextRect.offsetMin = new Vector2(16f, 6f);
        fieldTextRect.offsetMax = new Vector2(-16f, -6f);

        Text placeholder = CreateUIText(fieldBackground.transform, "Placeholder", "Upisi odgovor ovdje...", font, 28, new Color(0f, 0f, 0f, 0.4f), TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.offsetMin = new Vector2(16f, 6f);
        placeholderRect.offsetMax = new Vector2(-16f, -6f);

        InputField inputField = fieldBackground.AddComponent<InputField>();
        inputField.textComponent = fieldText;
        inputField.placeholder = placeholder;
        inputField.contentType = InputField.ContentType.Standard;
        uiController.answerInputField = inputField;

        GameObject submitButtonObject = CreateUIRect(panel.transform, "Submit Button", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(240f, 54f));
        Image submitButtonImage = submitButtonObject.AddComponent<Image>();
        submitButtonImage.color = new Color(0.16f, 0.5f, 0.3f, 1f);
        Button submitButton = submitButtonObject.AddComponent<Button>();
        submitButton.onClick.AddListener(uiController.SubmitAnswerInput);

        Text submitText = CreateUIText(submitButtonObject.transform, "Text", "Potvrdi (Enter)", font, 22, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        submitText.raycastTarget = false;
    }

    private static void BuildMultipleChoicePanel(Transform canvasRoot, GameUIController uiController, Font font)
    {
        GameObject panel = CreateUIRect(canvasRoot, "Answer Choice Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.07f, 0.94f);
        panel.SetActive(false);
        uiController.answerChoicePanel = panel;

        Text question = CreateUIText(panel.transform, "Question Text", string.Empty, font, 28, Color.white, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(700f, 130f));
        question.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiController.answerChoiceQuestionText = question;

        int buttonCount = 4;
        Button[] buttons = new Button[buttonCount];

        for (int i = 0; i < buttonCount; i++)
        {
            GameObject buttonObject = CreateUIRect(panel.transform, "Option " + i, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f - i * 82f), new Vector2(620f, 70f));
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.16f, 0.34f, 0.5f, 1f);
            Button button = buttonObject.AddComponent<Button>();

            Text label = CreateUIText(buttonObject.transform, "Text", string.Empty, font, 26, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            label.raycastTarget = false;

            buttons[i] = button;
        }

        uiController.answerChoiceButtons = buttons;
    }

    private static Text CreateUIText(Transform parent, string name, string text, Font font, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = CreateUIRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        Text uiText = textObject.AddComponent<Text>();
        uiText.font = font;
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.color = color;
        uiText.alignment = alignment;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        return uiText;
    }

    private static GameObject CreateUIRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        return rectObject;
    }

    // ---------------------------------------------------------------------
    // Lighting
    // ---------------------------------------------------------------------

    private static void CreateSun(Transform root)
    {
        GameObject sunObject = new GameObject("Directional Light");
        sunObject.transform.SetParent(root);
        sunObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
    }

    private static void CreateRoomLight(Transform root, Vector3 center, Vector2 size)
    {
        GameObject lightObject = new GameObject("Room Light");
        lightObject.transform.SetParent(root);
        lightObject.transform.position = center + new Vector3(0f, WallHeight - 0.6f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = Mathf.Max(size.x, size.y) * 1.15f;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.96f, 0.88f);
    }

    // ---------------------------------------------------------------------
    // Primitive + material helpers
    // ---------------------------------------------------------------------

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.position = position;
        cube.transform.rotation = rotation;
        cube.transform.localScale = scale;
        SetMaterial(cube, material);
        return cube;
    }

    private static void RemoveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void SetMaterial(GameObject gameObject, Material material)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Vector3 GetDoorwayPosition(Vector3 center, Vector2 size, RoomSide side, float offset)
    {
        return side switch
        {
            RoomSide.North => center + new Vector3(offset, 0f, size.y * 0.5f),
            RoomSide.South => center + new Vector3(offset, 0f, -size.y * 0.5f),
            RoomSide.East => center + new Vector3(size.x * 0.5f, 0f, offset),
            _ => center + new Vector3(-size.x * 0.5f, 0f, offset)
        };
    }

    private static bool IsNorthSouth(RoomSide side)
    {
        return side == RoomSide.North || side == RoomSide.South;
    }

    private static LevelMaterials CreateMaterials()
    {
        return new LevelMaterials
        {
            Floor = CreateMaterial("Floor", new Color(0.22f, 0.24f, 0.26f)),
            Wall = CreateMaterial("Wall", new Color(0.6f, 0.62f, 0.64f)),
            Ceiling = CreateMaterial("Ceiling", new Color(0.36f, 0.38f, 0.4f)),
            Door = CreateMaterial("Door", new Color(0.4f, 0.24f, 0.13f)),
            Accent = CreateMaterial("Accent", new Color(0.1f, 0.66f, 0.95f), true),
            Podium = CreateMaterial("Podium", new Color(0.3f, 0.32f, 0.36f)),
            Button = CreateMaterial("Button", new Color(0.16f, 0.18f, 0.22f)),
            Marker = CreateMaterial("Marker", new Color(0.92f, 0.95f, 1f)),
            Board = CreateMaterial("Board", new Color(0.18f, 0.55f, 0.95f), true),
            Terminal = CreateMaterial("Terminal", new Color(0.1f, 0.62f, 0.6f), true),
            Furniture = CreateMaterial("Furniture", new Color(0.46f, 0.36f, 0.28f)),
            Key = CreateMaterial("Key", new Color(0.98f, 0.82f, 0.18f), true)
        };
    }

    private static Material CreateMaterial(string name, Color color, bool emission = false)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.5f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else if (material.HasProperty("_EmissionColor"))
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------------------------------------------------------------------
    // Input action references
    // ---------------------------------------------------------------------

    private static InputReferences CreateInputReferences()
    {
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        if (inputAsset == null)
        {
            Debug.LogWarning("Grade4LevelBuilder ne moze pronaci Assets/InputSystem_Actions.inputactions. Runtime skripte ce koristiti tipkovnicu i mis kao rezervni unos.");
            return new InputReferences();
        }

        InputActionMap playerMap = inputAsset.FindActionMap("Player", false);
        if (playerMap == null)
        {
            Debug.LogWarning("Grade4LevelBuilder ne moze pronaci Player action map. Runtime skripte ce koristiti tipkovnicu i mis kao rezervni unos.");
            return new InputReferences();
        }

        return new InputReferences
        {
            Move = CreateInputActionReference(playerMap, "Move"),
            Look = CreateInputActionReference(playerMap, "Look"),
            Jump = CreateInputActionReference(playerMap, "Jump"),
            Interact = CreateInputActionReference(playerMap, "Interact")
        };
    }

    private static InputActionReference CreateInputActionReference(InputActionMap map, string actionName)
    {
        InputAction action = map.FindAction(actionName, false);
        if (action == null)
        {
            Debug.LogWarning("Grade4LevelBuilder ne moze pronaci input akciju: " + actionName);
            return null;
        }

        string path = InputReferencesFolder + "/" + actionName + "ActionReference.asset";
        AssetDatabase.DeleteAsset(path);

        InputActionReference reference = InputActionReference.Create(action);
        reference.name = actionName + " Action Reference";
        AssetDatabase.CreateAsset(reference, path);
        return reference;
    }

    // ---------------------------------------------------------------------
    // Misc editor helpers
    // ---------------------------------------------------------------------

    private static void RegisterSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.Any(s => s.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string folder)
    {
        folder = folder.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string child = Path.GetFileName(folder);

        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }
}
