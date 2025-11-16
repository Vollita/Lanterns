using System;
using UnityEngine;

[Serializable]
public class RiddleData
{
    public string question;
    public string[] options = new string[4];
    public int correctOptionIndex; // 0-3 ∂‘”¶ A-B-C-D
    public string explanation;
}

[CreateAssetMenu(fileName = "NewRiddle", menuName = "XR Game/Riddle")]
public class RiddleSO : ScriptableObject
{
    public RiddleData riddleData;
}