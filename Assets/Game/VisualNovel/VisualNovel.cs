using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using static VisualNovel;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

// Data structure for dialogue entries
[System.Serializable]
public class DialogueEntry
{
    public string characterName;
    public string dialogueText;
    public string portraitSpriteName; // Name of sprite resource or path
    public Emotions emotion = Emotions.NEUTRAL;
    public string audioCue; // Optional audio cue when slide comes up
}

// Container for dialogue sequences
[System.Serializable]
public class DialogueSequence
{
    public string sequenceId;
    public List<DialogueEntry> dialogues = new List<DialogueEntry>();
}

// Main Visual Novel Controller
public class VisualNovel : MonoBehaviour
{
    public enum Emotions { NEUTRAL, HAPPY, ANGRY, CONFUSED }

    [Header("Dependencies")]
    [SerializeField] private InputActionReference interact;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button nextButton;

    [Header("Resources")]
    [SerializeField] private Sprite[] characterPortraits; // Assign in inspector
    [SerializeField] private string dialogueFile = "example_dialogue";

    // Current dialogue state
    private DialogueSequence currentSequence;
    private int currentIndex = 0;
    private bool isDialogueActive = false;

    // Reference to the canvas
    private Canvas visualNovelCanvas;

    void Start()
    {
        // Trigger the dialogue when game starts if a valid file is inputted, otherwise skip
        if (dialogueFile != null)
        {
            // Get canvas component
            visualNovelCanvas = GetComponent<Canvas>();

            // Setup button listener
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextDialogue);
            }

            // Setup input action callback
            if (interact != null)
            {
                interact.action.performed += OnInteractPressed;
            }

            TriggerDialogue(dialogueFile);

            // Ensure the canvas is active on start
            ShowDialogueUI();
        }
    }

    // Method to trigger dialogue from UI button or other events
    public void TriggerDialogue(string fileName)
    {
        LoadDialogueFromJSON(fileName);
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (isDialogueActive)
        {
            NextDialogue();
        }
    }

    void OnDestroy()
    {
        // Clean up the callback to prevent memory leaks
        if (interact != null)
        {
            interact.action.performed -= OnInteractPressed;
        }
    }

    // Load dialogue from JSON file
    public void LoadDialogueFromJSON(string fileName)
    {
        string folderPath = Application.dataPath + "/Game/VisualNovel/";
        string filePath = folderPath + fileName + ".json";

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string jsonContent = System.IO.File.ReadAllText(filePath);
                currentSequence = JsonUtility.FromJson<DialogueSequence>(jsonContent);
                StartDialogue();
                Debug.Log($"Loaded dialogue from: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading dialogue file {fileName}: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"Could not find dialogue file: {filePath}");
        }
    }

    // Alternative: Load dialogue from ScriptableObject
    public void LoadDialogueSequence(DialogueSequence sequence)
    {
        currentSequence = sequence;
        StartDialogue();
    }

    // Start dialogue sequence
    public void StartDialogue()
    {
        if (currentSequence == null || currentSequence.dialogues.Count == 0)
        {
            Debug.LogWarning("No dialogue sequence loaded!");
            return;
        }

        currentIndex = 0;
        isDialogueActive = true;

        // Show the dialogue UI
        ShowDialogueUI();

        DisplayCurrentDialogue();
    }

    // Display current dialogue entry
    private void DisplayCurrentDialogue()
    {
        if (currentIndex >= currentSequence.dialogues.Count) return;

        DialogueEntry current = currentSequence.dialogues[currentIndex];

        // Update UI elements
        if (nameText != null)
            nameText.text = current.characterName;

        if (dialogueText != null)
            dialogueText.text = current.dialogueText;

        if (portraitImage != null)
            UpdatePortrait(current.portraitSpriteName);

        // Handle audio cue
        if (!string.IsNullOrEmpty(current.audioCue))
        {
            // AudioManager.PlayBGM(current.audioCue);
        }

        // Handle emotion-based effects
        HandleEmotionEffects(current.emotion);
    }

    // Advance to next dialogue
    public void NextDialogue()
    {
        if (!isDialogueActive) return;

        currentIndex++;

        if (currentIndex >= currentSequence.dialogues.Count)
        {
            EndDialogue();
        }
        else
        {
            DisplayCurrentDialogue();
        }
    }

    // End dialogue sequence
    private void EndDialogue()
    {
        isDialogueActive = false;

        // Hide the dialogue UI (but keep canvas active)
        HideDialogueUI();

        // Optional: Trigger events when dialogue ends
        OnDialogueComplete();
    }

    // Show dialogue UI elements
    private void ShowDialogueUI()
    {
        if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(true);
    }

    // Hide dialogue UI elements
    private void HideDialogueUI()
    {
        if (dialoguePanel != null) dialoguePanel.gameObject.SetActive(false);
    }

    // Update character portrait
    private void UpdatePortrait(string spriteName)
    {
        if (portraitImage == null || string.IsNullOrEmpty(spriteName)) return;

        // Find sprite in array by name
        Sprite sprite = System.Array.Find(characterPortraits, s => s.name == spriteName);
    }

    // Handle emotion-based visual or audio effects
    private void HandleEmotionEffects(Emotions emotion)
    {
        switch (emotion)
        {
            case Emotions.HAPPY:
                // Add happy effects (particles, color tint, etc.)
                Debug.Log("Character is happy!");
                break;
            case Emotions.ANGRY:
                // Add angry effects (screen shake, red tint, etc.)
                Debug.Log("Character is angry!");
                break;
            case Emotions.CONFUSED:
                // Add confused effects (question marks, wobble, etc.)
                Debug.Log("Character is confused!");
                break;
            case Emotions.NEUTRAL:
            default:
                // Reset to neutral state
                Debug.Log("Character is neutral.");
                break;
        }
    }

    // Called when dialogue sequence completes
    private void OnDialogueComplete()
    {
        Debug.Log("Dialogue sequence completed!");
        // Add your custom logic here (unlock next scene, trigger events, etc.)
    }

    // Utility method to create example JSON
    [ContextMenu("Generate Example JSON")]
    private void GenerateExampleJSON()
    {
        DialogueSequence example = new DialogueSequence();
        example.sequenceId = "example_dialogue";

        example.dialogues.Add(new DialogueEntry
        {
            characterName = "Fair Flyer",
            dialogueText = "Hello! Welcome to the Mare Restaurant!",
            portraitSpriteName = "Sprite_Fair_Flyer_0",
            emotion = Emotions.HAPPY
        });

        example.dialogues.Add(new DialogueEntry
        {
            characterName = "Soiree",
            dialogueText = "Oh another customer...",
            portraitSpriteName = "bob_excited",
            emotion = Emotions.NEUTRAL
        });

        example.dialogues.Add(new DialogueEntry
        {
            characterName = "Morning Mimosa",
            dialogueText = "You wanna see me jump the shark!?",
            portraitSpriteName = "Sprite_Fair_Flyer_0",
            emotion = Emotions.HAPPY
        });

        string json = JsonUtility.ToJson(example, true);
        Debug.Log("Example JSON:\n" + json);

        // Save to file (optional)
#if UNITY_EDITOR
        string path = Application.dataPath + "/Resources/example_dialogue.json";
        System.IO.File.WriteAllText(path, json);
        Debug.Log("JSON saved to: " + path);
#endif
    }
}

// Optional: ScriptableObject version for easier editing in inspector
[CreateAssetMenu(fileName = "New Dialogue Sequence", menuName = "Visual Novel/Dialogue Sequence")]
public class DialogueSequenceAsset : ScriptableObject
{
    public DialogueSequence dialogueSequence = new DialogueSequence();
}