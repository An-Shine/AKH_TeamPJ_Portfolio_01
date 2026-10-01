using UnityEngine;
using System;

[Serializable]
public class DialogueLine
{
    [SerializeField] private string _speakerName;
    [SerializeField] private Sprite _portrait;
    [SerializeField][TextArea(3, 8)] private string _text;

    public string SpeakerName => _speakerName;
    public Sprite Portrait => _portrait;
    public string Text => _text;
}