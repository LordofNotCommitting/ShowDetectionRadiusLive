using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace ShowDetectionRadiusLive
{
    [HarmonyPatch(typeof(SelectTargetView), nameof(SelectTargetView.HighlightObstaclesOnShootTrajectory))]
    public static class AddDetectionRadius
    {
        private struct Segment
        {
            public Vector2 Start;
            public Vector2 End;

            public Segment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }
        }

        // ==========================================
        // 1. TILE POOL SYSTEM (Edge_vision == false)
        // ==========================================
        public static class CustomTilePool
        {
            private static readonly Queue<GameObject> _pool = new Queue<GameObject>();
            private static readonly List<GameObject> _activeTiles = new List<GameObject>();

            private static readonly Color TargetGreenColor = new Color(0f, 1f, 0f, 0.25f);

            private static readonly int RendererColorID = Shader.PropertyToID("_RendererColor");
            private static readonly int ColorID = Shader.PropertyToID("_Color");
            private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

            private static readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();

            private static Texture2D _whiteTexture;

            private static Texture2D GetWhiteTexture()
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }

            public static GameObject GetTile(GameObject template, Vector3 position, Transform parent)
            {
                GameObject tile;

                if (_pool.Count > 0)
                {
                    tile = _pool.Dequeue();
                    tile.transform.position = position;
                    tile.SetActive(true);
                }
                else
                {
                    tile = UnityEngine.Object.Instantiate(template, position, template.transform.rotation);

                    if (parent != null)
                    {
                        tile.transform.SetParent(parent, true);
                    }

                    tile.transform.localScale = template.transform.localScale;

                    SpriteRenderer cloneSR = tile.GetComponent<SpriteRenderer>();

                    if (cloneSR != null)
                    {
                        cloneSR.sortingLayerID = SortingLayer.NameToID("Doors");
                        cloneSR.sortingOrder = 5;
                        cloneSR.enabled = true;
                    }

                    tile.SetActive(true);
                }

                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.GetPropertyBlock(_propBlock);

                    _propBlock.SetTexture(MainTexID, GetWhiteTexture());
                    _propBlock.SetColor(RendererColorID, TargetGreenColor);
                    _propBlock.SetColor(ColorID, TargetGreenColor);

                    sr.SetPropertyBlock(_propBlock);
                }

                _activeTiles.Add(tile);
                return tile;
            }

            public static void RecycleAll()
            {
                foreach (var tile in _activeTiles)
                {
                    if (tile != null)
                    {
                        tile.SetActive(false);
                        _pool.Enqueue(tile);
                    }
                }
                _activeTiles.Clear();
            }
        }

        // ==========================================
        // 2. LINE POOL SYSTEM (Edge_vision == true)
        // ==========================================
        public static class LinePool
        {
            private static readonly Queue<LineRenderer> _pool = new Queue<LineRenderer>();
            private static readonly List<LineRenderer> _activeLines = new List<LineRenderer>();
            private static readonly Color TargetGreenColor = new Color(0f, 1f, 0f, 0.6f);

            private static float GetWorldLineWidth(float pixelWidth)
            {
                Camera mainCam = Camera.main;
                if (mainCam == null) return 0.05f;
                return (pixelWidth * mainCam.orthographicSize * 2f) / mainCam.pixelHeight;
            }

            public static LineRenderer GetLine(Transform parent)
            {
                LineRenderer lr;

                if (_pool.Count > 0)
                {
                    lr = _pool.Dequeue();
                    lr.gameObject.SetActive(true);
                }
                else
                {
                    GameObject go = new GameObject("DetectionRadiusLine");
                    if (parent != null) go.transform.SetParent(parent, false);

                    lr = go.AddComponent<LineRenderer>();
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.sortingLayerID = SortingLayer.NameToID("Doors");
                    lr.sortingOrder = 5;
                    lr.useWorldSpace = true;
                    lr.alignment = LineAlignment.TransformZ;
                    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lr.receiveShadows = false;
                }

                float width = GetWorldLineWidth(2f); // 2px thickness
                lr.startWidth = width;
                lr.endWidth = width;

                lr.startColor = TargetGreenColor;
                lr.endColor = TargetGreenColor;

                _activeLines.Add(lr);
                return lr;
            }

            public static void RecycleAll()
            {
                foreach (var line in _activeLines)
                {
                    if (line != null)
                    {
                        line.gameObject.SetActive(false);
                        _pool.Enqueue(line);
                    }
                }
                _activeLines.Clear();
            }
        }


        static bool Tile_Vision = Plugin.ConfigGeneral.ModData.GetConfigValue<bool>("Tile_Vision", true);
        static bool Edge_Vision = Plugin.ConfigGeneral.ModData.GetConfigValue<bool>("Edge_Vision", false);

        public static void Postfix(SelectTargetView __instance)
        {
            if ((!Tile_Vision && !Edge_Vision) || __instance == null || __instance._creatures?.Player == null) return;

            var playerData = __instance._creatures.Player.CreatureData;
            if (playerData.EffectsController.HasAnyEffect<WoundEffectRevealingEye>() || !__instance._creatures.Player.IsAbleToSpotAnEnemy()) return;

            CustomTilePool.RecycleAll();
            LinePool.RecycleAll();

            int enemyRevealRange = playerData.GetEnemyRevealRange();
            if (enemyRevealRange <= 0) return;

            GameObject templateTile = __instance.DrawSpareAreaCell(new CellPosition(0, 0));
            if (templateTile == null) return;

            templateTile.SetActive(false);

            if (Edge_Vision)
            {
                // ==========================================
                // BRANCH A: OUTLINE AT VERY OUTER PERIMETER ONLY
                // ==========================================
                HashSet<CellPosition> filledDisk = new HashSet<CellPosition>();
                CollectFilledDiskCells(playerData.Position, enemyRevealRange, filledDisk, __instance);

                if (filledDisk.Count == 0) return;

                Vector2 cellSize = Vector2.one;
                SpriteRenderer templateSR = templateTile.GetComponent<SpriteRenderer>();
                if (templateSR != null)
                {
                    cellSize = templateSR.bounds.size;
                }

                // Extract ONLY edges where neighbor is OUTSIDE the radius
                List<Segment> outerSegments = ExtractOuterEdges(filledDisk, playerData.Position, enemyRevealRange, cellSize, __instance);
                List<List<Vector3>> paths = StitchSegmentsToPaths(outerSegments);

                foreach (var path in paths)
                {
                    LineRenderer lr = LinePool.GetLine(__instance.transform);
                    lr.positionCount = path.Count;
                    lr.SetPositions(path.ToArray());
                }
            }
            if (Tile_Vision)
            {
                // ==========================================
                // BRANCH B: GREEN TILE RING
                // ==========================================
                HashSet<CellPosition> radiusEdgePositions = new HashSet<CellPosition>();
                CollectRadiusEdgeCells(playerData.Position, enemyRevealRange, radiusEdgePositions, __instance);

                if (radiusEdgePositions.Count == 0) return;

                foreach (CellPosition pos in radiusEdgePositions)
                {
                    Vector3 worldPos = DrawHelper.FromCellToWorldPosition(pos.X, pos.Y, __instance._mapRenderer);
                    GameObject greenTile = CustomTilePool.GetTile(templateTile, worldPos, templateTile.transform.parent);
                }
            }
        }

        // Collects ALL cells inside radius (entire filled circle)
        private static void CollectFilledDiskCells(CellPosition center, int radius, HashSet<CellPosition> radiusPositions, SelectTargetView instance)
        {
            if (instance._mapGrid == null || radius <= 0) return;

            float maxDist = (float)radius;

            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(instance._mapGrid.MaxWidth - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(instance._mapGrid.MaxHeight - 1, center.Y + radius);

            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    CellPosition cellPos = new CellPosition(i, j);
                    float dist = center.Distance(cellPos);

                    if (dist <= maxDist)
                    {
                        MapCell cell = instance._mapGrid.GetCell(cellPos, true);
                        if (cell != null)
                        {
                            radiusPositions.Add(cellPos);
                        }
                    }
                }
            }
        }

        // Collects only outer edge ring of cells (for tile mode)
        private static void CollectRadiusEdgeCells(CellPosition center, int radius, HashSet<CellPosition> radiusPositions, SelectTargetView instance)
        {
            if (instance._mapGrid == null || radius <= 0) return;

            float maxDist = (float)radius;

            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(instance._mapGrid.MaxWidth - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(instance._mapGrid.MaxHeight - 1, center.Y + radius);

            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    CellPosition cellPos = new CellPosition(i, j);
                    float dist = center.Distance(cellPos);

                    if (dist <= maxDist && dist > (maxDist - 1.0f))
                    {
                        MapCell cell = instance._mapGrid.GetCell(cellPos, true);
                        if (cell != null)
                        {
                            radiusPositions.Add(cellPos);
                        }
                    }
                }
            }
        }

        private static List<Segment> ExtractOuterEdges(HashSet<CellPosition> filledDisk, CellPosition center, int radius, Vector2 cellSize, SelectTargetView instance)
        {
            List<Segment> outerEdges = new List<Segment>();

            // Directions: Right (+X), Top (+Y), Left (-X), Bottom (-Y)
            int[] dx = { 1, 0, -1, 0 };
            int[] dy = { 0, 1, 0, -1 };

            float halfW = cellSize.x * 0.5f;
            float halfH = cellSize.y * 0.5f;

            foreach (CellPosition cell in filledDisk)
            {
                Vector3 centerWorld = DrawHelper.FromCellToWorldPosition(cell.X, cell.Y, instance._mapRenderer);

                Vector2 bl = new Vector2(centerWorld.x - halfW, centerWorld.y - halfH);
                Vector2 br = new Vector2(centerWorld.x + halfW, centerWorld.y - halfH);
                Vector2 tr = new Vector2(centerWorld.x + halfW, centerWorld.y + halfH);
                Vector2 tl = new Vector2(centerWorld.x - halfW, centerWorld.y + halfH);

                Segment[] cellEdges = new Segment[]
                {
                    new Segment(br, tr), // Right
                    new Segment(tr, tl), // Top
                    new Segment(tl, bl), // Left
                    new Segment(bl, br)  // Bottom
                };

                for (int i = 0; i < 4; i++)
                {
                    CellPosition neighbor = new CellPosition(cell.X + dx[i], cell.Y + dy[i]);

                    // An edge belongs to the outer perimeter ONLY if its neighbor is outside the disk radius
                    if (!filledDisk.Contains(neighbor) || center.Distance(neighbor) > radius)
                    {
                        outerEdges.Add(cellEdges[i]);
                    }
                }
            }

            return outerEdges;
        }

        private static List<List<Vector3>> StitchSegmentsToPaths(List<Segment> segments)
        {
            List<List<Vector3>> paths = new List<List<Vector3>>();
            if (segments.Count == 0) return paths;

            List<Segment> pool = new List<Segment>(segments);

            while (pool.Count > 0)
            {
                List<Vector3> currentPath = new List<Vector3>();
                Segment first = pool[0];
                pool.RemoveAt(0);

                currentPath.Add(first.Start);
                currentPath.Add(first.End);

                bool edgeAdded = true;
                while (edgeAdded && pool.Count > 0)
                {
                    edgeAdded = false;
                    Vector2 currentTail = currentPath[currentPath.Count - 1];

                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (Vector2.Distance(pool[i].Start, currentTail) < 0.05f)
                        {
                            currentPath.Add(pool[i].End);
                            pool.RemoveAt(i);
                            edgeAdded = true;
                            break;
                        }
                        else if (Vector2.Distance(pool[i].End, currentTail) < 0.05f)
                        {
                            currentPath.Add(pool[i].Start);
                            pool.RemoveAt(i);
                            edgeAdded = true;
                            break;
                        }
                    }
                }

                if (currentPath.Count > 1)
                {
                    paths.Add(currentPath);
                }
            }

            return paths;
        }
    }
}