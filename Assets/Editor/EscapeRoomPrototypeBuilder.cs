using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EscapeRoomPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/EscapeRoomPrototype.unity";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    private const string GeneratedRoot = "Assets/Generated/EscapeRoomPrototype";
    private const string MaterialsFolder = GeneratedRoot + "/Materials";
    private const string InputReferencesFolder = GeneratedRoot + "/InputReferences";

    // Audio koraka (koristi Footsteps - Essentials pack).
    private const string FootstepWalkFolder = "Assets/Audio/SFX/Footsteps - Essentials/Footsteps_Tile/Footsteps_Tile_Walk";
    private const string FootstepRunFolder = "Assets/Audio/SFX/Footsteps - Essentials/Footsteps_Tile/Footsteps_Tile_Run";
    private const string DoorOpenClipPath = "Assets/Audio/SFX/Door, Cabinet and Locker Sound Pack (Free)/FREE VERSION/Open Door 7.wav";
    private const string DoorCloseClipPath = "Assets/Audio/SFX/Door, Cabinet and Locker Sound Pack (Free)/FREE VERSION/Close Door 6.wav";
    private const string DoorLockedClipPath = "Assets/Audio/SFX/Door, Cabinet and Locker Sound Pack (Free)/FREE VERSION/Locked Door 2.wav";
    private const string DoorUnlockClipPath = "Assets/Audio/SFX/Door, Cabinet and Locker Sound Pack (Free)/FREE VERSION/Unlock 1.wav";

    private const float WallHeight = 4f;
    private const float WallThickness = 0.3f;
    private const float DoorWidth = 2.4f;
    private const float DoorHeight = 3f;

    private enum RoomSide
    {
        North,
        South,
        East,
        West
    }

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

    private sealed class PrototypeMaterials
    {
        public Material Floor;
        public Material Wall;
        public Material Ceiling;
        public Material Door;
        public Material TutorialAccent;
        public Material MathAccent;
        public Material HistoryAccent;
        public Material ScienceAccent;
        public Material LanguageAccent;
        public Material LogicAccent;
        public Material Button;
        public Material CorrectButton;
        public Material Marker;
        public Material QuestionMarker;
        public Material DoorKnob;
        public Material MessageObject;
        public Material Lever;
    }

    private sealed class InputReferences
    {
        public InputActionReference Move;
        public InputActionReference Look;
        public InputActionReference Jump;
        public InputActionReference Interact;
    }

    public static string GeneratedScenePath => ScenePath;

    [MenuItem("Tools/Escape Room/Build Prototype Scene")]
    public static void BuildPrototypeSceneFromMenu()
    {
        BuildPrototypeScene(true, true);
    }

    public static bool BuildPrototypeScene(bool promptBeforeReplace = true, bool showCompletionDialog = true)
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
                "Zamijeniti scenu prototipa escape rooma?",
                "Ovo će zamijeniti Assets/Scenes/EscapeRoomPrototype.unity. Ostale scene i asseti ostaju netaknuti.",
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

        PrototypeMaterials materials = CreateMaterials();
        InputReferences inputReferences = CreateInputReferences();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.28f, 0.3f, 0.34f);

        GameObject roomsRoot = new GameObject("Prototype_Rooms");
        GameObject lightingRoot = new GameObject("Prototype_Lighting");
        GameObject uiRoot = new GameObject("Prototype_UI");
        GameObject playerRoot = new GameObject("Prototype_Player");

        CreateLighting(lightingRoot.transform);
        GameUIController uiController = CreateUI(uiRoot.transform);
        BuildRoomsAndPuzzles(roomsRoot.transform, uiController, materials);
        GameObject player = CreatePlayer(playerRoot.transform, uiController, inputReferences);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.SetActiveScene(scene);
        Selection.activeGameObject = player;

        if (showCompletionDialog)
        {
            EditorUtility.DisplayDialog(
                "Prototip escape rooma je izgrađen",
                "Scena prototipa je spremna. Pritisni Play za provjeru kretanja, interakcije, uvodnog otključavanja, vrata u središnjoj sobi i svih pet soba sa zagonetkama.",
                "U redu");
        }

        return true;
    }

    private static void BuildRoomsAndPuzzles(Transform root, GameUIController uiController, PrototypeMaterials materials)
    {
        Vector3 tutorialCenter = new Vector3(0f, 0f, 0f);
        Vector2 tutorialSize = new Vector2(12f, 10f);
        Vector3 hubCenter = new Vector3(0f, 0f, 12f);
        Vector2 hubSize = new Vector2(20f, 14f);

        Vector3 mathCenter = new Vector3(0f, 0f, 24f);
        Vector2 mathSize = new Vector2(12f, 10f);
        Vector3 historyCenter = new Vector3(-15f, 0f, 8f);
        Vector3 languageCenter = new Vector3(-15f, 0f, 16f);
        Vector3 scienceCenter = new Vector3(15f, 0f, 8f);
        Vector3 logicCenter = new Vector3(15f, 0f, 16f);
        Vector2 sideRoomSize = new Vector2(10f, 8f);

        CreateRoom(root, "Tutorial Room", tutorialCenter, tutorialSize, materials.Floor, materials.Wall, materials.Ceiling,
            new RoomOpening(RoomSide.North, 0f));

        CreateRoom(root, "Hub Room", hubCenter, hubSize, materials.Floor, materials.Wall, materials.Ceiling,
            new RoomOpening(RoomSide.South, 0f),
            new RoomOpening(RoomSide.North, 0f),
            new RoomOpening(RoomSide.West, -4f),
            new RoomOpening(RoomSide.West, 4f),
            new RoomOpening(RoomSide.East, -4f),
            new RoomOpening(RoomSide.East, 4f));

        CreatePuzzleRoomShell(root, "Matematika", mathCenter, mathSize, RoomSide.South, RoomSide.North, materials, materials.MathAccent);
        CreatePuzzleRoomShell(root, "Povijest", historyCenter, sideRoomSize, RoomSide.East, RoomSide.West, materials, materials.HistoryAccent);
        CreatePuzzleRoomShell(root, "Priroda", scienceCenter, sideRoomSize, RoomSide.West, RoomSide.East, materials, materials.ScienceAccent);
        CreatePuzzleRoomShell(root, "Hrvatski jezik", languageCenter, sideRoomSize, RoomSide.East, RoomSide.West, materials, materials.LanguageAccent);
        CreatePuzzleRoomShell(root, "Logika", logicCenter, sideRoomSize, RoomSide.West, RoomSide.East, materials, materials.LogicAccent);

        EscapeRoomDoor tutorialDoor = CreateDoor(root, "Tutorial Exit Door", GetDoorwayPosition(tutorialCenter, tutorialSize, RoomSide.North, 0f), RoomSide.North, true, uiController, null, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        tutorialDoor.interactionPrompt = "Otvori izlaz iz uvodne sobe";
        tutorialDoor.closePrompt = "Zatvori izlaz iz uvodne sobe";
        tutorialDoor.lockedPrompt = "Izlaz je zaključan";
        tutorialDoor.lockedMessage = "Izlaz iz uvodne sobe je zaključan. Pokušaj s polugom pokraj svjetleće kocke.";

        CreateTutorialObjects(root, tutorialDoor, uiController, materials);

        CreateSubjectDoor(root, "Matematika", GetDoorwayPosition(hubCenter, hubSize, RoomSide.North, 0f), RoomSide.North, uiController, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        CreateSubjectDoor(root, "Povijest", GetDoorwayPosition(hubCenter, hubSize, RoomSide.West, -4f), RoomSide.West, uiController, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        CreateSubjectDoor(root, "Priroda", GetDoorwayPosition(hubCenter, hubSize, RoomSide.East, -4f), RoomSide.East, uiController, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        CreateSubjectDoor(root, "Hrvatski jezik", GetDoorwayPosition(hubCenter, hubSize, RoomSide.West, 4f), RoomSide.West, uiController, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        CreateSubjectDoor(root, "Logika", GetDoorwayPosition(hubCenter, hubSize, RoomSide.East, 4f), RoomSide.East, uiController, materials.Door, materials.QuestionMarker, materials.DoorKnob);

        CreateMultipleChoicePuzzle(root, "Matematika", mathCenter, mathSize, RoomSide.North,
            "U učionici je 7 karata Hrvatske, a u knjižnici još 5. Koliko ih je ukupno?",
            "12",
            new[] { "10", "12", "14" },
            "Točno. 7 + 5 = 12.",
            "Nije točno. Dodaj 5 na 7 i pokušaj ponovno.",
            uiController,
            materials,
            materials.MathAccent);

        CreateMultipleChoicePuzzle(root, "Povijest", historyCenter, sideRoomSize, RoomSide.West,
            "Koje je godine Hrvatska međunarodno priznata?",
            "1992",
            new[] { "1991", "1992", "1995" },
            "Točno. Hrvatska je međunarodno priznata 15. siječnja 1992.",
            "Nije točno. Razmisli o datumu međunarodnog priznanja Hrvatske.",
            uiController,
            materials,
            materials.HistoryAccent);

        CreateMultipleChoicePuzzle(root, "Priroda", scienceCenter, sideRoomSize, RoomSide.East,
            "Koja hrvatska rijeka tvori poznate slapove u Nacionalnom parku Krka?",
            "Krka",
            new[] { "Sava", "Krka", "Drava" },
            "Točno. Rijeka Krka tvori poznate slapove u istoimenom nacionalnom parku.",
            "Nije točno. Traži rijeku po kojoj je nacionalni park dobio ime.",
            uiController,
            materials,
            materials.ScienceAccent);

        CreateMultipleChoicePuzzle(root, "Hrvatski jezik", languageCenter, sideRoomSize, RoomSide.West,
            "Koja riječ znači učenik i pravilno koristi slovo đ?",
            "đak",
            new[] { "džep", "đak", "čamac" },
            "Točno. Đak je učenik, a riječ koristi hrvatsko slovo đ.",
            "Nije točno. Traži riječ za učenika i pazi na znakove č, ć, đ i dž.",
            uiController,
            materials,
            materials.LanguageAccent);

        CreateSequencePuzzle(root, "Logika", logicCenter, sideRoomSize, RoomSide.East, uiController, materials, materials.LogicAccent);

        CreateDeadEndDoor(root, "Povijest", historyCenter, sideRoomSize, RoomSide.South, uiController, materials.Door, materials.QuestionMarker);
        CreateDeadEndDoor(root, "Priroda", scienceCenter, sideRoomSize, RoomSide.North, uiController, materials.Door, materials.QuestionMarker);
    }

    private static void CreatePuzzleRoomShell(Transform root, string subject, Vector3 center, Vector2 size, RoomSide entranceSide, RoomSide finishSide, PrototypeMaterials materials, Material accentMaterial)
    {
        CreateRoom(root, subject + " Room", center, size, materials.Floor, materials.Wall, materials.Ceiling,
            new RoomOpening(entranceSide, 0f),
            new RoomOpening(finishSide, 0f));

        Vector2 alcoveSize = IsNorthSouth(finishSide) ? new Vector2(6f, 4f) : new Vector2(4f, 5f);
        Vector3 alcoveCenter = GetAdjacentCenter(center, size, finishSide, alcoveSize);

        CreateRoom(root, subject + " Completion Alcove", alcoveCenter, alcoveSize, materials.Floor, accentMaterial, materials.Ceiling,
            new RoomOpening(GetOppositeSide(finishSide), 0f));

    }

    private static void CreateTutorialObjects(Transform root, EscapeRoomDoor tutorialDoor, GameUIController uiController, PrototypeMaterials materials)
    {
        CreateMessageInteractable(
            root,
            "Tutorial Message Cube",
            new Vector3(-3f, 0.75f, -0.5f),
            new Vector3(1f, 1f, 1f),
            materials.MessageObject,
            "Pregledaj svjetleću kocku",
            "Dobro. Ovo je predmet za interakciju. Pogledaj predmete i pritisni E kako bi nešto naučio ili aktivirao.",
            uiController);

        MessageInteractable lever = CreateMessageInteractable(
            root,
            "Tutorial Exit Lever",
            new Vector3(3f, 0.75f, -0.5f),
            new Vector3(0.7f, 1.2f, 0.45f),
            materials.Lever,
            "Povuci polugu izlaza",
            "Izlaz iz uvodne sobe je otključan. Otvori vrata i uđi u središnju sobu.",
            uiController);

        lever.doorToUnlock = tutorialDoor;
        lever.openDoorAfterUnlock = false;

    }

    private static void CreateSubjectDoor(Transform root, string subject, Vector3 position, RoomSide side, GameUIController uiController, Material doorMaterial, Material questionMarkerMaterial, Material knobMaterial)
    {
        EscapeRoomDoor door = CreateDoor(root, subject + " Door", position, side, false, uiController, null, doorMaterial, questionMarkerMaterial, knobMaterial);
        door.interactionPrompt = "Otvori sobu: " + subject;
        door.closePrompt = "Zatvori sobu: " + subject;
        door.lockedPrompt = "Soba je zaključana";
        door.lockedMessage = "Vrata sobe " + subject + " su zaključana.";
        door.openMessage = "Soba: " + subject + ". Pronađi vrata s ? za pitanje, zatim pogledaj ! oznake za odgovore.";
    }

    private static void CreateDeadEndDoor(Transform root, string subject, Vector3 roomCenter, Vector2 roomSize, RoomSide side, GameUIController uiController, Material doorMaterial, Material markerMaterial)
    {
        Vector3 doorPosition = GetDoorwayPosition(roomCenter, roomSize, side, 0f) - GetOutwardDirection(side) * 0.16f + Vector3.up * 1.5f;
        Vector3 doorScale = IsNorthSouth(side) ? new Vector3(2.1f, 3f, 0.18f) : new Vector3(0.18f, 3f, 2.1f);

        MessageInteractable deadEnd = CreateMessageInteractable(
            root,
            subject + " Dead End Door",
            doorPosition,
            doorScale,
            doorMaterial,
            "Provjeri slijepi put",
            "Ovaj put je slijep. Vrati se na zagonetku i odaberi korisne tragove.",
            uiController);

        deadEnd.openDoorAfterUnlock = false;
        CreateWorldQuestionMarker(root, subject + " Dead End Marker", doorPosition + Vector3.up * 0.75f + GetInwardDirection(side) * 0.16f, side, markerMaterial);
    }

    private static MultipleChoicePuzzle CreateMultipleChoicePuzzle(Transform root, string subject, Vector3 center, Vector2 size, RoomSide finishSide, string question, string correctAnswer, string[] answers, string correctFeedback, string wrongFeedback, GameUIController uiController, PrototypeMaterials materials, Material accentMaterial)
    {
        GameObject puzzleObject = new GameObject(subject + " Puzzle Manager");
        puzzleObject.transform.SetParent(root);
        puzzleObject.transform.position = center;

        MultipleChoicePuzzle puzzle = puzzleObject.AddComponent<MultipleChoicePuzzle>();
        puzzle.puzzleName = subject;
        puzzle.questionText = question;
        puzzle.correctAnswer = correctAnswer;
        puzzle.correctFeedback = correctFeedback;
        puzzle.wrongFeedback = wrongFeedback;
        puzzle.solvedFeedback = "Zagonetka iz predmeta " + subject + " je riješena. Završna vrata su otvorena.";
        puzzle.uiController = uiController;

        Vector3 buttonAxis = IsNorthSouth(finishSide) ? Vector3.right : Vector3.forward;
        Vector3 buttonBase = center - GetOutwardDirection(finishSide) * 1.3f + Vector3.up * 0.65f;
        float spacing = 2.35f;

        for (int i = 0; i < answers.Length; i++)
        {
            float offset = (i - (answers.Length - 1) * 0.5f) * spacing;
            Vector3 buttonPosition = buttonBase + buttonAxis * offset;
            CreateAnswerButton(root, subject + " Answer " + answers[i], buttonPosition, answers[i], puzzle, materials.Button, materials.Marker);
        }

        EscapeRoomDoor finishDoor = CreateDoor(root, subject + " Finish Door", GetDoorwayPosition(center, size, finishSide, 0f), finishSide, true, uiController, puzzle, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        finishDoor.interactionPrompt = "Otvori završna vrata: " + subject;
        finishDoor.closePrompt = "Zatvori završna vrata: " + subject;
        finishDoor.lockedPrompt = "Pročitaj pitanje: " + subject;
        finishDoor.lockedMessage = question + "\nPogledaj svaku ! oznaku za pregled odgovora, zatim odaberi točan blok.";
        finishDoor.messageDuration = 7f;
        finishDoor.openWhenPuzzleSolved = true;

        return puzzle;
    }

    private static void CreateSequencePuzzle(Transform root, string subject, Vector3 center, Vector2 size, RoomSide finishSide, GameUIController uiController, PrototypeMaterials materials, Material accentMaterial)
    {
        GameObject puzzleObject = new GameObject(subject + " Puzzle Manager");
        puzzleObject.transform.SetParent(root);
        puzzleObject.transform.position = center;

        SequencePuzzle puzzle = puzzleObject.AddComponent<SequencePuzzle>();
        puzzle.puzzleName = subject;
        puzzle.expectedSequence = new[] { 1, 2, 3 };
        puzzle.progressFeedback = "Točan korak. Nastavi niz.";
        puzzle.resetFeedback = "Pogrešan redoslijed. Kreni ponovno od 1.";
        puzzle.solvedFeedback = "Logička zagonetka je riješena. Završna vrata su otvorena.";
        puzzle.uiController = uiController;

        Vector3 buttonAxis = IsNorthSouth(finishSide) ? Vector3.right : Vector3.forward;
        Vector3 buttonBase = center - GetOutwardDirection(finishSide) * 1.3f + Vector3.up * 0.65f;
        float spacing = 2.25f;

        for (int step = 1; step <= 3; step++)
        {
            float offset = (step - 2) * spacing;
            Vector3 buttonPosition = buttonBase + buttonAxis * offset;
            string city = step == 1 ? "Rijeka" : step == 2 ? "Zadar" : "Split";
            CreateSequenceButton(root, subject + " Sequence Button " + step, buttonPosition, step, city, puzzle, materials.Button, materials.Marker);
        }

        EscapeRoomDoor finishDoor = CreateDoor(root, subject + " Finish Door", GetDoorwayPosition(center, size, finishSide, 0f), finishSide, true, uiController, puzzle, materials.Door, materials.QuestionMarker, materials.DoorKnob);
        finishDoor.interactionPrompt = "Otvori završna vrata: Logika";
        finishDoor.closePrompt = "Zatvori završna vrata: Logika";
        finishDoor.lockedPrompt = "Pročitaj pitanje: Logika";
        finishDoor.lockedMessage = "Logičko pitanje: poredaj hrvatske obalne gradove od sjevera prema jugu: Rijeka, Zadar, Split. Pogledaj svaku ! oznaku da vidiš grad.";
        finishDoor.messageDuration = 7f;
        finishDoor.openWhenPuzzleSolved = true;
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
        List<RoomOpening> sideOpenings = openings
            .Where(opening => opening.Side == side)
            .OrderBy(opening => opening.Offset)
            .ToList();

        float wallLength = IsNorthSouth(side) ? size.x : size.y;
        float halfLength = wallLength * 0.5f;
        float cursor = -halfLength;

        if (sideOpenings.Count == 0)
        {
            CreateWallSegment(root, name, center, side, 0f, wallLength, WallHeight, WallHeight * 0.5f, material);
            return;
        }

        int segmentIndex = 0;
        foreach (RoomOpening opening in sideOpenings)
        {
            float start = Mathf.Clamp(opening.Offset - opening.Width * 0.5f, -halfLength, halfLength);
            float end = Mathf.Clamp(opening.Offset + opening.Width * 0.5f, -halfLength, halfLength);

            if (start > cursor)
            {
                float segmentLength = start - cursor;
                float segmentOffset = cursor + segmentLength * 0.5f;
                CreateWallSegment(root, name + " Segment " + segmentIndex, center, side, segmentOffset, segmentLength, WallHeight, WallHeight * 0.5f, material);
                segmentIndex++;
            }

            float lintelHeight = Mathf.Max(0.05f, WallHeight - opening.Height);
            CreateWallSegment(root, name + " Lintel " + segmentIndex, center, side, opening.Offset, opening.Width, lintelHeight, opening.Height + lintelHeight * 0.5f, material);
            cursor = end;
        }

        if (cursor < halfLength)
        {
            float segmentLength = halfLength - cursor;
            float segmentOffset = cursor + segmentLength * 0.5f;
            CreateWallSegment(root, name + " Segment " + segmentIndex, center, side, segmentOffset, segmentLength, WallHeight, WallHeight * 0.5f, material);
        }
    }

    private static void CreateWallSegment(Transform root, string name, Vector3 center, RoomSide side, float offset, float length, float height, float yPosition, Material material)
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
                position.z += GetRoomHalfLength(root) + WallThickness * 0.5f;
                scale = new Vector3(length, height, WallThickness);
                break;
            case RoomSide.South:
                position += new Vector3(offset, 0f, 0f);
                position.z -= GetRoomHalfLength(root) + WallThickness * 0.5f;
                scale = new Vector3(length, height, WallThickness);
                break;
            case RoomSide.East:
                position += new Vector3(0f, 0f, offset);
                position.x += GetRoomHalfWidth(root) + WallThickness * 0.5f;
                scale = new Vector3(WallThickness, height, length);
                break;
            default:
                position += new Vector3(0f, 0f, offset);
                position.x -= GetRoomHalfWidth(root) + WallThickness * 0.5f;
                scale = new Vector3(WallThickness, height, length);
                break;
        }

        CreateCube(root, name, position, Quaternion.identity, scale, material);
    }

    private static float GetRoomHalfWidth(Transform roomRoot)
    {
        Transform floor = roomRoot.Find("Floor");
        return floor != null ? floor.localScale.x * 0.5f : 5f;
    }

    private static float GetRoomHalfLength(Transform roomRoot)
    {
        Transform floor = roomRoot.Find("Floor");
        return floor != null ? floor.localScale.z * 0.5f : 5f;
    }

    private static EscapeRoomDoor CreateDoor(Transform root, string name, Vector3 position, RoomSide side, bool startsLocked, GameUIController uiController, PuzzleBase requiredPuzzle, Material material, Material questionMarkerMaterial, Material knobMaterial)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(root);
        Vector3 slabScale;
        Vector3 slabLocalPosition;
        Vector3 pivotOffset;
        Vector3 openOffset;

        switch (side)
        {
            case RoomSide.North:
                slabScale = new Vector3(DoorWidth, DoorHeight, 0.25f);
                slabLocalPosition = new Vector3(DoorWidth * 0.5f, 0f, 0f);
                pivotOffset = Vector3.left * (DoorWidth * 0.5f);
                openOffset = new Vector3(0f, -100f, 0f);
                break;
            case RoomSide.South:
                slabScale = new Vector3(DoorWidth, DoorHeight, 0.25f);
                slabLocalPosition = new Vector3(-DoorWidth * 0.5f, 0f, 0f);
                pivotOffset = Vector3.right * (DoorWidth * 0.5f);
                openOffset = new Vector3(0f, 100f, 0f);
                break;
            case RoomSide.East:
                slabScale = new Vector3(0.25f, DoorHeight, DoorWidth);
                slabLocalPosition = new Vector3(0f, 0f, -DoorWidth * 0.5f);
                pivotOffset = Vector3.forward * (DoorWidth * 0.5f);
                openOffset = new Vector3(0f, -100f, 0f);
                break;
            default:
                slabScale = new Vector3(0.25f, DoorHeight, DoorWidth);
                slabLocalPosition = new Vector3(0f, 0f, DoorWidth * 0.5f);
                pivotOffset = Vector3.back * (DoorWidth * 0.5f);
                openOffset = new Vector3(0f, 100f, 0f);
                break;
        }

        pivot.transform.position = position + pivotOffset + Vector3.up * (DoorHeight * 0.5f);

        GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "Door Slab";
        slab.transform.SetParent(pivot.transform, false);

        slab.transform.localPosition = slabLocalPosition;
        slab.transform.localRotation = Quaternion.identity;
        slab.transform.localScale = slabScale;
        SetMaterial(slab, material);
        CreateDoorQuestionMarker(pivot.transform, side, slabLocalPosition, questionMarkerMaterial);
        CreateDoorKnobs(pivot.transform, side, slabLocalPosition, knobMaterial);

        EscapeRoomDoor door = pivot.AddComponent<EscapeRoomDoor>();
        door.startsLocked = startsLocked;
        door.uiController = uiController;
        door.requiredPuzzle = requiredPuzzle;
        door.openEulerOffset = openOffset;
        door.unlockWhenPuzzleSolved = true;
        door.openWhenPuzzleSolved = requiredPuzzle != null;
        door.openClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorOpenClipPath);
        door.closeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorCloseClipPath);
        door.lockedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorLockedClipPath);
        door.unlockClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorUnlockClipPath);
        return door;
    }

    private static MessageInteractable CreateMessageInteractable(Transform root, string name, Vector3 position, Vector3 scale, Material material, string prompt, string message, GameUIController uiController)
    {
        GameObject item = CreateCube(root, name, position, Quaternion.identity, scale, material);
        MessageInteractable interactable = item.AddComponent<MessageInteractable>();
        interactable.promptText = prompt;
        interactable.messageText = message;
        interactable.uiController = uiController;

        if (material == null || material != null && material.name.Contains("Glow"))
        {
            Light light = item.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f;
            light.intensity = 1.2f;
            light.color = Color.cyan;
        }

        return interactable;
    }

    private static void CreateAnswerButton(Transform root, string name, Vector3 position, string answer, MultipleChoicePuzzle puzzle, Material material, Material markerMaterial)
    {
        Vector3 buttonScale = new Vector3(1.6f, 0.5f, 1.1f);
        GameObject button = CreateCube(root, name, position, Quaternion.identity, buttonScale, material);
        PuzzleAnswerButton answerButton = button.AddComponent<PuzzleAnswerButton>();
        answerButton.answerText = answer;
        answerButton.puzzle = puzzle;
        answerButton.promptText = "Odaberi odgovor";

        CreateTopExclamationMarker(root, name + " Marker", position, buttonScale, markerMaterial);
    }

    private static void CreateSequenceButton(Transform root, string name, Vector3 position, int step, string displayText, SequencePuzzle puzzle, Material material, Material markerMaterial)
    {
        Vector3 buttonScale = new Vector3(1.4f, 0.5f, 1.1f);
        GameObject button = CreateCube(root, name, position, Quaternion.identity, buttonScale, material);
        SequenceButton sequenceButton = button.AddComponent<SequenceButton>();
        sequenceButton.stepNumber = step;
        sequenceButton.displayText = displayText;
        sequenceButton.puzzle = puzzle;
        sequenceButton.promptText = "Odaberi grad";

        CreateTopExclamationMarker(root, name + " Marker", position, buttonScale, markerMaterial);
    }

    private static GameObject CreatePlayer(Transform root, GameUIController uiController, InputReferences inputReferences)
    {
        GameObject player = new GameObject("Player");
        player.transform.SetParent(root);
        player.transform.position = new Vector3(0f, 0f, -3.5f);

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
        camera.farClipPlane = 120f;
        cameraObject.AddComponent<AudioListener>();

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

        AttachFootsteps(player);

        return player;
    }

    // Dodaje FootstepAudio na igraca i puni walk/run isjecke iz Footsteps packa.
    private static void AttachFootsteps(GameObject player)
    {
        FootstepAudio footsteps = player.AddComponent<FootstepAudio>();
        footsteps.walkClips = LoadClipsFromFolder(FootstepWalkFolder);
        footsteps.runClips = LoadClipsFromFolder(FootstepRunFolder);

        if (footsteps.walkClips == null || footsteps.walkClips.Length == 0)
        {
            Debug.LogWarning("AttachFootsteps ne moze pronaci zvukove koraka u " + FootstepWalkFolder + ". Koraci nece svirati dok se ne dodijele isjecci.");
        }
    }

    private static AudioClip[] LoadClipsFromFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return new AudioClip[0];
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        List<AudioClip> clips = new List<AudioClip>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                clips.Add(clip);
            }
        }

        clips.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return clips.ToArray();
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

        GameUIController uiController = canvasObject.AddComponent<GameUIController>();

        Text crosshair = CreateUIText(canvasObject.transform, "Crosshair", "+", font, 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(40f, 40f));
        crosshair.raycastTarget = false;

        Text prompt = CreateUIText(canvasObject.transform, "Interaction Prompt", string.Empty, font, 30, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(900f, 60f));
        uiController.interactionPromptText = prompt;

        Text objective = CreateUIText(canvasObject.transform, "Objective Text", "Cilj: Dovrši uvod, zatim odaberi predmetnu sobu.", font, 26, Color.white, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430f, -36f), new Vector2(780f, 80f));
        uiController.objectiveText = objective;

        GameObject panel = CreateUIRect(canvasObject.transform, "Message Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(1040f, 140f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        uiController.messagePanel = panel;

        Text message = CreateUIText(panel.transform, "Message Text", string.Empty, font, 24, Color.white, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        RectTransform messageRect = message.GetComponent<RectTransform>();
        messageRect.offsetMin = new Vector2(18f, 10f);
        messageRect.offsetMax = new Vector2(-18f, -10f);
        uiController.messageText = message;

        GameObject pausePanel = CreateUIRect(canvasObject.transform, "Pause Panel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image pauseImage = pausePanel.AddComponent<Image>();
        pauseImage.color = new Color(0f, 0f, 0f, 0.78f);

        Text pauseText = CreateUIText(pausePanel.transform, "Pause Text", "Pauza\nPritisni Esc za nastavak", font, 64, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 180f));
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

    private static Text CreateUIText(Transform parent, string name, string text, Font font, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = CreateUIRect(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta);
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

    private static GameObject CreateUIRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        RectTransform rectTransform = rectObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        return rectObject;
    }

    private static void CreateLighting(Transform root)
    {
        GameObject sunObject = new GameObject("Directional Light");
        sunObject.transform.SetParent(root);
        sunObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.5f;
        sun.shadows = LightShadows.Soft;

        Vector3[] lightPositions =
        {
            new Vector3(0f, 3.4f, 0f),
            new Vector3(0f, 3.4f, 12f),
            new Vector3(0f, 3.4f, 24f),
            new Vector3(-15f, 3.4f, 8f),
            new Vector3(15f, 3.4f, 8f),
            new Vector3(-15f, 3.4f, 16f),
            new Vector3(15f, 3.4f, 16f)
        };

        for (int i = 0; i < lightPositions.Length; i++)
        {
            GameObject lightObject = new GameObject("Room Light " + (i + 1));
            lightObject.transform.SetParent(root);
            lightObject.transform.position = lightPositions[i];
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 11f;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.95f, 0.86f);
        }
    }

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

    private static void SetMaterial(GameObject gameObject, Material material)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void CreateDoorQuestionMarker(Transform doorPivot, RoomSide side, Vector3 slabLocalPosition, Material markerMaterial)
    {
        Vector3 localPosition = slabLocalPosition + Vector3.up * 0.5f + GetInwardDirection(side) * 0.18f;
        CreateSolidQuestionMarker(doorPivot, "Question Mark", localPosition, side, markerMaterial);
    }

    private static void CreateWorldQuestionMarker(Transform parent, string name, Vector3 position, RoomSide side, Material markerMaterial)
    {
        GameObject markerRoot = new GameObject(name);
        markerRoot.transform.SetParent(parent);
        markerRoot.transform.position = position;
        CreateSolidQuestionMarker(markerRoot.transform, "Question Mark", Vector3.zero, side, markerMaterial);
    }

    private static void CreateDoorKnobs(Transform doorPivot, RoomSide side, Vector3 slabLocalPosition, Material knobMaterial)
    {
        Vector3 latchDirection = slabLocalPosition;
        latchDirection.y = 0f;

        if (latchDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        latchDirection.Normalize();
        Vector3 latchPosition = latchDirection * (DoorWidth - 0.32f);
        latchPosition.y = -0.35f;
        float faceOffset = 0.25f * 0.5f + 0.12f;

        CreateLocalKnob(doorPivot, "Inner Door Knob", latchPosition + GetInwardDirection(side) * faceOffset, knobMaterial);
        CreateLocalKnob(doorPivot, "Outer Door Knob", latchPosition + GetOutwardDirection(side) * faceOffset, knobMaterial);
    }

    private static void CreateLocalKnob(Transform parent, string name, Vector3 localPosition, Material material)
    {
        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knob.name = name;
        knob.transform.SetParent(parent, false);
        knob.transform.localPosition = localPosition;
        knob.transform.localRotation = Quaternion.identity;
        knob.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        SetMaterial(knob, material);
    }

    private static void CreateSolidQuestionMarker(Transform parent, string name, Vector3 localPosition, RoomSide side, Material markerMaterial)
    {
        GameObject markerRoot = new GameObject(name);
        markerRoot.transform.SetParent(parent, false);
        markerRoot.transform.localPosition = localPosition;

        CreateQuestionPiece(markerRoot.transform, "Top", side, new Vector2(0f, 0.3f), new Vector2(0.5f, 0.08f), markerMaterial);
        CreateQuestionPiece(markerRoot.transform, "Upper Right", side, new Vector2(0.21f, 0.16f), new Vector2(0.08f, 0.28f), markerMaterial);
        CreateQuestionPiece(markerRoot.transform, "Middle", side, new Vector2(0.04f, 0.02f), new Vector2(0.34f, 0.08f), markerMaterial);
        CreateQuestionPiece(markerRoot.transform, "Lower", side, new Vector2(-0.11f, -0.16f), new Vector2(0.08f, 0.27f), markerMaterial);
        CreateQuestionPiece(markerRoot.transform, "Dot", side, new Vector2(-0.11f, -0.42f), new Vector2(0.13f, 0.13f), markerMaterial);
    }

    private static void CreateQuestionPiece(Transform parent, string name, RoomSide side, Vector2 center, Vector2 size, Material material)
    {
        float horizontal = center.x * GetQuestionHorizontalSign(side);

        Vector3 localPosition = IsNorthSouth(side)
            ? new Vector3(horizontal, center.y, 0f)
            : new Vector3(0f, center.y, horizontal);

        Vector3 localScale = IsNorthSouth(side)
            ? new Vector3(size.x, size.y, 0.045f)
            : new Vector3(0.045f, size.y, size.x);

        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Quaternion.identity;
        piece.transform.localScale = localScale;
        SetMaterial(piece, material);

        Collider collider = piece.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }
    }

    private static float GetQuestionHorizontalSign(RoomSide side)
    {
        return side == RoomSide.South || side == RoomSide.East ? -1f : 1f;
    }

    private static void CreateTopExclamationMarker(Transform parent, string name, Vector3 buttonPosition, Vector3 buttonScale, Material markerMaterial)
    {
        GameObject markerRoot = new GameObject(name);
        markerRoot.transform.SetParent(parent);

        float topY = buttonPosition.y + buttonScale.y * 0.5f + 0.018f;
        CreateMarkerPiece(markerRoot.transform, "Stroke", new Vector3(buttonPosition.x, topY, buttonPosition.z + 0.08f), new Vector3(0.14f, 0.035f, 0.52f), markerMaterial);
        CreateMarkerPiece(markerRoot.transform, "Dot", new Vector3(buttonPosition.x, topY, buttonPosition.z - 0.34f), new Vector3(0.18f, 0.035f, 0.18f), markerMaterial);
    }

    private static void CreateMarkerPiece(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject piece = CreateCube(parent, name, position, Quaternion.identity, scale, material);
        Collider collider = piece.GetComponent<Collider>();

        if (collider != null)
        {
            UnityEngine.Object.DestroyImmediate(collider);
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

    private static Vector3 GetAdjacentCenter(Vector3 center, Vector2 size, RoomSide side, Vector2 adjacentSize)
    {
        return side switch
        {
            RoomSide.North => center + Vector3.forward * (size.y * 0.5f + adjacentSize.y * 0.5f),
            RoomSide.South => center + Vector3.back * (size.y * 0.5f + adjacentSize.y * 0.5f),
            RoomSide.East => center + Vector3.right * (size.x * 0.5f + adjacentSize.x * 0.5f),
            _ => center + Vector3.left * (size.x * 0.5f + adjacentSize.x * 0.5f)
        };
    }

    private static RoomSide GetOppositeSide(RoomSide side)
    {
        return side switch
        {
            RoomSide.North => RoomSide.South,
            RoomSide.South => RoomSide.North,
            RoomSide.East => RoomSide.West,
            _ => RoomSide.East
        };
    }

    private static bool IsNorthSouth(RoomSide side)
    {
        return side == RoomSide.North || side == RoomSide.South;
    }

    private static Vector3 GetOutwardDirection(RoomSide side)
    {
        return side switch
        {
            RoomSide.North => Vector3.forward,
            RoomSide.South => Vector3.back,
            RoomSide.East => Vector3.right,
            _ => Vector3.left
        };
    }

    private static Vector3 GetInwardDirection(RoomSide side)
    {
        return -GetOutwardDirection(side);
    }

    private static PrototypeMaterials CreateMaterials()
    {
        return new PrototypeMaterials
        {
            Floor = CreateMaterial("Floor", new Color(0.22f, 0.24f, 0.25f)),
            Wall = CreateMaterial("Wall", new Color(0.58f, 0.6f, 0.62f)),
            Ceiling = CreateMaterial("Ceiling", new Color(0.36f, 0.37f, 0.39f)),
            Door = CreateMaterial("Door", new Color(0.38f, 0.22f, 0.12f)),
            TutorialAccent = CreateMaterial("TutorialAccent", new Color(0.2f, 0.65f, 0.95f), true),
            MathAccent = CreateMaterial("MathAccent", new Color(0.1f, 0.65f, 0.95f), true),
            HistoryAccent = CreateMaterial("HistoryAccent", new Color(0.85f, 0.5f, 0.18f), true),
            ScienceAccent = CreateMaterial("ScienceAccent", new Color(0.25f, 0.8f, 0.45f), true),
            LanguageAccent = CreateMaterial("LanguageAccent", new Color(0.9f, 0.35f, 0.55f), true),
            LogicAccent = CreateMaterial("LogicAccent", new Color(0.72f, 0.55f, 0.95f), true),
            Button = CreateMaterial("Button", new Color(0.16f, 0.18f, 0.2f)),
            CorrectButton = CreateMaterial("CorrectButton", new Color(0.15f, 0.65f, 0.32f), true),
            Marker = CreateMaterial("Marker", new Color(0.015f, 0.018f, 0.02f)),
            QuestionMarker = CreateMaterial("QuestionMarker", new Color(0.92f, 0.95f, 1f)),
            DoorKnob = CreateMaterial("DoorKnob", new Color(0.95f, 0.68f, 0.2f)),
            MessageObject = CreateMaterial("GlowMessageObject", new Color(0.05f, 0.9f, 1f), true),
            Lever = CreateMaterial("Lever", new Color(1f, 0.85f, 0.15f), true)
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
            material.SetColor("_EmissionColor", color * 1.6f);
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

    private static InputReferences CreateInputReferences()
    {
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        if (inputAsset == null)
        {
            Debug.LogWarning("EscapeRoomPrototypeBuilder ne može pronaći Assets/InputSystem_Actions.inputactions. Runtime skripte će koristiti tipkovnicu i miš kao rezervni unos.");
            return new InputReferences();
        }

        InputActionMap playerMap = inputAsset.FindActionMap("Player", false);
        if (playerMap == null)
        {
            Debug.LogWarning("EscapeRoomPrototypeBuilder ne može pronaći Player action map. Runtime skripte će koristiti tipkovnicu i miš kao rezervni unos.");
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
            Debug.LogWarning("EscapeRoomPrototypeBuilder ne može pronaći input akciju: " + actionName);
            return null;
        }

        string path = InputReferencesFolder + "/" + actionName + "ActionReference.asset";
        AssetDatabase.DeleteAsset(path);

        InputActionReference reference = InputActionReference.Create(action);
        reference.name = actionName + " Action Reference";
        AssetDatabase.CreateAsset(reference, path);
        return reference;
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