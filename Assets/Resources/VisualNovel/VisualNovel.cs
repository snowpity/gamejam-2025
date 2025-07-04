using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using static VisualNovel;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Data structure for dialogue entries
[System.Serializable]
public class DialogueEntry
{
    public string characterName;
    public string dialogueText;
    public string portraitSpriteRight; // Name of sprite resource or path
    public string portraitSpriteLeft;
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
    [SerializeField] private Image portraitImageRight;
    [SerializeField] private Image portraitImageLeft;
    [SerializeField] private Text nameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Button nextButton;

    [Header("Resources")]
    [SerializeField] private Sprite[] characterPortraits; // Assign in inspector
    [SerializeField] private string dialogueFile = "example_dialogue";
    [SerializeField] private AudioClip VNMusic; // Plays while in VN mode
    [SerializeField] private AudioClip BGMusic; // Plays while in game

    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 0.02f; // Time between characters
    [SerializeField] private bool skipTypewriterOnClick = true;
    [SerializeField] private AudioClip typewriterSound; // Optional typing sound
    private AudioSource audioSource;

    // Typewriter effect
    private Coroutine typewriterCoroutine;
    private bool isTyping = false;
    private string currentDialogueFullText = "";

    // Current dialogue state
    private DialogueSequence currentSequence;
    private int currentIndex = 0;
    private bool isDialogueActive = false;

    // Reference to the canvas
    private Canvas visualNovelCanvas;

    [SerializeField] AudioController AudioController;

    void Start()
    {
        // Automatically select dialogue file based on scene
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Level 2")
            dialogueFile = "level2_dialogue";
        else if (sceneName == "Level 3")
            dialogueFile = "level3_dialogue";
        else
            dialogueFile = "example_dialogue";

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
        AudioController.PlayBackgroundMusic(VNMusic);
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

    // Accessibility, instantly complete the typewriter effect
    public void CompleteTypewriter()
    {
        if (isTyping && typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            dialogueText.text = currentDialogueFullText;
            isTyping = false;
            typewriterCoroutine = null;
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
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>($"VisualNovel/{fileName}");
            if (jsonFile != null)
            {
                currentSequence = JsonUtility.FromJson<DialogueSequence>(jsonFile.text);
                StartDialogue();
                Debug.Log($"Loaded dialogue: {fileName}");
            }
            else
            {
                Debug.LogError($"Could not find dialogue file: {fileName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading dialogue file {fileName}: {e.Message}");
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

        // Store the full text and start typewriter effect
        currentDialogueFullText = current.dialogueText;
        if (dialogueText != null)
        {
            // Stop any existing typewriter effect
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            // Start new typewriter effect
            typewriterCoroutine = StartCoroutine(TypewriterEffect(currentDialogueFullText));
        }

        // Handle portraits separately
        if (portraitImageRight != null)
            UpdatePortrait(portraitImageRight, current.portraitSpriteRight);

        if (portraitImageLeft != null)
            UpdatePortrait(portraitImageLeft, current.portraitSpriteLeft);

        // Handle audio cue
        if (!string.IsNullOrEmpty(current.audioCue))
        {
            // AudioManager.PlayBGM(current.audioCue);
        }

        // Handle emotion-based effects
        HandleEmotionEffects(current.emotion);
    }

    private System.Collections.IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        dialogueText.text = "";

        // Get or create AudioSource for typing sounds
        if (audioSource == null && typewriterSound != null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        for (int i = 0; i <= fullText.Length; i++)
        {
            dialogueText.text = fullText.Substring(0, i);

            // Play typing sound (but not on spaces or at very fast speeds)
            if (typewriterSound != null && audioSource != null && i < fullText.Length)
            {
                char currentChar = fullText[i];
                if (!char.IsWhiteSpace(currentChar) && typewriterSpeed > 0.01f)
                {
                    audioSource.PlayOneShot(typewriterSound, 0.5f);
                }
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
        typewriterCoroutine = null;
    }

    // Advance to next dialogue
    public void NextDialogue()
    {
        if (!isDialogueActive) return;

        // If currently typing and skip is enabled, complete the text immediately
        if (isTyping && skipTypewriterOnClick)
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            dialogueText.text = currentDialogueFullText;
            isTyping = false;
            return; // Don't advance to next dialogue, just complete current one
        }

        // Normal dialogue advancement
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
        // Stop any active typewriter effect
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isDialogueActive = false;
        isTyping = false;

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

    private void HidePortraits(int portraitID = 0)
    {
        switch(portraitID)
        {
            case 0: // Hide all by default
                if (portraitImageRight != null)
                    portraitImageRight.gameObject.SetActive(false);

                if (portraitImageLeft != null)
                    portraitImageLeft.gameObject.SetActive(false);
                break;
            case 1: // Hide right for ID 1
                if (portraitImageRight != null)
                    portraitImageRight.gameObject.SetActive(false);
                break;
            case 2: // Hide left for ID 2
                if (portraitImageRight != null)
                    portraitImageRight.gameObject.SetActive(false);
                break;
            default:
                break;
        }
    }

    // Update character portrait
    private void UpdatePortrait(Image portraitImage, string spriteName)
    {
        if (portraitImage == null) return;

        if (string.IsNullOrEmpty(spriteName))
        {
            // Hide portrait if no sprite name provided
            portraitImage.gameObject.SetActive(false);
            return;
        }

        // Find sprite in array by name
        Sprite foundSprite = System.Array.Find(characterPortraits, s => s.name == spriteName);

        if (foundSprite != null)
        {
            portraitImage.sprite = foundSprite;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Portrait sprite '{spriteName}' not found in characterPortraits array!");
            portraitImage.gameObject.SetActive(false);
        }
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
        GameStateManager.SetGameStarted(true);

        AudioController.PlayBackgroundMusic(BGMusic);
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
            portraitSpriteRight = "Sprite_Fair_Flyer_0",
            portraitSpriteLeft = "",
            emotion = Emotions.HAPPY
        });

        example.dialogues.Add(new DialogueEntry
        {
            characterName = "Soiree",
            dialogueText = "Oh another customer...",
            portraitSpriteRight = "Sprite_Anon_Filly_0",
            portraitSpriteLeft = "Sprite_Fair_Flyer_0",
            emotion = Emotions.NEUTRAL
        });

        example.dialogues.Add(new DialogueEntry
        {
            characterName = "Morning Mimosa",
            dialogueText = "You wanna see me jump the shark!?",
            portraitSpriteRight = "Sprite_Fair_Flyer_0",
            portraitSpriteLeft = "",
            emotion = Emotions.HAPPY
        });

        string json = JsonUtility.ToJson(example, true);
        Debug.Log("Example JSON:\n" + json);

        // Save to Resources folder (works in builds)
#if UNITY_EDITOR
        string resourcesPath = Application.dataPath + "/Resources/VisualNovel/";

        // Create directory if it doesn't exist
        if (!System.IO.Directory.Exists(resourcesPath))
        {
            System.IO.Directory.CreateDirectory(resourcesPath);
            Debug.Log("Created Resources/VisualNovel/ directory");
        }

        string path = resourcesPath + "example_dialogue.json";
        System.IO.File.WriteAllText(path, json);
        Debug.Log("JSON saved to: " + path);

        // Refresh the asset database so Unity recognizes the new file
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}

// Optional: ScriptableObject version for easier editing in inspector
[CreateAssetMenu(fileName = "New Dialogue Sequence", menuName = "Visual Novel/Dialogue Sequence")]
public class DialogueSequenceAsset : ScriptableObject
{
    public DialogueSequence dialogueSequence = new DialogueSequence();
}