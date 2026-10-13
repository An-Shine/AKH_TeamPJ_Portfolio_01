using UnityEngine;

public class TooltipAnimationEventReceiver : MonoBehaviour
{
    public void OnEndAnimationFinished()
    {
        ToolTipManager manager = ToolTipManager.Instance;
        if (manager == null) return;
        manager.OnEndAnimationFinished();
    }
}