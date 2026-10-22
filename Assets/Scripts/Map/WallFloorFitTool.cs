using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WallFloorFitTool : EditorWindow
{
    private string floorPrefix = "Floor";
    private string wallPrefix  = "Wall";
    private bool alignBaseToFloorTop = true;

    [MenuItem("Tools/Wall-Floor Fit Tool")]
    private static void Open() => GetWindow<WallFloorFitTool>("Wall-Floor Fit");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("讓牆壁對齊最近的地板", EditorStyles.boldLabel);
        floorPrefix = EditorGUILayout.TextField("地板名稱前綴", floorPrefix);
        wallPrefix  = EditorGUILayout.TextField("牆壁名稱前綴", wallPrefix);
        alignBaseToFloorTop = EditorGUILayout.Toggle("牆底對齊地板頂面", alignBaseToFloorTop);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("內面貼齊地板邊緣、轉角端點外推一個牆厚補縫、（可選）牆底落在地板上。" +
                                "門片的開口端不會被動到。只處理軸對齊的牆，走廊牆 (Corridor*) 會略過。",
                                MessageType.Info);
        if (GUILayout.Button("套用"))
            Apply();
    }

    private void Apply()
    {
        // 收集地板
        List<Renderer> floors = new List<Renderer>();
        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
            if (mr.gameObject.name.StartsWith(floorPrefix)) floors.Add(mr);

        if (floors.Count == 0) { Debug.LogWarning("[WallFloorFit] 找不到任何地板（前綴 '" + floorPrefix + "'）。"); return; }

        // 收集要處理的牆（軸對齊、非走廊）
        List<Transform> walls = new List<Transform>();
        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
        {
            GameObject go = mr.gameObject;
            if (!go.name.StartsWith(wallPrefix)) continue;
            if (go.name.StartsWith("Corridor")) continue;
            if (Quaternion.Angle(go.transform.rotation, Quaternion.identity) > 1f) continue; // 跳過有旋轉的
            walls.Add(go.transform);
        }
        if (walls.Count == 0) { Debug.LogWarning("[WallFloorFit] 找不到可處理的牆。"); return; }

        int changed = 0;
        foreach (Transform t in walls)
        {
            Renderer wr = t.GetComponent<Renderer>();
            if (wr == null) continue;
            Bounds bd = wr.bounds;

            // 找最近的地板（以 XZ 到地板矩形的距離）
            Renderer floor = null; float best = float.MaxValue;
            foreach (Renderer f in floors)
            {
                float d = DistXZ(bd.center, f.bounds);
                if (d < best) { best = d; floor = f; }
            }
            if (floor == null) continue;
            Bounds fb = floor.bounds;

            float thk    = Mathf.Min(bd.size.x, bd.size.z); // 牆厚（薄軸）
            float height = bd.size.y;
            bool longX   = bd.size.x >= bd.size.z;
            float yc     = alignBaseToFloorTop ? fb.max.y + height * 0.5f : bd.center.y;

            Vector3 targetCenter, targetSize;
            if (longX) // N/S 牆：薄軸 = Z，長軸 = X
            {
                float edgeZ = (bd.center.z > fb.center.z) ? fb.max.z : fb.min.z;
                float newZ  = (bd.center.z > fb.center.z) ? edgeZ + thk * 0.5f : edgeZ - thk * 0.5f;

                float ne0 = SnapEnd(bd.min.x, fb.min.x, fb.max.x, fb.center.x, thk);
                float ne1 = SnapEnd(bd.max.x, fb.min.x, fb.max.x, fb.center.x, thk);

                targetCenter = new Vector3((ne0 + ne1) * 0.5f, yc, newZ);
                targetSize   = new Vector3(ne1 - ne0, height, thk);
            }
            else // E/W 牆：薄軸 = X，長軸 = Z
            {
                float edgeX = (bd.center.x > fb.center.x) ? fb.max.x : fb.min.x;
                float newX  = (bd.center.x > fb.center.x) ? edgeX + thk * 0.5f : edgeX - thk * 0.5f;

                float ne0 = SnapEnd(bd.min.z, fb.min.z, fb.max.z, fb.center.z, thk);
                float ne1 = SnapEnd(bd.max.z, fb.min.z, fb.max.z, fb.center.z, thk);

                targetCenter = new Vector3(newX, yc, (ne0 + ne1) * 0.5f);
                targetSize   = new Vector3(thk, height, ne1 - ne0);
            }

            Undo.RecordObject(t, "Fit Walls to Floor");
            t.position = targetCenter;
            Vector3 ps = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(targetSize.x / ps.x, targetSize.y / ps.y, targetSize.z / ps.z);
            changed++;
        }

        Debug.Log($"[WallFloorFit] 已調整 {changed} 道牆去貼合最近的地板。");
    }

    // 端點處理：靠近某個地板角落 → 外推一個牆厚補縫；靠近中段（門口端）→ 原封不動
    private static float SnapEnd(float p, float lo, float hi, float mid, float thk)
    {
        float dLo = Mathf.Abs(p - lo), dHi = Mathf.Abs(p - hi), dMid = Mathf.Abs(p - mid);
        if (dLo <= dHi && dLo < dMid) return lo - thk; // 低端角落
        if (dHi <  dLo && dHi < dMid) return hi + thk; // 高端角落
        return p;                                      // 門口端 / 內側端：保留
    }

    private static float DistXZ(Vector3 c, Bounds b)
    {
        float dx = Mathf.Max(b.min.x - c.x, 0f, c.x - b.max.x);
        float dz = Mathf.Max(b.min.z - c.z, 0f, c.z - b.max.z);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}