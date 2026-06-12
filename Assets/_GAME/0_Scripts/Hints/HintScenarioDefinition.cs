using UnityEngine;

[CreateAssetMenu(menuName = "Hints/Hint Scenario")]
public class HintScenarioDefinition : ScriptableObject
{
    public float ScreenHintDelay = 10f;
    
    public float WorldHintDelay = 15f;

    public float HighlightDelay = 20f;

    public float ArrowDelay = 30f;
}