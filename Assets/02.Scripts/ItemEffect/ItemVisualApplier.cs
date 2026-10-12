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
                gunVisualObject.transform.localScale = Vector3.one * itemData.VisualValue;
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
                GameObject visualObject = child.transform.GetChild(0).gameObject;
                if (visualObject.TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    Color color = spriteRenderer.color;
                    color.r += itemData.VisualValue;
                    spriteRenderer.color = color;
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