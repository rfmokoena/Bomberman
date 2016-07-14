﻿using BomberBot.Common;
using BomberBot.Domain.Model;
using BomberBot.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BomberBot.Business.Helpers
{
    public class BotHelper
    {
        public static bool IsValidBlock(GameState state, Location loc)
        {
            return (loc.X > 0 && loc.X < state.MapWidth - 1)
                && (loc.Y > 0 && loc.Y < state.MapHeight - 1);
        }

        /// <summary>
        /// Possible move Locations
        /// </summary>
        /// <param name="state"></param>
        /// <param name="startLoc"></param>
        /// <param name="curLoc"></param>
        /// <returns>next posssible move Locations</returns>
        public static List<Location> ExpandMoveBlocks(GameState state, Location startLoc, Location curLoc, bool stayClear = false)
        {
            Location loc;
            var movesLoc = new List<Location>();

            if (curLoc.Equals(startLoc))
            {
                List<Bomb> bombs;
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }


                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    bombs = FindBombsInLOS(state, loc);

                    if (bombs == null || stayClear)
                    {
                        movesLoc.Add(loc);
                    }
                }

            }
            else
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockClear(loc))
                {
                    movesLoc.Add(loc);
                }
            }
            return movesLoc;
        }

        /// <summary>
        /// Shortest route to target
        /// </summary>
        /// <param name="startLoc"></param>
        /// <param name="targetLoc"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static MapNode BuildPathToTarget(GameState state, Location startLoc, Location targetLoc, bool stayClear = false)
        {
            var openList = new List<MapNode> { new MapNode { Location = startLoc } };
            var closedList = new List<MapNode>();

            int gCost, hCost, fCost;
            MapNode qMapNode;

            while (openList.Count != 0)
            {
                qMapNode = openList.OrderBy(node => node.FCost).First();

                if (qMapNode.Location.Equals(targetLoc))
                {
                    return qMapNode;
                }

                openList.Remove(qMapNode);
                closedList.Add(qMapNode);


                var childrenLoc = ExpandMoveBlocks(state, startLoc, qMapNode.Location, stayClear);


                foreach (var loc in childrenLoc)
                {
                    gCost = qMapNode.GCost + 1;
                    hCost = Math.Abs(loc.X - targetLoc.X) + Math.Abs(loc.Y - targetLoc.Y);
                    fCost = gCost + hCost;

                    var newChild = new MapNode
                    {
                        Parent = qMapNode,
                        Location = loc,
                        GCost = gCost,
                        HCost = hCost,
                        FCost = fCost
                    };

                    var nodeInOpenList = openList.FirstOrDefault(node => (node.Location.Equals(loc)));

                    if (nodeInOpenList != null && nodeInOpenList.FCost < newChild.FCost)
                        continue;

                    var nodeInClosedList = closedList.FirstOrDefault(node => (node.Location.Equals(loc)));
                    if (nodeInClosedList != null && nodeInClosedList.FCost < newChild.FCost)
                        continue;

                    openList.Add(newChild);
                }
            }
            return null;
        }

        /// <summary>
        /// Reconstruct the path to target
        /// </summary>
        /// <param name="startLoc"></param>
        /// <param name="goalMapNode"></param>
        /// <returns>Next move Location towards target</returns>
        public static Location RecontractPath(Location startLoc, MapNode goalMapNode)
        {
            if (goalMapNode == null) return null;

            if (goalMapNode.Location.Equals(startLoc)) return startLoc;

            var currentMapNode = goalMapNode;
            while (!currentMapNode.Parent.Location.Equals(startLoc))
            {
                currentMapNode = currentMapNode.Parent;
            }
            return new Location(currentMapNode.Location.X, currentMapNode.Location.Y);
        }

        /// <summary>
        /// Find bombs endangering a bot
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns>List of bombs in LOS</returns>
        public static List<Bomb> FindBombsInLOS(GameState state, Location curLoc)
        {
            var bombsInLOS = new List<Bomb>();

            //Sitting on Bomb
            if (state.IsBomb(curLoc))
            {
                var bomb = state.GetBlock(curLoc).Bomb;
                bombsInLOS.Add(bomb);
            }

            //Continue to add others
            var openBlocks = new List<Location> { curLoc };
            Location qLoc;
            List<Location> blocksLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks.First();

                openBlocks.Remove(qLoc);

                blocksLoc = ExpandBombBlocks(state, curLoc, qLoc);

                foreach (var bLoc in blocksLoc)
                {
                    if (state.IsBomb(bLoc))
                    {
                        var bomb = state.GetBlock(bLoc).Bomb;
                        var bombDistance = Math.Abs(curLoc.X - bLoc.X) + Math.Abs(curLoc.Y - bLoc.Y);
                        if (bomb.BombRadius > bombDistance - 1)
                        {
                            bombsInLOS.Add(bomb);
                        }
                    }
                    else
                    {
                        openBlocks.Add(bLoc);
                    }
                }
            }
            return bombsInLOS.Count == 0 ? null : bombsInLOS.OrderBy(b => b.BombTimer).ToList();
        }

        /// <summary>
        /// Expand blocks in the direction of bombs
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <returns></returns>

        private static List<Location> ExpandBombBlocks(GameState state, Location curLoc, Location blockLoc)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                    {
                        blocksLoc.Add(loc);
                    }
                }
                else
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (IsValidBlock(state, loc) && state.IsBlockBombClear(loc))
                    {
                        blocksLoc.Add(loc);
                    }
                }
            }
            return blocksLoc;
        }

        /// <summary>
        /// Expand safe blocks
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Location> ExpandSafeBlocks(GameState state, Location curLoc)
        {
            Location loc;
            var safeBlocks = new List<Location>();

            loc = new Location(curLoc.X, curLoc.Y - 1);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }


            loc = new Location(curLoc.X + 1, curLoc.Y);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }

            loc = new Location(curLoc.X, curLoc.Y + 1);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }

            loc = new Location(curLoc.X - 1, curLoc.Y);

            if (IsValidBlock(state, loc) && state.IsBlockSafe(loc))
            {
                safeBlocks.Add(loc);
            }

            return safeBlocks;
        }

        /// <summary>
        /// Destructible walls in LOS
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <returns></returns>
        public static List<DestructibleWall> FindWallsInLOS(GameState state, Location curLoc, Player player)
        {
            var wallsInLOS = new List<DestructibleWall>();

            var openBlocks = new List<Location> { curLoc };
            Location qLoc;

            while (openBlocks.Count != 0)
            {
                qLoc = openBlocks.First();

                openBlocks.Remove(qLoc);

                var blocksLoc = ExpandWallBlocks(state, curLoc, qLoc, player.BombRadius);

                foreach (var wLoc in blocksLoc)
                {
                    if (state.IsDestructibleWall(wLoc))
                    {
                        var wall = (DestructibleWall)state.GetBlock(wLoc).Entity;
                        wallsInLOS.Add(wall);
                    }
                    else
                    {
                        openBlocks.Add(wLoc);
                    }
                }
            }
            return wallsInLOS.Count == 0 ? null : wallsInLOS;
        }

        /// <summary>
        /// Expand plant blocks
        /// </summary>
        /// <param name="state"></param>
        /// <param name="curLoc"></param>
        /// <param name="blockLoc"></param>
        /// <returns></returns>
        private static List<Location> ExpandWallBlocks(GameState state, Location curLoc, Location blockLoc, int bombRadius)
        {
            var blocksLoc = new List<Location>();
            Location loc;

            if (blockLoc.Equals(curLoc))
            {
                loc = new Location(curLoc.X, curLoc.Y - 1);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }


                loc = new Location(curLoc.X + 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X, curLoc.Y + 1);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }

                loc = new Location(curLoc.X - 1, curLoc.Y);

                if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                {
                    blocksLoc.Add(loc);
                }
            }
            else
            {
                if (blockLoc.X == curLoc.X)
                {
                    loc = new Location(blockLoc.X, blockLoc.Y < curLoc.Y ? blockLoc.Y - 1 : blockLoc.Y + 1);

                    if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.X - loc.X) + Math.Abs(curLoc.Y - loc.Y);
                        if (bombRadius > locDistance - 1)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
                else
                {
                    loc = new Location(blockLoc.X < curLoc.X ? blockLoc.X - 1 : blockLoc.X + 1, blockLoc.Y);

                    if (IsValidBlock(state, loc) && state.IsBlockPlantClear(loc))
                    {
                        var locDistance = Math.Abs(curLoc.X - loc.X) + Math.Abs(curLoc.Y - loc.Y);
                        if (bombRadius > locDistance - 1)
                        {
                            blocksLoc.Add(loc);
                        }
                    }
                }
            }
            return blocksLoc;
        }
    }
}