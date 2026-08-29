using HarmonyLib;
using MGSC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShowDetectionRadiusLive
{
    [HarmonyPatch(typeof(SelectTargetView), nameof(SelectTargetView.HighlightObstaclesOnShootTrajectory))]
    //consider alternative attachment location at PlayerInteractionSystem.ProcessInput
    // or VisibilitySystem.UpdateVisibility
    public static class AddDetectionRadius
    {

        public static class CustomTilePool
        {
            private static readonly Queue<GameObject> _pool = new Queue<GameObject>();
            private static readonly List<GameObject> _activeTiles = new List<GameObject>();

            private static readonly Color TargetGreenColor = new Color(0f, 1f, 0f, 0.25f);

            private static readonly int RendererColorID = Shader.PropertyToID("_RendererColor");
            private static readonly int ColorID = Shader.PropertyToID("_Color");
            private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

            private static readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();



            // Create a static pure white fallback texture
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
                        // === HARDCODED FOG-OF-WAR BYPASS ===
                        // Force the sprite onto the exact layer and order used by UI signals
                        cloneSR.sortingLayerID = SortingLayer.NameToID("Doors");
                        cloneSR.sortingOrder = 5; // 7 ensures it draws just above the signal (6)

                        cloneSR.enabled = true;
                    }

                    tile.SetActive(true);
                }

                // Apply white texture replacement + neon green tint
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
                // 1. Recycle pooled green tile GameObjects
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


        static bool Radius_When_Aim = Plugin.ConfigGeneral.ModData.GetConfigValue<bool>("Radius_When_Aim", true);

        public static void Postfix(SelectTargetView __instance)
        {
            if (!Radius_When_Aim || __instance == null || __instance._creatures?.Player == null) return;

            var playerData = __instance._creatures.Player.CreatureData;
            if (playerData.EffectsController.HasAnyEffect<WoundEffectRevealingEye>() || !__instance._creatures.Player.IsAbleToSpotAnEnemy()) return;

            CustomTilePool.RecycleAll();

            HashSet<CellPosition> radiusEdgePositions = new HashSet<CellPosition>();

            int enemyRevealRange = playerData.GetEnemyRevealRange();
            CollectRadiusEdgeCells(playerData.Position, enemyRevealRange, radiusEdgePositions, __instance);

            if (radiusEdgePositions.Count == 0) return;

            GameObject templateTile = __instance.DrawSpareAreaCell(new CellPosition(0, 0));
            if (templateTile == null) return;

            templateTile.SetActive(false);

            FogOfWar fowInstance = null;
            bool fowUpdated = false;

            foreach (CellPosition pos in radiusEdgePositions)
            {

                Vector3 worldPos = DrawHelper.FromCellToWorldPosition(pos.X, pos.Y, __instance._mapRenderer);
                GameObject greenTile = CustomTilePool.GetTile(templateTile, worldPos, templateTile.transform.parent);
            }

        }

        private static void CollectRadiusEdgeCells(CellPosition center, int radius, HashSet<CellPosition> radiusPositions, SelectTargetView instance)
        {
            if (instance._mapGrid == null || radius <= 0) return;

            float maxDist = (float)radius;

            // Use MapGrid's MaxWidth and MaxHeight properties
            int minX = Math.Max(0, center.X - radius);
            int maxX = Math.Min(instance._mapGrid.MaxWidth - 1, center.X + radius);
            int minY = Math.Max(0, center.Y - radius);
            int maxY = Math.Min(instance._mapGrid.MaxHeight - 1, center.Y + radius);

            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    CellPosition cellPos = new CellPosition(i, j);

                    // Floating-point distance check matching native engine math
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


    }
}
