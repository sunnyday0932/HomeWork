void Main()
{

	var rng = new Random(12345546); // 固定種子：重現結果

	// 1) 生成出口
	var exits = GenerateExits(rng, MinimumExitCount);

	// 2) 生成參與者
	var survivors = new List<Actor>();
	var killers = new List<Actor>();
	SpawnActors(rng, survivors, killers, exits);

	// 3) 初始狀態
	int tick = 0;
	int survivorScore = 0, killerScore = 0;

	Log($"Init: Exits = {string.Join(", ", exits)}");
	Log($"Init: Survivors = {string.Join(", ", survivors)}");
	Log($"Init: Killers   = {string.Join(", ", killers)}");
	Log("---- Start ----");

	// 4) 主迴圈：奇數殺手拍、偶數倖存者拍（同步移動）
	for (tick = 1; tick <= MaxTicks; tick++)
	{
		bool isKillersTurn = (tick % 2 == 1);

		// 兩張距離圖（當前位置重新計算；50x50 成本很小）
		int[,] exitDistanceMap = ComputeExitDistanceMap(exits);
		int[,] killerDistanceMap = ComputeKillerDistanceMap(killers.Where(k => k.IsActive).Select(k => k.Position));

		if (isKillersTurn)
		{
			// ---- 殺手拍：決策 → 同步套用 → 捕捉判定 ----
			var plannedPositions = new Dictionary<int, GridPoint>();

			foreach (var killer in killers.Where(k => k.IsActive))
				plannedPositions[killer.Id] = DecideKillerMove(killer, survivors.Where(s => s.IsActive));

			foreach (var killer in killers.Where(k => k.IsActive))
				killer.Position = plannedPositions[killer.Id];

			// 捕捉：任一殺手與任一倖存者同格 → 倖存者移除，殺手隊 +1
			var killerPositions = killers.Where(k => k.IsActive).Select(k => k.Position).ToHashSet();
			var capturedSurvivorIds = survivors
				.Where(s => s.IsActive && killerPositions.Contains(s.Position))
				.Select(s => s.Id)
				.ToList();

			if (capturedSurvivorIds.Count > 0)
			{
				foreach (var sid in capturedSurvivorIds)
					survivors.First(s => s.Id == sid).IsActive = false;

				killerScore += capturedSurvivorIds.Count;
				Log($"Tick {tick} (Killers): Captured {capturedSurvivorIds.Count} -> +{capturedSurvivorIds.Count}, KillerScore={killerScore}");
			}
		}
		else
		{
			// ---- 倖存者拍：決策 → 同步套用 → 先捕捉、後逃脫 ----
			var plannedPositions = new Dictionary<int, GridPoint>();

			foreach (var survivor in survivors.Where(s => s.IsActive))
				plannedPositions[survivor.Id] = DecideSurvivorMove(survivor, exits, exitDistanceMap, killerDistanceMap);

			foreach (var survivor in survivors.Where(s => s.IsActive))
				survivor.Position = plannedPositions[survivor.Id];

			// 先捕捉（同拍同格先被抓）
			var killerPositions = killers.Where(k => k.IsActive).Select(k => k.Position).ToHashSet();
			var capturedSurvivorIds = survivors
				.Where(s => s.IsActive && killerPositions.Contains(s.Position))
				.Select(s => s.Id)
				.ToList();

			if (capturedSurvivorIds.Count > 0)
			{
				foreach (var sid in capturedSurvivorIds)
					survivors.First(s => s.Id == sid).IsActive = false;

				killerScore += capturedSurvivorIds.Count;
				Log($"Tick {tick} (Survivors): Captured {capturedSurvivorIds.Count} -> +{capturedSurvivorIds.Count}, KillerScore={killerScore}");
			}

			// 再逃脫（格上無殺手且是出口）
			var exitSet = exits.ToHashSet();
			var escapedSurvivorIds = survivors
				.Where(s => s.IsActive && exitSet.Contains(s.Position))
				.Select(s => s.Id)
				.ToList();

			if (escapedSurvivorIds.Count > 0)
			{
				foreach (var sid in escapedSurvivorIds)
					survivors.First(s => s.Id == sid).IsActive = false;

				survivorScore += escapedSurvivorIds.Count;
				Log($"Tick {tick} (Survivors): Escaped {escapedSurvivorIds.Count} -> +{escapedSurvivorIds.Count}, SurvivorScore={survivorScore}");
			}
		}

		// 終局判斷：所有倖存者已被移除，或達到最大 Tick
		if (!survivors.Any(s => s.IsActive))
		{
			Log($"All survivors removed at Tick {tick}.");
			break;
		}
		if (tick == MaxTicks)
		{
			Log($"Tick limit reached ({MaxTicks}).");
			break;
		}
	}

	// 5) 輸出結果
	Log("---- Finished ----");
	Log($"Ticks: {tick}");
	Log($"Score: Survivors={survivorScore}, Killers={killerScore}");
	string result =
		survivorScore == killerScore ? "Draw" :
		(survivorScore > killerScore ? "Survivors Win" : "Killers Win");
	Log($"Result: {result}");

}

// 地圖/回合
const int MapWidth = 50;
const int MapHeight = 50;
const int MaxTicks = 100;

// 參與者數量與出口數量
const int SurvivorCount = 3;    // 1~3
const int KillerCount = 2;      // 2~5
const int MinimumExitCount = 2; // >= 2

// 生成與安全距離（Chebyshev）
const int MinExitSeparation = 8; // 出口間至少距離
const int MinSpawnDistance = 5;  // 出生與出口/對手最小距離

// 倖存者決策相關
const int ExitThreatRadius = 4;      // 殺手靠近出口的威脅半徑
const int ExitRiskWeight = 8;        // 出口評分的殺手距離權重 α
const int SurvivorSafetyRadius = 1;  // 倖存者允許接近殺手的安全半徑
const int HighRiskPenalty = 1000;    // 高風險格懲罰（殺手當拍或下一拍可到）
const int KillerDistanceCap = 10;    // 打分時對 KillerDist 的上限



// 方向（八方向）與固定優先序（作為 tie-break）
private static GridPoint[] Directions8 = new GridPoint[]
{
			new(-1,-1), new(0,-1), new(1,-1),
			new(-1, 0),            new(1, 0),
			new(-1, 1), new(0, 1), new(1, 1)
};

private static readonly GridPoint[] DirectionPreference = new GridPoint[]
{
			new(-1,-1), new(0,-1), new(1,-1),
			new(-1, 0),            new(1, 0),
			new(-1, 1), new(0, 1), new(1, 1)
};


// 工具：距離/邊界
static int ChebyshevDistance(in GridPoint a, in GridPoint b)
	=> Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

static bool IsInsideMap(in GridPoint p)
	=> (uint)p.X < MapWidth && (uint)p.Y < MapHeight;

enum Team
{
	Survivor,
	Killer
}

/// <summary>二維整數座標（棋盤格）</summary>
private readonly record struct GridPoint(int X, int Y)
{
	public static GridPoint operator +(GridPoint a, GridPoint b) => new(a.X + b.X, a.Y + b.Y);
	public override string ToString() => $"({X},{Y})";
}

/// <summary>遊戲參與者（倖存者或殺手）</summary>
sealed class Actor
{
	public int Id { get; }
	public Team Team { get; }
	public GridPoint Position;
	public bool IsActive = true;

	public Actor(int id, Team team, GridPoint position)
	{
		Id = id;
		Team = team;
		Position = position;
	}

	public override string ToString() => $"{Team}#{Id}@{Position}";
}


// =========================
// 決策：殺手
// =========================
/// <summary>殺手：追最近倖存者（Chebyshev 最短，含斜走）。</summary>
static GridPoint DecideKillerMove(Actor killer, IEnumerable<Actor> aliveSurvivors)
{
	// 1) 選目標：Chebyshev 距離最近（平手取 Id 較小）
	Actor? target = null;
	int bestDistance = int.MaxValue;

	foreach (var survivor in aliveSurvivors)
	{
		int distance = ChebyshevDistance(killer.Position, survivor.Position);
		if (distance < bestDistance || (distance == bestDistance && survivor.Id < (target?.Id ?? int.MaxValue)))
		{
			bestDistance = distance;
			target = survivor;
		}
	}

	if (target is null) return killer.Position; // 無目標則不動

	// 2) 縮短一步（優先 (dx,dy) 斜走，其次其他同樣能縮短的方向，皆依固定優先序）
	int dx = Math.Sign(target.Position.X - killer.Position.X);
	int dy = Math.Sign(target.Position.Y - killer.Position.Y);
	var primaryStep = new GridPoint(dx, dy);

	var candidateSteps = new List<GridPoint>();
	if (!(dx == 0 && dy == 0)) candidateSteps.Add(primaryStep);

	foreach (var dir in DirectionPreference)
	{
		if (dir.Equals(primaryStep)) continue;
		var next = killer.Position + dir;
		if (!IsInsideMap(next)) continue;
		if (ChebyshevDistance(next, target.Position) < bestDistance)
			candidateSteps.Add(dir);
	}

	foreach (var step in candidateSteps)
	{
		var next = killer.Position + step;
		if (IsInsideMap(next)) return next;
	}

	return killer.Position; // 邊界特殊情況：無法縮短則留在原地
}

// =========================
// 決策：倖存者
// =========================
/// <summary>
/// 倖存者：兩張距離圖（到出口/到殺手）+ 出口風險評分 + 高風險格懲罰。
/// </summary>
static GridPoint DecideSurvivorMove(Actor survivor, List<GridPoint> exits, int[,] exitDist, int[,] killerDist)
{
	// A) 選出口（最近為基礎，加入殺手靠近出口的風險）
	int bestExitIndex = -1;
	int bestExitScore = int.MaxValue;

	for (int i = 0; i < exits.Count; i++)
	{
		var exitPoint = exits[i];
		int distanceToExit = ChebyshevDistance(survivor.Position, exitPoint);
		int killerToExit = killerDist[exitPoint.X, exitPoint.Y];

		// 風險：距出口越近越好；出口越遠離殺手越好
		// 使用簡單整合：distanceToExit + α * (1_000 / (1 + killerToExit))
		int score = distanceToExit + ExitRiskWeight * (1_000 / (1 + killerToExit));

		// 額外懲罰：若出口在威脅半徑內
		if (killerToExit <= ExitThreatRadius)
			score += 500;

		if (score < bestExitScore)
		{
			bestExitScore = score;
			bestExitIndex = i;
		}
	}

	var chosenExit = exits[bestExitIndex];

	// B) 列出「能讓 exitDist 減 1」的候選步
	int currentExitDistance = exitDist[survivor.Position.X, survivor.Position.Y];
	var greedyCandidates = new List<GridPoint>();

	foreach (var dir in DirectionPreference)
	{
		var next = survivor.Position + dir;
		if (!IsInsideMap(next)) continue;
		if (exitDist[next.X, next.Y] == currentExitDistance - 1)
			greedyCandidates.Add(dir);
	}

	// C) 打分函式：-ExitDist + KillerDist - 高風險懲罰
	int Evaluate(GridPoint step)
	{
		var next = survivor.Position + step;
		int distToExit = exitDist[next.X, next.Y];
		int distToKiller = killerDist[next.X, next.Y];

		int penalty = 0;
		// 高風險：當拍殺手在此（0）或下一拍可達（1）→ 大懲罰
		if (distToKiller <= 1) penalty += HighRiskPenalty;
		// 安全半徑內再加小懲罰，避免貼近
		if (distToKiller <= SurvivorSafetyRadius) penalty += 100;

		// 越靠近出口越好（距離越小分數越高；取負），越遠離殺手越好
		int score = -distToExit + Math.Min(distToKiller, KillerDistanceCap) - penalty;
		return score;
	}

	// D) 先嘗試「縮短出口距離」的候選
	GridPoint? bestStep = null;
	int bestScore = int.MinValue;

	foreach (var step in greedyCandidates)
	{
		int score = Evaluate(step);
		if (score > bestScore) { bestScore = score; bestStep = step; }
	}

	// E) 若皆不理想（過於危險），允許逃生步：不一定縮短，但盡量拉開與殺手距離
	if (bestStep is null)
	{
		foreach (var step in DirectionPreference)
		{
			var next = survivor.Position + step;
			if (!IsInsideMap(next)) continue;
			int score = Evaluate(step);
			if (score > bestScore) { bestScore = score; bestStep = step; }
		}
	}

	return bestStep is null ? survivor.Position : survivor.Position + bestStep.Value;
}


// =========================
// BFS 距離圖
// =========================
static int[,] ComputeExitDistanceMap(List<GridPoint> exits)
	=> ComputeDistanceMapFromSources(exits);

static int[,] ComputeKillerDistanceMap(IEnumerable<GridPoint> killerPositions)
	=> ComputeDistanceMapFromSources(killerPositions);

/// <summary>
/// 多源 BFS：輸入多個起點（距離=0），回傳每格到最近起點的最短步數（一步=八方向任一）。
/// </summary>
static int[,] ComputeDistanceMapFromSources(IEnumerable<GridPoint> sources)
{
	const int INF = int.MaxValue / 4;
	var distance = new int[MapWidth, MapHeight];

	for (int x = 0; x < MapWidth; x++)
		for (int y = 0; y < MapHeight; y++)
			distance[x, y] = INF;

	var queue = new Queue<GridPoint>();

	foreach (var src in sources)
	{
		distance[src.X, src.Y] = 0;
		queue.Enqueue(src);
	}

	while (queue.Count > 0)
	{
		var p = queue.Dequeue();
		int nextDistance = distance[p.X, p.Y] + 1;

		foreach (var dir in Directions8)
		{
			var np = p + dir;
			if (!IsInsideMap(np)) continue;
			if (distance[np.X, np.Y] <= nextDistance) continue;

			distance[np.X, np.Y] = nextDistance;
			queue.Enqueue(np);
		}
	}

	return distance;
}

// =========================
// 生成：出口與參與者
// =========================
static List<GridPoint> GenerateExits(Random rng, int requiredCount)
{
	var exits = new List<GridPoint>();
	while (exits.Count < requiredCount)
	{
		var candidate = new GridPoint(rng.Next(MapWidth), rng.Next(MapHeight));
		if (exits.Count == 0 || exits.All(e => ChebyshevDistance(e, candidate) >= MinExitSeparation))
			exits.Add(candidate);
	}
	return exits;
}

static void SpawnActors(Random rng, List<Actor> survivors, List<Actor> killers, List<GridPoint> exits)
{
	var occupied = new HashSet<(int X, int Y)>();
	bool IsFree(GridPoint p) => !occupied.Contains((p.X, p.Y)) && !exits.Contains(p);

	// 先放殺手
	for (int i = 0; i < KillerCount; i++)
	{
		GridPoint pos;
		int tries = 0;
		do
		{
			pos = new GridPoint(rng.Next(MapWidth), rng.Next(MapHeight));
			tries++;
		}
		while ((!IsFree(pos) || exits.Any(e => ChebyshevDistance(e, pos) < MinSpawnDistance)) && tries < 10_000);

		killers.Add(new Actor(i, Team.Killer, pos));
		occupied.Add((pos.X, pos.Y));
	}

	// 再放倖存者（與出口、殺手都保持距離）
	for (int i = 0; i < SurvivorCount; i++)
	{
		GridPoint pos;
		int tries = 0;
		do
		{
			pos = new GridPoint(rng.Next(MapWidth), rng.Next(MapHeight));
			tries++;
		}
		while ((!IsFree(pos)
				|| exits.Any(e => ChebyshevDistance(e, pos) < MinSpawnDistance)
				|| killers.Any(k => ChebyshevDistance(k.Position, pos) < MinSpawnDistance))
				&& tries < 10_000);

		survivors.Add(new Actor(i, Team.Survivor, pos));
		occupied.Add((pos.X, pos.Y));
	}
}

// =========================
// 記錄輸出
// =========================
static void Log(string message) => Console.WriteLine(message);

// You can define other methods, fields, classes and namespaces here