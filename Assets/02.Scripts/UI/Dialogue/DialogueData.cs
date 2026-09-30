using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string _dialogueId;
    [SerializeField] private DialogueLine[] _lines = Array.Empty<DialogueLine>();

    public string DialogueId => _dialogueId;
    public DialogueLine[] Lines => _lines;
    public int LineCount => _lines.Length;
}