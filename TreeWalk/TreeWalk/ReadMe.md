# 選擇題目理由
  寫完第一題後看了一下二跟三，直覺反應是題目二比較容易驗證，而且真實模擬實際場景所需耗費時間也不會太長，所以決定選二先做。

# 文件內容
  - docs
    - DiscussionContext : 內有完整與 AI 對話設計此 POC 內容。
    - memory.json : 供未來其他對話視窗此 AI 可以快速回憶起之前對話的內容摘要。

# 程式架構
  - .net 8 
  - 本專案直接使用 xunit 測試框架進行開發，可直接執行 dotnet test 。
  - Benchmark 測試方式可以使用 cd 到 TreewalkforBenchmark 資料夾內，使用 dotnet run -c release 執行。


# 📌 總結整理

## 1. 思考與討論的關鍵點

* **需求定義**

  * 使用者輸入一個字串，代表一棵二元樹（level-order，空字串代表空子樹）。
  * 使用者輸入 `source` 與 `destination`，系統需找出最短路徑與完整移動方向（如 `U/R/L`）。
  * 樹深度最多 10 層，會被使用數十萬次，因此需要快取策略。

* **兩大解法方向**

  * **方案 A：預先 BFS 建所有點對最短路徑**

    * 優化變體：

      1. **NextHopMatrix**：記錄 `(u,v)` 的下一步節點。
      2. **NextDirectionMatrix**：記錄 `(u,v)` 的第一步方向。
      3. **NextDirection+DistanceMatrix**：記錄 `(u,v)` 的第一步方向 + 距離。
    * 討論了 key 儲存方式、是否需要存反向、如何節省一半儲存量等。
  * **方案 B：LCA (Lowest Common Ancestor)**

    * 利用 Binary Lifting 預處理，查詢時找到 LCA，再拼接路徑。
    * 記憶體消耗極低，建表快，但每次查詢需計算 O(logN)。

* **方向符號的討論**

  * 最初考慮將 `U` 細分為 `UL`/`UR`，但最後決定統一只用 `U`（簡單直觀即可）。

* **快取考量**

  * 生產環境不適合直接用 `Dictionary` 儲存，需考慮：

    * **不可變結構**（FrozenDictionary/ImmutableDictionary）。
    * **IMemoryCache** 控管大小與過期。
    * **Lazy + per-key 鎖**避免 cache stampede。
    * **Redis** 可作為多機共享的 metadata 層，本機仍用 MemoryCache 提速。

---

## 2. 測試案例設計

我們設計了多種測試樹來驗證策略正確性與健壯性：

1. **簡單小樹**（2–3 層）

   * 驗證基本路徑計算（如 B→F）。
2. **不對稱樹**（含空子樹）

   * 驗證處理 `null` 子樹的邏輯。
3. **深度測試**（ZigZag 到深度 10）

   * 驗證極端長路徑（A→J、J→A）。
   * J→A 要能正確輸出 9 個 `U`。
4. **完全滿樹（深度 10，1023 節點）**

   * 供 Benchmark 使用，模擬最大規模情境。

測試內容包括：

* 距離是否正確。
* 路徑方向是否符合預期。
* 反向路徑是否與正向對稱。

---

## 3. Benchmark 實驗數據

在「深度 10 完全滿樹（1023 節點）」下，用 BenchmarkDotNet 測得結果：

* **Build 時間與記憶體**

  * NextHopMatrix ≈ 52ms / 60MB
  * NextDirectionMatrix ≈ 46ms / 59MB
  * NextDirection+Distance ≈ 49ms / 60MB
  * LCA ≈ 0.08ms / 0.27MB

* **單次查詢時間**

  * Root→Leaf (距離=9)：NextHop \~90ns，LCA \~200ns
  * Leaf→Leaf (距離=18)：NextHop \~160ns，LCA \~276ns
  * Mid→Mid：NextHop \~124ns，LCA \~222ns
  * NextDirection 系列比 NextHop 慢 2 倍，沒有明顯優勢。

👉 查詢效能差距小（都在 100–300ns 級別），但建表差距極大。

---

## 4. 結論與適用情境

* **NextHopMatrix**

  * **優點**：查詢最快，方向產生簡單。
  * **缺點**：建表 O(N²)，佔用 \~60MB，樹更新代價高。
  * **適合情境**：樹固定、查詢量極大（數百萬次以上）、記憶體充足。

* **NextDirectionMatrix / NextDirection+Distance**

  * **優點**：理論上節省一半資訊；+Distance 在「只要距離」時 O(1)。
  * **缺點**：查詢速度比 NextHop 慢、記憶體與建表幾乎一樣大。
  * **適合情境**：需求只問「距離」時，可考慮 NextDir+Dist；其他情境不如 NextHop 或 LCA。

* **LowestCommonAncestor (LCA)**

  * **優點**：建表極快（\~0.1ms）、極省記憶體（0.27MB），查詢速度接近 NextHop。
  * **缺點**：查詢有 O(logN) 成本，但在深度 10 的樹上幾乎感受不到。
  * **適合情境**：樹會更新、記憶體敏感、或需要高彈性時。

---

# 🎯 總結一句話

* **固定樹、追求查詢極速** → 選 **NextHopMatrix**。
* **樹可能重建、記憶體有限** → 選 **LCA**。
* **只要距離** → NextDirection+Distance 有價值，但其他情境不如前兩者。

---

要不要我幫你把這份總結再整理成 **Mermaid 流程圖**（「如果需求是 X → 選擇哪個策略」）方便在簡報或文件裡快速呈現？


# Benchmark 跑分結果

| Method                                            | Mean             | Error            | StdDev         | Gen0      | Gen1     | Gen2     | Allocated  |
|-------------------------------------------------- |-----------------:|-----------------:|---------------:|----------:|---------:|---------:|-----------:|
| 'Build NextHopMatrix'                             | 52,545,655.00 ns | 2,988,466.894 ns | 462,468.559 ns | 3500.0000 | 400.0000 | 100.0000 | 60115644 B |
| 'Build NextDirectionMatrix'                       | 46,187,601.82 ns | 1,247,526.685 ns | 323,978.719 ns | 3454.5455 | 272.7273 |        - | 59069048 B |
| 'Build NextDirectionWithDistance'                 | 49,227,290.00 ns | 1,829,319.464 ns | 475,068.456 ns | 3500.0000 | 400.0000 | 100.0000 | 60115702 B |
| 'Build LowestCommonAncestor'                      |     78,984.92 ns |     3,918.083 ns |   1,017.514 ns |   16.1133 |   7.9346 |        - |   270280 B |
| 'Query NextHop: root->leftmostLeaf'               |         92.67 ns |         3.377 ns |       0.877 ns |    0.0215 |        - |        - |      360 B |
| 'Query NextDir: root->leftmostLeaf'               |        180.82 ns |         3.934 ns |       0.609 ns |    0.0215 |        - |        - |      360 B |
| 'Query NextDir+Dist: root->leftmostLeaf'          |        183.46 ns |        12.243 ns |       3.180 ns |    0.0215 |        - |        - |      360 B |
| 'Query LCA: root->leftmostLeaf'                   |        200.70 ns |         5.168 ns |       1.342 ns |    0.0434 |        - |        - |      728 B |
| 'Query NextHop: leftmostLeaf->rightmostLeaf'      |        159.42 ns |         4.060 ns |       1.054 ns |    0.0381 |        - |        - |      640 B |
| 'Query NextDir: leftmostLeaf->rightmostLeaf'      |        343.96 ns |        51.227 ns |      13.303 ns |    0.0381 |        - |        - |      640 B |
| 'Query NextDir+Dist: leftmostLeaf->rightmostLeaf' |        331.23 ns |        13.395 ns |       2.073 ns |    0.0381 |        - |        - |      640 B |
| 'Query LCA: leftmostLeaf->rightmostLeaf'          |        275.89 ns |        12.879 ns |       3.345 ns |    0.0601 |        - |        - |     1008 B |
| 'Query NextHop: midLeft->midRight'                |        123.50 ns |         2.684 ns |       0.697 ns |    0.0215 |        - |        - |      360 B |
| 'Query NextDir: midLeft->midRight'                |        278.06 ns |         8.190 ns |       2.127 ns |    0.0215 |        - |        - |      360 B |
| 'Query NextDir+Dist: midLeft->midRight'           |        273.94 ns |         8.129 ns |       1.258 ns |    0.0215 |        - |        - |      360 B |
| 'Query LCA: midLeft->midRight'                    |        221.66 ns |        51.838 ns |      13.462 ns |    0.0343 |        - |        - |      576 B |
