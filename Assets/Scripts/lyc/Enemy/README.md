# 敵人 AI 第一版

## 獨立測試場景

開啟 `Assets/Scenes/EnemyPatrolTest.unity`，按 Play。
此場景已保存簡化房間、走廊、牆壁、15 個點位、5 條通風管路徑與烘焙好的 NavMesh；開啟即可查看，按 Play 開始運作。建構元件仍保留從空場景建立配置的能力。這是依約定連線建立的功能測試地圖，不是正式關卡的尺寸重建。

- WASD／方向鍵：移動玩家替身（須讓 Game 視窗取得焦點）。
- R：將玩家放回起點並恢復可被追擊狀態。
- 敵人從房間 1 出發，玩家位於房間 3。
- 選取 `Enemy (select to inspect action)`，Inspector 可查看 Action／Current Room。Scene 視窗選點位可看連線 Gizmos。
- 捕獲後玩家 `alive` 變為 false，敵人回到出生點。死亡畫面／攻擊動畫未製作。
- 抵達點位 10 觸發 `onDeparted`，預設停用敵人物件，表示離場。

想在 Edit Mode 編輯完整測試場景，使用 `Tools > Asylum > Create Enemy Patrol Test Scene`。它會提醒保存目前場景，再建立完整場景、保存 NavMesh 並檢查各普通巡邏連線可達性。成功時 Console 顯示 `ENEMY_PATROL_VERIFY_PASS`。重新建立會重建測試場景，因此請先另存自行修改的版本。原有 NavMesh 資產不會被刪除。

## 正式關卡接法

1. 烘焙符合敵人尺寸的 NavMesh；地面、牆與關閉的門應納入導航／遮擋設定。
2. 建立 15 個物件並掛 `EnemyPatrolPoint`。1～10 代表房間，11～15 是通風口。點位應位於可行走地面。
3. 使用 `EnemyPatrolNetwork`，依 ID 順序拖入 15 個點位，從元件選單執行 `Apply agreed 15-point graph and waiting times`。此操作會覆寫連線與基礎等待時間。
4. 每個房間建立 `EnemyRoomVolume`，指定該房間的 BoxCollider（Is Trigger），填入房間編號。範圍不要重疊，走廊留在房間範圍外。判定使用座標包含檢查，無須玩家或敵人提供 Rigidbody 來觸發事件。非矩形房間可用數個相同 ID 的範圍組成。
5. 每個通風口的 `ventRoute` 依序填入轉彎位置與最終出口；出口必須位於對應房間的 NavMesh 上。B 的目的地是入口房間，F 的目的地是出口房間；入口旁等待後才切換 Vent。房間到通風口仍使用 walkSpeed。
6. 敵人掛 NavMeshAgent、EnemyPatrolBrain，指定 points、spawnPoint、eye。若有 Rigidbody，必須使用 Kinematic。模型原點以腳底為基準。
7. 每個可追擊玩家掛 EnemyTarget，sightPoint 指定頭或上半身，角色根位置需位於相應房間範圍內。尋路與捕獲使用腳底位置：有 CharacterController 時自動推算；其他角色可指定 navigationPoint 為腳底／地面追蹤點，否則使用本體 Collider 的底部或角色根位置。支援多個目標，選擇可見且最近的玩家。
8. obstructionMask 只包含牆壁與會遮擋視線的物件；排除敵人與玩家自身的 Collider。測試場景的幾何使用 Default，角色使用 Ignore Raycast。
9. onCaptured（玩家）、onCapture（敵人）、onDeparted（敵人）留給死亡／捕獲／離場效果。玩家復活後須由遊戲邏輯將 alive 設回 true。

## 行為與參數

- 移動和等待時都可偵測同房或相鄰房間內的玩家，還需滿足視距、視角與無遮擋。
- 房間鄰接讀取普通房間點位的所有普通房間連線，包含權重 0；通風管不授予跨管視野。
- 看見玩家持續更新最後目擊位置，失去視線後前往該位置；抵達／不可達／卡住則結束追擊。
- 追擊失敗返回目前實際房間的點位，走廊則選路徑距離最近的可達普通點位；不依原本巡邏來源或目標判斷。
- 進管後 CurrentRoom=0，Action=Vent，清除舊追擊目標並停止偵測；出管後更新房間、恢復偵測、步行前往出口房間的點位。
- 入口 11、12 等待 0 秒；13、14、15 基礎等待 5 秒。其他時間照清單。
- 10 號不抽下一點，不使用自我迴圈。零權重不會被難度解鎖，也不是找不到路時的備用選項。
- 隨機到達位置需同時位於 NavMesh、目標房間且路徑完整；取樣失敗退回點位中心。
- AI level 開始時取樣一次：速度倍率 `min(maximumSpeedMultiplier, 1 + level * 0.08)`；Forward 權重倍率 `1 + level * 0.15`；等待倍率 `1 / (1 + level * 0.15)`。正等待時間受 minimumWait 限制；0 秒保持 0。
- V 權重乘 ventWeight；F 權重乘前進倍率；B、S 保持基礎權重。walkSpeed／ventSpeed 保存敵人種類基礎值。
- aggression 保留數值，目前不影響任何行為。
- 回出生點途中不重新追玩家；到達出生點後恢復一般巡邏。
- 本版為獨立、本機功能測試，尚未接 Photon Fusion 的權威端 AI、位置同步或玩家死亡 RPC。正式多人遊戲應僅讓權威端驅動，其他端同步結果。

## 驗證

純選路權重回歸測試（需 .NET 10）：

```powershell
dotnet run --project Tests/EnemyPatrol/PatrolWeightChecks.csproj
```

檢查零權重、全無效連線、邊界抽樣、Vent／Forward 倍率，以及固定種子的 100,000 次分布。
Editor 建立工具另檢查房間判定、通風管出口與普通連線 NavMesh 可達性。2026-11-12 已通過 111 項場景檢查與實際 Play Mode 自動流程測試，包含巡邏、追擊、返回、捕獲、五條通風管及離場；詳見 Tests/EnemyPatrol/VERIFICATION.md。
