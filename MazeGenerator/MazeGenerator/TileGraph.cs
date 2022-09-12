using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MazeGenerator
{
    [Flags]
    public enum TileTypes
    {
        Space = 0,
        VisitedSpace = 1,
        Sand = 2,
        Wall = 4,
        Start = 8,
        End = 16
    }
    public enum SpecialTileTypes
    {
        None,
        Start,
        End
    }

    public class Tile
    {
        public enum Borders
        {
            Bottom,
            Right
        }
        public TileTypes TileType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; }
        public int BorderSize { get; set; }
        public Color[] BorderColors { get; set; }
        public Texture2D Texture { get; set; }
        public Texture2D BorderTexture { get; set; }

        public Tile(int x, int y, int size, int borderSize, TileTypes tileType, Texture2D texture, GraphicsDevice graphicsDevice)
        {
            X = x;
            Y = y;
            Size = size;
            BorderSize = borderSize;
            BorderColors = new Color[2]
            {
                Color.Black,
                Color.Black,
            };
            TileType = tileType;
            Texture = texture;

            BorderTexture = new Texture2D(graphicsDevice, 1, 1);
            BorderTexture.SetData(new Color[]
            {
                Color.White
            });
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            var color = Color.LightGray;

            if (TileType.HasFlag(TileTypes.Start))
            {
                color = Color.Green;
            }
            else if (TileType.HasFlag(TileTypes.End))
            {
                color = Color.Red;
            }
            else if (TileType.HasFlag(TileTypes.Wall))
            {
                color = Color.DimGray;
            }
            else if (TileType == TileTypes.VisitedSpace)
            {
                color = Color.LightSkyBlue;
            }
            else if (TileType.HasFlag(TileTypes.Sand) && !TileType.HasFlag(TileTypes.VisitedSpace))
            {
                color = Color.SandyBrown;
            }
            else if (TileType.HasFlag(TileTypes.Sand) && TileType.HasFlag(TileTypes.VisitedSpace))
            {
                color = Color.Lerp(Color.LightSkyBlue, Color.SandyBrown, 0.5f);
            }

            var rect = new Rectangle(X * Size + X * BorderSize, Y * Size + Y * BorderSize, Size, Size);

            // Tile
            spriteBatch.Draw(Texture, rect, color);

            var bottom = BorderColors[(int)Borders.Bottom];
            var right = BorderColors[(int)Borders.Right];

            // Bottom Border
            spriteBatch.Draw(BorderTexture, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, BorderSize),
                             TileType.HasFlag(TileTypes.VisitedSpace) && bottom == Color.LightGray ? color : bottom);
            // Right Border
            spriteBatch.Draw(BorderTexture, new Rectangle(rect.X + rect.Width, rect.Y, BorderSize, rect.Height),
                             TileType.HasFlag(TileTypes.VisitedSpace) && right == Color.LightGray ? color : right);
            // Space between borders
            spriteBatch.Draw(BorderTexture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, BorderSize, BorderSize), Color.Black);
        }
    }

    public class TileGraph
    {
        public OrthographicCamera Camera { get; set; }

        public int GraphSize { get; set; }
        public float CardinalDistance { get; set; }
        public float OrdinalDistance { get; set; }

        public Texture2D TileTexture { get; set; }
        public Texture2D ButtonTexture { get; set; }
        public Texture2D ZoomInButtonTexture { get; set; }
        public Texture2D ZoomOutButtonTexture { get; set; }
        public SpriteFont ButtonFont { get; set; }

        public int TileSize { get; set; }
        public int TileBorderSize { get; set; }
        public int SandTileWeight { get; set; }
        Tile[,] tiles { get; set; }
        Tile lastValidTile { get; set; }

        TileTypes draggingTileType { get; set; }
        TileTypes selectedTileType { get; set; }
        SpecialTileTypes specialTileType { get; set; }

        Dictionary<Point, Vertex> vertexMap { get; set; }
        WeightedDirectedGraph<Tile> vertexGraph { get; set; }

        enum pathfinderNames
        {
            Djikstra,
            AStar
        }
        Func<Vertex, Vertex, float, float, float> heuristic { get; set; }
        Vertex startVertex { get; set; }
        Vertex endVertex { get; set; }
        List<Vertex> path;
        List<Vertex> visitedNodes;

        enum buttonNames
        {
            GenMaze,
            ZoomIn,
            ZoomOut,
            RunDjikstra,
            RunAStar,
            SelectManhattan,
            SelectOctile,
            SelectChebyshev,
            SelectEuclidean
        }
        Button[] buttons { get; set; }
        Vector2 ButtonInfoPos { get; set; }

        int visualizationProgressionIndex { get; set; }
        Stopwatch stopwatch { get; set; }

        public TileGraph(
            GraphicsDevice graphicsDevice,
            Texture2D tileTexture,
            Texture2D buttonTexture,
            Texture2D zoomInButtonTexture,
            Texture2D zoomOutButtonTexture,
            SpriteFont buttonFont)
        {
            Camera = new OrthographicCamera(graphicsDevice)
            {
                MaximumZoom = 10,
                MinimumZoom = 1
            };

            TileTexture = tileTexture;
            ButtonTexture = buttonTexture;
            ZoomInButtonTexture = zoomInButtonTexture;
            ZoomOutButtonTexture = zoomOutButtonTexture;
            ButtonFont = buttonFont;

            GraphSize = 48;
            CardinalDistance = 1;
            OrdinalDistance = (float)Math.Sqrt(2);

            TileSize = 14;
            TileBorderSize = 1;
            SandTileWeight = 2;
            tiles = new Tile[GraphSize, GraphSize];

            selectedTileType = TileTypes.Wall;
            specialTileType = SpecialTileTypes.None;

            vertexMap = new Dictionary<Point, Vertex>();
            vertexGraph = new WeightedDirectedGraph<Tile>(SandTileWeight);

            #region BuildingGraph
            for (int y = 0; y < GraphSize; y++)
            {
                for (int x = 0; x < GraphSize; x++)
                {
                    tiles[y, x] = new Tile(x, y, TileSize, TileBorderSize, TileTypes.Space, TileTexture, graphicsDevice);

                    var point = new Point(x, y);
                    var vertex = new Vertex(x, y, Vertex.Types.Space);

                    vertexMap.Add(point, vertex);
                    vertexGraph.AddVertex(vertexMap[point]);
                }
            }
            //var points = new Point[8];
            //foreach (var point in vertexMap.Keys)
            //{
            //    points[0] = new Point(point.X, point.Y - 1);    // up
            //    points[1] = new Point(point.X, point.Y + 1);    // down
            //    points[2] = new Point(point.X - 1, point.Y);    // left
            //    points[3] = new Point(point.X + 1, point.Y);    // right
            //    points[4] = new Point(point.X - 1, point.Y - 1);// up left
            //    points[5] = new Point(point.X + 1, point.Y - 1);// up right
            //    points[6] = new Point(point.X - 1, point.Y + 1);// down left
            //    points[7] = new Point(point.X + 1, point.Y + 1);// down right

            //    for (int i = 0; i < points.Length; i++)
            //    {
            //        if (!vertexMap.ContainsKey(points[i])) continue;

            //        Vertex neighborVertex = vertexMap[points[i]];
            //        float weight = i >= 4 ? OrdinalDistance : CardinalDistance;

            //        if (tiles[points[i].Y, points[i].X].TileType.HasFlag(TileTypes.Sand))
            //        {
            //            weight = SandTileWeight;
            //            if (i >= 4 && CardinalDistance != OrdinalDistance)
            //            {
            //                weight = (float)Math.Sqrt(2 * (SandTileWeight ^ 2));
            //            }
            //        }

            //        vertexGraph.AddEdge(vertexMap[point],
            //            neighborVertex,
            //            weight);
            //    }
            //}

            #endregion BuildingGraph

            heuristic = WeightedDirectedGraph<int>.Manhattan;
            path = new List<Vertex>();
            visitedNodes = new List<Vertex>();

            #region SettingButtonProperties
            buttons = new Button[9];

            buttons[(int)buttonNames.GenMaze] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 50,
                height: 50,
                action: () => { GenMaze();/*ChangeSelectedObstacle(TileTypes.Wall);*/ },
                text: "Generate Maze",
                fontColor: Color.White,
                backgroundColor: Color.White);//Color.Lerp(Color.DimGray, Color.LightSkyBlue, .25f));

            buttons[(int)buttonNames.ZoomIn] = new Button(
                texture: ZoomInButtonTexture,
                font: ButtonFont,
                width: 50,
                height: 50,
                action: () => { Camera.ZoomIn(1); },
                text: "Zoom In",
                fontColor: Color.White,
                backgroundColor: null);

            buttons[(int)buttonNames.ZoomOut] = new Button(
                texture: ZoomOutButtonTexture,
                font: ButtonFont,
                width: 50,
                height: 50,
                action: () => { Camera.ZoomOut(1); },
                text: "Zoom Out",
                fontColor: Color.White,
                backgroundColor: null);

            buttons[(int)buttonNames.RunDjikstra] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 50,
                height: 50,
                action: () => { RunPathfinding(pathfinderNames.Djikstra); },
                text: "Run Djikstra",
                fontColor: Color.White,
                backgroundColor: null);

            buttons[(int)buttonNames.RunAStar] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 50,
                height: 50,
                action: () => { RunPathfinding(pathfinderNames.AStar); },
                text: "Run A*",
                fontColor: Color.White,
                backgroundColor: null);

            buttons[(int)buttonNames.SelectManhattan] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 25,
                height: 25,
                action: () =>
                {
                    ChangeSelectedHeuristic(
                        newHeuristic: WeightedDirectedGraph<int>.Manhattan,
                        buttonIndex: (int)buttonNames.SelectManhattan,
                        newOrdinalDistance: (float)Math.Sqrt(2));
                },
                text: "Manhattan Heuristic (Selected)",
                fontColor: Color.White,
                backgroundColor: Color.LightSkyBlue);

            buttons[(int)buttonNames.SelectOctile] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 25,
                height: 25,
                action: () =>
                {
                    ChangeSelectedHeuristic(
                        newHeuristic: WeightedDirectedGraph<int>.Diagonal,
                        buttonIndex: (int)buttonNames.SelectOctile,
                        newOrdinalDistance: (float)Math.Sqrt(2));
                },
                text: "Octile Heuristic",
                fontColor: Color.White,
                backgroundColor: null);

            buttons[(int)buttonNames.SelectChebyshev] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 25,
                height: 25,
                action: () =>
                {
                    ChangeSelectedHeuristic(
                        newHeuristic: WeightedDirectedGraph<int>.Diagonal,
                        buttonIndex: (int)buttonNames.SelectChebyshev,
                        newOrdinalDistance: 1f);
                },
                text: "Chebyshev Heuristic",
                fontColor: Color.White,
                backgroundColor: null);

            buttons[(int)buttonNames.SelectEuclidean] = new Button(
                texture: ButtonTexture,
                font: ButtonFont,
                width: 25,
                height: 25,
                action: () =>
                {
                    ChangeSelectedHeuristic(
                        newHeuristic: WeightedDirectedGraph<int>.Euclidean,
                        buttonIndex: (int)buttonNames.SelectEuclidean,
                        newOrdinalDistance: (float)Math.Sqrt(2));
                },
                text: "Euclidean Heuristic",
                fontColor: Color.White,
                backgroundColor: null);

            int buttonX = graphicsDevice.Viewport.Height + 50;
            int buttonGroupYOffsets = 10;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i == 1 || i == 3 || i == 4)
                {
                    buttonGroupYOffsets += 15;
                }
                if (i > 5)
                {
                    buttonGroupYOffsets -= 25;
                }
                buttons[i].Position = new Vector2(buttonX, buttonGroupYOffsets + buttons[0].Height * 1.15f * i);
            }
            ButtonInfoPos = new Vector2(buttonX, buttonGroupYOffsets + buttons[0].Height * 1.15f * buttons.Length);
            #endregion SettingButtonProperties

            visualizationProgressionIndex = 0;
            stopwatch = new Stopwatch();

            tiles[0, 0].TileType = TileTypes.Start;
            tiles[GraphSize - 1, GraphSize - 1].TileType = TileTypes.End;
            startVertex = vertexMap[new Point(0, 0)];
            endVertex = vertexMap[new Point(GraphSize - 1, GraphSize - 1)];
        }

        public void Update(MouseState mouseState, MouseState prevMouseState, KeyboardState keyboardState, Viewport viewport)
        {
            int tileWidth = TileSize + TileBorderSize;

            var mousePos = Vector2.Transform(mouseState.Position.ToVector2(), Camera.GetInverseViewMatrix()).ToPoint();
            mousePos.X /= tileWidth;
            mousePos.Y /= tileWidth;

            var prevMousePos = Vector2.Transform(prevMouseState.Position.ToVector2(), Camera.GetInverseViewMatrix()).ToPoint();
            prevMousePos.X /= tileWidth;
            prevMousePos.Y /= tileWidth;

            if (mouseState.ScrollWheelValue > prevMouseState.ScrollWheelValue)
            {
                Camera.ZoomIn(1);
            }
            else if (mouseState.ScrollWheelValue < prevMouseState.ScrollWheelValue)
            {
                Camera.ZoomOut(1);
            }

            foreach (var button in buttons)
            {
                button.Update(mouseState, prevMouseState);
            }

            UpdateTiles(mouseState, prevMouseState, mousePos, prevMousePos, viewport);

            if (stopwatch.ElapsedMilliseconds >= 10 && visualizationProgressionIndex < visitedNodes.Count)
            {
                visualizationProgressionIndex++;
                stopwatch.Restart();
            }

            UpdateCameraPosition(keyboardState);
        }
        public void Draw(Viewport viewport, SpriteBatch spriteBatch, Point mousePos)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Camera.GetViewMatrix());

            DrawGraph(spriteBatch, mousePos);

            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawButtons(spriteBatch, viewport);

            spriteBatch.End();
        }

        private void DrawGraph(SpriteBatch spriteBatch, Point mousePos)
        {
            int tileWidth = TileSize + TileBorderSize;

            Vector2 tempPos = Vector2.Transform(mousePos.ToVector2(), Camera.GetInverseViewMatrix());
            mousePos = (tempPos / tileWidth).ToPoint();

            // set tile type to visited
            for (int i = 0; i < visualizationProgressionIndex; i++)
            {
                var tile = tiles[visitedNodes[i].Y, visitedNodes[i].X];
                if (!tile.TileType.HasFlag(TileTypes.Start) && !tile.TileType.HasFlag(TileTypes.End))
                {
                    tile.TileType |= TileTypes.VisitedSpace;
                }
            }

            // draw tiles
            foreach (var tile in tiles)
            {
                tile.Draw(spriteBatch);
            }

            // draw tile when dragging start or end
            if (specialTileType != SpecialTileTypes.None && mousePos.X < GraphSize && mousePos.X >= 0 && mousePos.Y < GraphSize && mousePos.Y >= 0)
            {
                var tile = tiles[mousePos.Y, mousePos.X];

                spriteBatch.Draw(
                    TileTexture,
                    new Rectangle(
                        tile.X * tileWidth,
                        tile.Y * tileWidth,
                        tile.Size,
                        tile.Size),
                    specialTileType == SpecialTileTypes.Start ? Color.Green : Color.Red);
            }

            // draw line from start to end
            if (visualizationProgressionIndex >= visitedNodes.Count - 1)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var center = new Vector2(path[i].X * tileWidth + TileSize / 2, path[i].Y * tileWidth + TileSize / 2);
                    var nextCenter = new Vector2(path[i + 1].X * tileWidth + TileSize / 2, path[i + 1].Y * tileWidth + TileSize / 2);

                    spriteBatch.DrawLine(center, nextCenter, Color.Black, 2f);
                }
            }
        }
        public void DrawButtons(SpriteBatch spriteBatch, Viewport viewport)
        {
            spriteBatch.Draw(TileTexture, new Rectangle(viewport.Height, 0, viewport.Height, viewport.Height), Color.Black);

            var pathLength = 0;
            foreach (var vertex in path)
            {
                if (vertex == startVertex || vertex == endVertex) continue;

                if (vertex.Type == Vertex.Types.Sand)
                {
                    pathLength++;
                }
                pathLength++;
            }
            spriteBatch.DrawString(ButtonFont, $"WASD or Arrow Keys to pan, space to reset camera\n\nClick + drag on empty spaces to place obstacles,\nClick + drag on obstacles to remove\n\nVisited Tiles: {visualizationProgressionIndex}/{visitedNodes.Count}\nPath Length: {pathLength}", ButtonInfoPos, Color.White);

            foreach (var button in buttons)
            {
                button.Draw(spriteBatch);
            }
        }

        private void GenMaze()
        {
            stopwatch.Restart();
            stopwatch.Stop();
            vertexMap = new Dictionary<Point, Vertex>();
            vertexGraph = new WeightedDirectedGraph<Tile>(SandTileWeight);

            for (int y = 0; y < GraphSize; y++)
            {
                for (int x = 0; x < GraphSize; x++)
                {
                    var point = new Point(x, y);
                    var vertex = new Vertex(x, y, Vertex.Types.Space);

                    vertexMap.Add(point, vertex);
                    vertexGraph.AddVertex(vertexMap[point]);

                    tiles[y, x].BorderColors[(int)Tile.Borders.Bottom] = Color.Black;
                    tiles[y, x].BorderColors[(int)Tile.Borders.Right] = Color.Black;
                }
            }
            var start = new Point(startVertex.X, startVertex.Y);
            var end = new Point(endVertex.X, endVertex.Y);

            tiles[start.Y, start.X].TileType = TileTypes.Start;
            tiles[end.Y, end.X].TileType = TileTypes.End;
            startVertex = vertexMap[start];
            endVertex = vertexMap[end];


            var unionFind = new QuickFind<Vertex>(vertexGraph.Vertices);
            var rng = new Random();

            Vertex p;
            Vertex q;

            var pointOffsets = new Point[4]
            {
                new Point(0, -1),
                new Point(-1, 0),
                new Point(0, 1),
                new Point(1, 0)
            };

            while (unionFind.SetCount > 1)
            {
                do
                {
                    var pPoint = new Point(rng.Next(0, GraphSize), rng.Next(0, GraphSize));
                    p = vertexMap[pPoint];

                    Point pointOffset;
                    do
                    {
                        pointOffset = pointOffsets[rng.Next(0, pointOffsets.Length)];
                    } while (!vertexMap.ContainsKey(pPoint + pointOffset));


                    q = vertexMap[pPoint + pointOffset];
                }
                while (!unionFind.Union(p, q));
                vertexGraph.AddEdge(p, q, distance: 1);
                vertexGraph.AddEdge(q, p, distance: 1);

                var isHorizontallyConnected = p.Y == q.Y;

                Vertex topLeftVertex = p;
                if (isHorizontallyConnected)
                {
                    topLeftVertex = p.X < q.X ? p : q;
                }
                else
                {
                    topLeftVertex = p.Y < q.Y ? p : q;
                }

                tiles[topLeftVertex.Y, topLeftVertex.X].BorderColors[isHorizontallyConnected ? (int)Tile.Borders.Right : (int)Tile.Borders.Bottom] = Color.LightGray;
            }
        }
        private void ChangeSelectedHeuristic(
            Func<Vertex, Vertex, float, float, float> newHeuristic,
            int buttonIndex,
            float newOrdinalDistance)
        {
            stopwatch.Restart();
            stopwatch.Stop();
            if (heuristic == WeightedDirectedGraph<int>.Manhattan)
            {
                buttons[(int)buttonNames.SelectManhattan].Text = "Manhattan Heuristic";
                buttons[(int)buttonNames.SelectManhattan].BackgroundColor = Color.White;
            }
            else if (heuristic == WeightedDirectedGraph<int>.Euclidean)
            {
                buttons[(int)buttonNames.SelectEuclidean].Text = "Euclidean Heuristic";
                buttons[(int)buttonNames.SelectEuclidean].BackgroundColor = Color.White;
            }
            else if (heuristic == WeightedDirectedGraph<int>.Diagonal && OrdinalDistance == 1)
            {
                buttons[(int)buttonNames.SelectChebyshev].Text = "Chebyshev Heuristic";
                buttons[(int)buttonNames.SelectChebyshev].BackgroundColor = Color.White;
            }
            else if (heuristic == WeightedDirectedGraph<int>.Diagonal)
            {
                buttons[(int)buttonNames.SelectOctile].Text = "Octile Heuristic";
                buttons[(int)buttonNames.SelectOctile].BackgroundColor = Color.White;
            }

            heuristic = newHeuristic;
            OrdinalDistance = newOrdinalDistance;
            string heuristicName = heuristic.Method.Name;

            if (heuristic == WeightedDirectedGraph<int>.Diagonal && OrdinalDistance == 1)
            {
                heuristicName = "Chebyshev";
            }
            else if (heuristic == WeightedDirectedGraph<int>.Diagonal)
            {
                heuristicName = "Octile";
            }

            buttons[buttonIndex].Text = $"{heuristicName} Heuristic (Selected)";
            buttons[buttonIndex].BackgroundColor = Color.LightSkyBlue;
        }
        private void RunPathfinding(pathfinderNames pathfinderName)
        {
            foreach (var vertex in visitedNodes)
            {
                Tile tile = tiles[vertex.Y, vertex.X];
                var tileType = tile.TileType;

                if (tileType.HasFlag(TileTypes.Start) ||
                    tileType.HasFlag(TileTypes.End) ||
                   !tileType.HasFlag(TileTypes.VisitedSpace)) continue;

                tile.TileType ^= TileTypes.VisitedSpace;
            }

            switch (pathfinderName)
            {
                case pathfinderNames.Djikstra:
                    vertexGraph.DijkstraPathfinder(startVertex, endVertex, out path, out visitedNodes);
                    break;

                case pathfinderNames.AStar:
                    vertexGraph.AStarPathfinder(
                        startVertex,
                        endVertex,
                        CardinalDistance,
                        OrdinalDistance,
                        out path,
                        out visitedNodes,
                        heuristic);
                    break;
            }

            visualizationProgressionIndex = 0;
            stopwatch.Restart();
        }
        private void UpdateTiles(MouseState mouseState, MouseState prevMouseState, Point mousePos, Point prevMousePos, Viewport viewport)
        {
            if (mousePos.X < 0
            || mousePos.Y < 0
            || prevMousePos.X < 0
            || prevMousePos.Y < 0
            || mousePos.X >= tiles.GetLength(1)
            || mousePos.Y >= tiles.GetLength(0)
            || prevMousePos.X >= tiles.GetLength(1)
            || prevMousePos.Y >= tiles.GetLength(0)
            || mouseState.Position.X >= viewport.Height
            || mouseState.Position.Y >= viewport.Height
            || prevMouseState.Position.X >= viewport.Height
            || prevMouseState.Position.X >= viewport.Height)
            {
                if (specialTileType == SpecialTileTypes.Start || specialTileType == SpecialTileTypes.End)
                {
                    var lastValidVertex = vertexMap[new Point(lastValidTile.X, lastValidTile.Y)];
                    if (specialTileType == SpecialTileTypes.Start)
                    {
                        startVertex = lastValidVertex;
                    }
                    else
                    {
                        endVertex = lastValidVertex;
                    }

                    lastValidTile.TileType = draggingTileType;
                    specialTileType = SpecialTileTypes.None;
                    draggingTileType = TileTypes.Wall;
                }

                return;
            }

            Tile currTile = tiles[mousePos.Y, mousePos.X];
            if (!currTile.TileType.HasFlag(TileTypes.Start) && !currTile.TileType.HasFlag(TileTypes.End))
            {
                lastValidTile = currTile;
            }

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                PlaceTiles(currTile);
                return;
            }

            UpdateDraggingTileType(currTile, prevMouseState);
        }
        private void PlaceTiles(Tile currTile)
        {
            // Start or End
            if (draggingTileType == TileTypes.Start || draggingTileType == TileTypes.End)
            {
                stopwatch.Restart();
                stopwatch.Stop();
                if (currTile.TileType.HasFlag(draggingTileType))
                {
                    currTile.TileType ^= draggingTileType;
                }
            }
        }
        private void UpdateDraggingTileType(Tile currTile, MouseState prevMouseState)
        {
            // place a start/end tile when released
            if (specialTileType != SpecialTileTypes.None && prevMouseState.LeftButton == ButtonState.Pressed)
            {
                if (specialTileType == SpecialTileTypes.Start)
                {
                    if (currTile.TileType == TileTypes.End)
                    {
                        lastValidTile.TileType = TileTypes.Start;
                    }
                    else
                    {
                        currTile.TileType = TileTypes.Start;
                        startVertex = vertexMap[new Point(currTile.X, currTile.Y)];
                    }
                }
                else
                {
                    if (currTile.TileType == TileTypes.Start)
                    {
                        lastValidTile.TileType = TileTypes.End;
                    }
                    else
                    {
                        currTile.TileType = TileTypes.End;
                        endVertex = vertexMap[new Point(currTile.X, currTile.Y)];
                    }
                }
            }

            specialTileType = SpecialTileTypes.None;
            if (currTile.TileType.HasFlag(TileTypes.Start))
            {
                lastValidTile = currTile;
                specialTileType = SpecialTileTypes.Start;
                draggingTileType = TileTypes.Start;
            }
            else if (currTile.TileType.HasFlag(TileTypes.End))
            {
                lastValidTile = currTile;
                specialTileType = SpecialTileTypes.End;
                draggingTileType = TileTypes.End;
            }
            else if (
                !currTile.TileType.HasFlag(TileTypes.Start) &&
                !currTile.TileType.HasFlag(TileTypes.End))
            {
                draggingTileType = selectedTileType;
            }
        }
        private void UpdateCameraPosition(KeyboardState keyboardState)
        {
            if (keyboardState.IsKeyDown(Keys.Space))
            {
                Camera.LookAt(Camera.Origin);
                Camera.Zoom = Camera.MinimumZoom;
            }

            var speedMultiplier = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift) ? 6 : 3;

            var tileWidth = TileSize + TileBorderSize;
            var bounds = new Rectangle(Vector2.Transform(Vector2.Zero, Camera.GetInverseViewMatrix()).ToPoint(),
                                       new Point(tileWidth * GraphSize / (int)Camera.Zoom, tileWidth * GraphSize / (int)Camera.Zoom));

            if (bounds.X < 0)
            {
                Camera.Move(new Vector2(0 - bounds.X, 0));
            }
            else if (bounds.X + bounds.Width > tileWidth * GraphSize)
            {
                Camera.Move(new Vector2(tileWidth * GraphSize - (bounds.X + bounds.Width), 0));
            }
            if (bounds.Y < 0)
            {
                Camera.Move(new Vector2(0, 0 - bounds.Y));
            }
            else if (bounds.Y + bounds.Height > tileWidth * GraphSize)
            {
                Camera.Move(new Vector2(0, tileWidth * GraphSize - (bounds.Y + bounds.Height)));
            }

            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            {
                Camera.Move(-Vector2.UnitY * speedMultiplier);
            }
            if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            {
                Camera.Move(Vector2.UnitY * speedMultiplier);
            }
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            {
                Camera.Move(-Vector2.UnitX * speedMultiplier);
            }
            if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            {
                Camera.Move(Vector2.UnitX * speedMultiplier);
            }
        }
    }
}
