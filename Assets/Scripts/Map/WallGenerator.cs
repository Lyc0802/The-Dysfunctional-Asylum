#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

public static class WallGenerator
{
    private const float WallThickness = 1f;   // 牆壁厚度（1 格）
    private const float WallHeight    = 10f;  // 牆壁高度

    // 選單：Tools/Generate Walls Around Cube （快捷鍵 Ctrl/Cmd + Shift + W，可刪）

    [MenuItem("Tools/Generate Walls Around Cube %#w")]
    private static void GenerateWalls()
    {
        GameObject cube = Selection.activeGameObject;
        if (cube == null)
        {
            Debug.LogWarning("[WallGenerator] 請先在場景中選取一個物件。");
            return;
        }

        Renderer rend = cube.GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("[WallGenerator] 選取的物件沒有 Renderer，無法偵測大小。");
            return;
        }

        // 用世界座標 bounds 取得實際的 X / Z 尺寸（已含 localScale）
        Bounds bounds = rend.bounds;
        float sizeX = bounds.size.x;
        float sizeZ = bounds.size.z;
        Vector3 center = bounds.center;

        float wallCenterY = bounds.min.y + WallHeight * 0.5f; // 牆底對齊 cube 底部
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float halfT = WallThickness * 0.5f;

        // 每次都建立一個新的 Walls 父物件
        GameObject wallsRoot = new GameObject("Walls");
        Undo.RegisterCreatedObjectUndo(wallsRoot, "Generate Walls");

        // 南北牆（沿 X 方向）：長度多加 2 格厚度，讓四個角落補滿不留縫
        CreateWall(wallsRoot, "Wall_North",
            new Vector3(center.x, wallCenterY, center.z + halfZ + halfT),
            new Vector3(sizeX + WallThickness * 2f, WallHeight, WallThickness));

        CreateWall(wallsRoot, "Wall_South",
            new Vector3(center.x, wallCenterY, center.z - halfZ - halfT),
            new Vector3(sizeX + WallThickness * 2f, WallHeight, WallThickness));

        // 東西牆（沿 Z 方向）
        CreateWall(wallsRoot, "Wall_East",
            new Vector3(center.x + halfX + halfT, wallCenterY, center.z),
            new Vector3(WallThickness, WallHeight, sizeZ));

        CreateWall(wallsRoot, "Wall_West",
            new Vector3(center.x - halfX - halfT, wallCenterY, center.z),
            new Vector3(WallThickness, WallHeight, sizeZ));

        Selection.activeGameObject = wallsRoot;
        Debug.Log($"[WallGenerator] 依 '{cube.name}' (X={sizeX:F2}, Z={sizeZ:F2}) 生成 4 道牆壁完成。");
    }



    private static void CreateWall(GameObject parent, string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent.transform, true);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        Undo.RegisterCreatedObjectUndo(wall, "Generate Walls");
    }

    // 沒選到帶 Renderer 的物件時，選單項目會變灰
    [MenuItem("Tools/Generate Walls Around Cube %#w", true)]
    private static bool ValidateGenerateWalls()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<Renderer>() != null;
    }

}
#endif
