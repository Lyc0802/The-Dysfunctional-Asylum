using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class DoorCutter
{
    private const float DoorWidth             = 2f;    // 門寬 / 走廊淨寬
    private const float CorridorWallThickness = 0.3f;  // 走廊牆厚
    private const float MinPiece              = 0.001f;
    private const float MinGap                = 0.01f;
    private const string WallPrefix           = "Wall";
    private const string CorridorName         = "Corridor";

    private class Crossing
    {
        public float s;         // 沿整條折線的弧長位置
        public Vector2 pt;      // 穿越點 (x,z)
        public float bottomY, height, roomThk;
        public int layer;
        public Material mat;
    }

    [MenuItem("Tools/Cut Door Between Two Objects %#d")]
    private static void CutDoors()
    {
        GameObject[] sel = Selection.gameObjects;
        if (sel.Length != 2 && sel.Length != 3)
        {
            Debug.LogWarning("[DoorCutter] 請選取 2 或 3 個物件。");
            return;
        }

        Undo.SetCurrentGroupName("Cut Door");
        int group = Undo.GetCurrentGroup();
        HashSet<GameObject> exclude = new HashSet<GameObject>(sel);

        // 折線 pts：2 物件=直線；3 物件=最近一對 + 轉彎接第三者
        List<Vector2> pts = new List<Vector2>();
        if (sel.Length == 2)
        {
            pts.Add(XZ(sel[0])); pts.Add(XZ(sel[1]));
        }
        else
        {
            Vector2 p0 = XZ(sel[0]), p1 = XZ(sel[1]), p2 = XZ(sel[2]);
            float d01 = (p0 - p1).sqrMagnitude, d12 = (p1 - p2).sqrMagnitude, d02 = (p0 - p2).sqrMagnitude;
            int iP, iQ, iR;
            if (d01 <= d12 && d01 <= d02)      { iP = 0; iQ = 1; iR = 2; }
            else if (d12 <= d01 && d12 <= d02) { iP = 1; iQ = 2; iR = 0; }
            else                               { iP = 0; iQ = 2; iR = 1; }
            Vector2 P = XZ(sel[iP]), Q = XZ(sel[iQ]), R = XZ(sel[iR]);
            bool hubIsP = (P - R).sqrMagnitude <= (Q - R).sqrMagnitude;
            Vector2 hub = hubIsP ? P : Q, far = hubIsP ? Q : P;
            pts.Add(far); pts.Add(hub); pts.Add(R); // 轉彎點 hub 在中間
        }

        // pts 的累積弧長
        float[] cumLen = new float[pts.Count];
        for (int i = 1; i < pts.Count; i++)
            cumLen[i] = cumLen[i - 1] + (pts[i] - pts[i - 1]).magnitude;

        // Phase 1：逐段收集穿越資訊（換算成全域弧長）並開門
        List<Crossing> crossings = new List<Crossing>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1], d = b - a;
            float segLen = d.magnitude;
            if (segLen < 1e-5f) continue;

            // 每段重抓牆，才能吃到前一段切出的新牆片
            List<GameObject> walls = new List<GameObject>();
            foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
            {
                GameObject go = mr.gameObject;
                if (exclude.Contains(go)) continue;
                if (!go.name.StartsWith(WallPrefix)) continue;
                walls.Add(go);
            }

            foreach (GameObject wall in walls)
            {
                Renderer r = wall.GetComponent<Renderer>();
                if (r == null) continue;
                Bounds bd = r.bounds;
                if (!ClipSegmentToRect(a, d, bd.min.x, bd.max.x, bd.min.z, bd.max.z,
                                       out float t0, out float t1)) continue;

                float tMid = (t0 + t1) * 0.5f;
                crossings.Add(new Crossing
                {
                    s = cumLen[i] + tMid * segLen,
                    pt = a + d * tMid,
                    bottomY = bd.min.y, height = bd.size.y,
                    roomThk = Mathf.Min(bd.size.x, bd.size.z),
                    layer = wall.layer, mat = r.sharedMaterial
                });
                CutDoorInWall(wall, a, b);
            }
        }

        if (crossings.Count == 0)
        {
            Debug.LogWarning("[DoorCutter] 連線沒有通過任何牆壁，未開門。");
            Undo.CollapseUndoOperations(group);
            return;
        }

        // Phase 2：以房間外空隙（排序後 (0,1)(2,3)… 對）蓋走廊。
        // 折線在房間內的區段不蓋牆（交給房間牆+門）；轉彎點落在空地時，兩腿會在空地斜接成 L。
        crossings.Sort((x, y) => x.s.CompareTo(y.s));
        GameObject root = null;
        int segCounter = 0;
        for (int k = 0; k + 1 < crossings.Count; k += 2)
            if (BuildCorridorRun(crossings[k], crossings[k + 1], pts, cumLen, ref root, segCounter))
                segCounter++;

        Undo.CollapseUndoOperations(group);
        Debug.Log($"[DoorCutter] 開門 {crossings.Count} 個，走廊 {segCounter} 段。");
    }

    private static Vector2 XZ(GameObject g) =>
        new Vector2(g.transform.position.x, g.transform.position.z);

    // 建一段走廊：弧長 [cA.s, cB.s] 之間的子折線（含中間原始轉彎點），兩側斜接牆
    private static bool BuildCorridorRun(Crossing cA, Crossing cB, List<Vector2> pts, float[] cumLen,
                                         ref GameObject root, int index)
    {
        List<Vector2> V = new List<Vector2> { cA.pt };
        for (int j = 1; j < pts.Count - 1; j++)
            if (cumLen[j] > cA.s + 1e-4f && cumLen[j] < cB.s - 1e-4f)
                V.Add(pts[j]);        // 轉彎點（在這段空隙內才納入）
        V.Add(cB.pt);
        if (V.Count < 2) return false;

        // 兩端各內縮半個房間牆厚 → 貼齊房間牆外側面，不伸進房間
        Vector2 ds = (V[1] - V[0]); float ls = ds.magnitude;
        if (ls > 1e-4f) V[0] += (ds / ls) * Mathf.Min(cA.roomThk * 0.5f, ls * 0.45f);
        int last = V.Count - 1;
        Vector2 de = (V[last] - V[last - 1]); float le = de.magnitude;
        if (le > 1e-4f) V[last] -= (de / le) * Mathf.Min(cB.roomThk * 0.5f, le * 0.45f);

        float total = 0f;
        for (int j = 0; j < V.Count - 1; j++) total += (V[j + 1] - V[j]).magnitude;
        if (total <= MinGap) return false;

        if (root == null)
        {
            root = new GameObject(CorridorName);
            Undo.RegisterCreatedObjectUndo(root, "Cut Door");
        }

        float off = DoorWidth * 0.5f + CorridorWallThickness * 0.5f;
        string sfx = index > 0 ? "_" + index : "";
        BuildSide(V, +1f, off, cA, root, $"Corridor{sfx}_L");
        BuildSide(V, -1f, off, cA, root, $"Corridor{sfx}_R");
        return true;
    }

    private static void BuildSide(List<Vector2> V, float sgn, float off, Crossing geo,
                                  GameObject root, string baseName)
    {
        int segs = V.Count - 1;
        Vector2[] dir = new Vector2[segs];
        Vector2[] nrm = new Vector2[segs]; // 垂直方向（= box 的 local +X）
        for (int j = 0; j < segs; j++)
        {
            Vector2 dd = (V[j + 1] - V[j]).normalized;
            dir[j] = dd;
            nrm[j] = new Vector2(dd.y, -dd.x); // Cross(up,dir) 在 XZ 的投影
        }

        // 偏移折線頂點（內外側皆斜接）
        Vector2[] W = new Vector2[V.Count];
        W[0] = V[0] + sgn * off * nrm[0];
        W[segs] = V[segs] + sgn * off * nrm[segs - 1];
        for (int j = 1; j < segs; j++)
            W[j] = IntersectLines(V[j] + sgn * off * nrm[j - 1], dir[j - 1],
                                  V[j] + sgn * off * nrm[j], dir[j]);

        for (int j = 0; j < segs; j++)
            MakeWallBox(root, $"{baseName}{j}", W[j], W[j + 1], geo);
    }

    private static void MakeWallBox(GameObject root, string name, Vector2 p0, Vector2 p1, Crossing geo)
    {
        Vector3 a3 = new Vector3(p0.x, 0, p0.y), b3 = new Vector3(p1.x, 0, p1.y);
        Vector3 flat = b3 - a3; float len = flat.magnitude;
        if (len < MinPiece) return;
        Vector3 dir = flat / len;
        Vector3 mid = (a3 + b3) * 0.5f; mid.y = geo.bottomY + geo.height * 0.5f;
        MakeBox(root, name, mid, Quaternion.LookRotation(dir, Vector3.up),
                new Vector3(CorridorWallThickness, geo.height, len), geo.mat, geo.layer);
    }

    private static Vector2 IntersectLines(Vector2 p1, Vector2 d1, Vector2 p2, Vector2 d2)
    {
        float denom = d1.x * d2.y - d1.y * d2.x;
        if (Mathf.Abs(denom) < 1e-6f) return (p1 + p2) * 0.5f; // 近乎平行（直線）
        float u = ((p2.x - p1.x) * d2.y - (p2.y - p1.y) * d2.x) / denom;
        u = Mathf.Clamp(u, -1e4f, 1e4f); // 防止夾角過小時轉角噴飛
        return p1 + d1 * u;
    }

    private static void MakeBox(GameObject parent, string name, Vector3 pos,
                                Quaternion rot, Vector3 size, Material mat, int layer)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent.transform, true); // 父為單位尺度物件
        g.transform.position = pos;
        g.transform.rotation = rot;
        g.transform.localScale = size;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        g.layer = layer;
        Undo.RegisterCreatedObjectUndo(g, "Cut Door");
    }

    // ---------- 開門 ----------
    private static bool CutDoorInWall(GameObject wall, Vector2 a, Vector2 b)
    {
        Renderer r = wall.GetComponent<Renderer>();
        if (r == null) return false;
        Bounds bd = r.bounds;
        float minX = bd.min.x, maxX = bd.max.x, minZ = bd.min.z, maxZ = bd.max.z;

        Vector2 d = b - a;
        if (!ClipSegmentToRect(a, d, minX, maxX, minZ, maxZ, out float t0, out float t1)) return false;
        Vector2 hit = a + d * ((t0 + t1) * 0.5f);

        bool longX = bd.size.x >= bd.size.z;
        float loMin = longX ? minX : minZ, loMax = longX ? maxX : maxZ;
        float center = longX ? hit.x : hit.y;
        float wallLen = loMax - loMin, half = DoorWidth * 0.5f;

        Material mat = r.sharedMaterial; int layer = wall.layer; Transform parent = wall.transform.parent;
        if (DoorWidth >= wallLen) { Undo.DestroyObjectImmediate(wall); return true; }
        center = Mathf.Clamp(center, loMin + half, loMax - half);

        float thinCenter = longX ? bd.center.z : bd.center.x;
        float thinSize = longX ? bd.size.z : bd.size.x;
        float yCenter = bd.center.y, ySize = bd.size.y;

        MakePiece(wall.name + "_A", loMin, center - half, longX, thinCenter, thinSize, yCenter, ySize, parent, mat, layer);
        MakePiece(wall.name + "_B", center + half, loMax, longX, thinCenter, thinSize, yCenter, ySize, parent, mat, layer);
        Undo.DestroyObjectImmediate(wall);
        return true;
    }

    private static void MakePiece(string name, float loStart, float loEnd, bool longX,
                                  float thinCenter, float thinSize, float yCenter, float ySize,
                                  Transform parent, Material mat, int layer)
    {
        float len = loEnd - loStart;
        if (len <= MinPiece) return;
        float loCenter = (loStart + loEnd) * 0.5f;

        Vector3 worldCenter, worldSize;
        if (longX) { worldCenter = new Vector3(loCenter, yCenter, thinCenter); worldSize = new Vector3(len, ySize, thinSize); }
        else       { worldCenter = new Vector3(thinCenter, yCenter, loCenter); worldSize = new Vector3(thinSize, ySize, len); }

        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        if (parent != null) w.transform.SetParent(parent, true);
        w.transform.position = worldCenter;
        w.transform.rotation = Quaternion.identity;
        Vector3 ls = worldSize;
        if (w.transform.parent != null)
        {
            Vector3 s = w.transform.parent.lossyScale;
            ls = new Vector3(worldSize.x / s.x, worldSize.y / s.y, worldSize.z / s.z);
        }
        w.transform.localScale = ls;
        if (mat != null) w.GetComponent<Renderer>().sharedMaterial = mat;
        w.layer = layer;
        Undo.RegisterCreatedObjectUndo(w, "Cut Door");
    }

    private static bool ClipSegmentToRect(Vector2 p, Vector2 d, float minX, float maxX,
                                          float minY, float maxY, out float t0, out float t1)
    {
        t0 = 0f; t1 = 1f;
        float[] pArr = { -d.x, d.x, -d.y, d.y };
        float[] qArr = { p.x - minX, maxX - p.x, p.y - minY, maxY - p.y };
        for (int i = 0; i < 4; i++)
        {
            float pi = pArr[i], qi = qArr[i];
            if (Mathf.Abs(pi) < 1e-8f) { if (qi < 0f) return false; }
            else
            {
                float t = qi / pi;
                if (pi < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
                else         { if (t < t0) return false; if (t < t1) t1 = t; }
            }
        }
        return true;
    }

    [MenuItem("Tools/Cut Door Between Two Objects %#d", true)]
    private static bool ValidateCutDoors()
    {
        int n = Selection.gameObjects.Length;
        return n == 2 || n == 3;
    }
}