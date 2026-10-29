#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WallThicknessTool : EditorWindow
{
    private float targetThickness = 0.3f;
    private bool  changeThickness = true;
    private bool  fitToFloor      = true;
    private bool  alignBaseToFloorTop = true;
    private string floorPrefix = "Floor";

    private const float MinSize = 0.001f; // 任何軸的最小尺寸，杜絕 scale=0

    // 只認這四個初始名稱（未被 DoorCutter 切過的原始牆）
    private static readonly HashSet<string> OriginalWallNames =
        new HashSet<string> { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };
    [MenuItem("Tools/Wall Thickness Tool")]
    private static void Open() => GetWindow<WallThicknessTool>("Wall Thickness");


    private void OnGUI()
    {
        EditorGUILayout.LabelField("修改原始牆厚度並貼合地板", EditorStyles.boldLabel);
        changeThickness = EditorGUILayout.Toggle("修改厚度", changeThickness);
        using (new EditorGUI.DisabledScope(!changeThickness))
            targetThickness = EditorGUILayout.FloatField("目標厚度", targetThickness);

        EditorGUILayout.Space();
        fitToFloor = EditorGUILayout.Toggle("改後貼合最近地板", fitToFloor);
        using (new EditorGUI.DisabledScope(!fitToFloor))
        {
            floorPrefix = EditorGUILayout.TextField("地板名稱前綴", floorPrefix);
            alignBaseToFloorTop = EditorGUILayout.Toggle("牆底對齊地板頂面", alignBaseToFloorTop);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("只處理名稱為 Wall_North/South/East/West 的原始牆；" +
                                "被開門切出的 _A/_B 牆片與走廊牆 (Corridor*) 一律略過。",
                                MessageType.Info);
        if (GUILayout.Button("套用"))
            Apply();
    }

    private void Apply()
    {
        // 收集原始牆
        List<Transform> walls = new List<Transform>();
        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
            if (OriginalWallNames.Contains(mr.gameObject.name))
                walls.Add(mr.transform);

        if (walls.Count == 0) { Debug.LogWarning("[WallThickness] 找不到原始牆 (Wall_North/South/East/West)。"); return; }

        // 收集地板（供貼合用）
        List<Renderer> floors = new List<Renderer>();
        if (fitToFloor)
            foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
                if (mr.gameObject.name.StartsWith(floorPrefix)) floors.Add(mr);
        if (fitToFloor && floors.Count == 0)
            Debug.LogWarning("[WallThickness] 找不到地板（前綴 '" + floorPrefix + "'），略過貼合步驟。");

        Undo.RecordObjects(walls.ConvertAll(t => (Object)t).ToArray(), "Wall Thickness / Fit");

        float thk = changeThickness ? Mathf.Max(MinSize, targetThickness) : 0f;
        bool doFit = fitToFloor && floors.Count > 0;
        int changed = 0;

        foreach (Transform t in walls)
        {
            Renderer wr = t.GetComponent<Renderer>();
            if (wr == null) continue;

            Bounds bd = wr.bounds;
            bool longX = bd.size.x >= bd.size.z;          // 長軸是 X？（薄軸即厚度軸）
            float curThk = Mathf.Min(bd.size.x, bd.size.z);
            float useThk = changeThickness ? thk : Mathf.Max(MinSize, curThk);
            float height = Mathf.Max(MinSize, bd.size.y);

            // 預設維持原世界範圍，只改厚度軸
            float cx = bd.center.x, cz = bd.center.z, cy = bd.center.y;
            float sx = bd.size.x,  sz = bd.size.z;

            if (longX) sz = useThk; else sx = useThk; // 先把厚度套上

            if (doFit)
            {
                Bounds fb = NearestFloor(floors, bd.center).bounds;

                if (alignBaseToFloorTop) { height = Mathf.Max(MinSize, bd.size.y); cy = fb.max.y + height * 0.5f; }

                if (longX) // N/S 牆：厚度在 Z，長度在 X
                {
                    float edgeZ = (bd.center.z > fb.center.z) ? fb.max.z : fb.min.z;
                    cz = (bd.center.z > fb.center.z) ? edgeZ + useThk * 0.5f : edgeZ - useThk * 0.5f;

                    float e0 = SnapEnd(bd.min.x, fb.min.x, fb.max.x, fb.center.x, useThk);
                    float e1 = SnapEnd(bd.max.x, fb.min.x, fb.max.x, fb.center.x, useThk);
                    cx = (e0 + e1) * 0.5f;
                    sx = Mathf.Max(MinSize, e1 - e0);
                }
                else       // E/W 牆：厚度在 X，長度在 Z
                {
                    float edgeX = (bd.center.x > fb.center.x) ? fb.max.x : fb.min.x;
                    cx = (bd.center.x > fb.center.x) ? edgeX + useThk * 0.5f : edgeX - useThk * 0.5f;

                    float e0 = SnapEnd(bd.min.z, fb.min.z, fb.max.z, fb.center.z, useThk);
                    float e1 = SnapEnd(bd.max.z, fb.min.z, fb.max.z, fb.center.z, useThk);
                    cz = (e0 + e1) * 0.5f;
                    sz = Mathf.Max(MinSize, e1 - e0);
                }
            }

            // 寫回（換算父物件縮放），每軸都夾最小值
            Vector3 worldSize = new Vector3(Mathf.Max(MinSize, sx),
                                            Mathf.Max(MinSize, height),
                                            Mathf.Max(MinSize, sz));
            Vector3 ps = t.parent != null ? t.parent.lossyScale : Vector3.one;
            ps = new Vector3(SafeDiv(ps.x), SafeDiv(ps.y), SafeDiv(ps.z));

            t.position   = new Vector3(cx, cy, cz);
            t.localScale = new Vector3(Mathf.Max(MinSize, worldSize.x / ps.x),
                                       Mathf.Max(MinSize, worldSize.y / ps.y),
                                       Mathf.Max(MinSize, worldSize.z / ps.z));
            changed++;
        }

        Debug.Log($"[WallThickness] 處理 {changed} 道原始牆" +
                  (changeThickness ? $"，厚度={thk}" : "") + (doFit ? "，已貼合地板。" : "。"));
    }

    private static Renderer NearestFloor(List<Renderer> floors, Vector3 c)
    {
        Renderer best = null; float bestD = float.MaxValue;
        foreach (Renderer f in floors)
        {
            float d = DistXZ(c, f.bounds);
            if (d < bestD) { bestD = d; best = f; }
        }
        return best;
    }

    // 端點：靠角落→外推一個牆厚補縫；靠中段（門口端/內側）→ 不動
    private static float SnapEnd(float p, float lo, float hi, float mid, float thk)
    {
        float dLo = Mathf.Abs(p - lo), dHi = Mathf.Abs(p - hi), dMid = Mathf.Abs(p - mid);
        if (dLo <= dHi && dLo < dMid) return lo - thk;
        if (dHi <  dLo && dHi < dMid) return hi + thk;
        return p;
    }

    private static float DistXZ(Vector3 c, Bounds b)
    {
        float dx = Mathf.Max(b.min.x - c.x, 0f, c.x - b.max.x);
        float dz = Mathf.Max(b.min.z - c.z, 0f, c.z - b.max.z);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static float SafeDiv(float v) => Mathf.Abs(v) < 1e-6f ? 1f : v;
}
#endif
