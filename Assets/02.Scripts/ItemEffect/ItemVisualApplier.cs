using UnityEngine;

public static class ItemVisualApplier
{
    public static void Visualize(ItemData itemData, Transform playerTransform)
    {
        switch (itemData.ItemId)
        {
            case 0:
            {
                break;
            }
            case 1:
            {
                Transform child = playerTransform.GetChild(0);
                GameObject gunVisualObject = child.transform.GetChild(1).gameObject;
                gunVisualObject.transform.localScale *= itemData.VisualValue;
                break;
            }
            case 4:
            {
                playerTransform.localScale *= itemData.VisualValue;
                break;
            }
            case 5:
            {
                Transform child = playerTransform.GetChild(0);
                GameObject bodyVisual = child.transform.GetChild(0).gameObject;
                if (bodyVisual.TryGetComponent(out SpriteRenderer bodyRenderer))
                {
                    float otherColor = itemData.VisualValue;
                    if (otherColor >= 1f)
                    {
                        otherColor = 1f;
                    }

                    Color color = new Color(1f, otherColor, otherColor, 1f);
                    bodyRenderer.color = color;
                }

                GameObject armVisual = child.transform.GetChild(2).gameObject;
                if (armVisual.TryGetComponent(out SpriteRenderer armRenderer))
                {
                    float otherColor = itemData.VisualValue;
                    if (otherColor >= 1f)
                    {
                        otherColor = 1f;
                    }

                    Color color = new Color(1f, otherColor, otherColor, 1f);
                    armRenderer.color = color;
                }

                break;
            }
            case 15:
            {
                Transform child = playerTransform.GetChild(0);
                GameObject gunVisualObject = child.transform.GetChild(1).gameObject;
                if (gunVisualObject.TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    spriteRenderer.material.SetFloat("_TextureLayer1Fade", 1f);
                }

                break;
            }
        }
    }
}