using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DialogueNode
{
    public string speakerName;
    [TextArea(3, 8)]
    public string text; // Multi-line text field for easy editing in Inspector
    [Tooltip("Check this if this node is spoken by Character A (Left Portrait). Uncheck for Character B (Right Portrait).")]
    public bool isCharacterA; 
}

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "PRU/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Character Portraits")]
    [Tooltip("Drag the sprite for Character A (Left Portrait) here.")]
    public Sprite characterASprite;
    [Tooltip("Drag the sprite for Character B (Left Portrait) here.")]
    public Sprite characterBSprite;

    [Header("Portrait Sizes")]
    [Tooltip("Width of the character portrait on screen.")]
    public float portraitWidth = 800f;
    [Tooltip("Height of the character portrait on screen.")]
    public float portraitHeight = 700f;

    [Header("Dialogue Sequence")]
    public List<DialogueNode> dialogueNodes = new List<DialogueNode>();
}
