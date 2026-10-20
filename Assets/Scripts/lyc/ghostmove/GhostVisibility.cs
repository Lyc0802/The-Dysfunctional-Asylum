using UnityEngine;
using UnityEngine.Rendering;



public class GhostVisibility : MonoBehaviour
{
    public string ghostLayerName = "Ghost";

    void Awake()
    {
        int layer = LayerMask.NameToLayer(ghostLayerName);
        if (layer < 0)
        {
            Debug.LogError($"找不到圖層「{ghostLayerName}」，請到 Edit → Project Settings → Tags and Layers 新增。");
            return;
        }

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            // 攝影機不改圖層，避免影響本地玩家的視角
            if (t.GetComponent<Camera>() != null) continue;
            t.gameObject.layer = layer;
        }

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = ShadowCastingMode.Off;
    }
}