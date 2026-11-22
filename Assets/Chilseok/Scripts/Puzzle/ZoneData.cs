using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chilseok.Puzzle
{
    public sealed class ZoneData
    {
        private readonly bool[,,] _blocks;

        public string Name { get; }
        public string Teaches { get; }
        public string Intent { get; }
        public Vector3Int Size { get; }
        public int MinimumRotations { get; }
        public int MaximumRotations { get; }
        public ViewDirection StartView { get; }
        public Vector3Int Start { get; }
        public Vector3Int Magpie { get; }
        public IReadOnlyList<ZoneFeather> Feathers { get; }

        internal ZoneData(string name, string teaches, string intent, Vector3Int size,
            int minimumRotations, int maximumRotations, ViewDirection startView,
            bool[,,] blocks, Vector3Int start, Vector3Int magpie, ZoneFeather[] feathers)
        {
            Name = name;
            Teaches = teaches;
            Intent = intent;
            Size = size;
            MinimumRotations = minimumRotations;
            MaximumRotations = maximumRotations;
            StartView = startView;
            Start = start;
            Magpie = magpie;
            _blocks = blocks;
            Feathers = Array.AsReadOnly(feathers);
        }

        public bool IsSolid(Vector3Int position) =>
            IsInside(position, Size) && _blocks[position.x, position.y, position.z];

        internal static bool IsInside(Vector3Int position, Vector3Int size) =>
            position.x >= 0 && position.x < size.x &&
            position.y >= 0 && position.y < size.y &&
            position.z >= 0 && position.z < size.z;
    }

    public readonly struct ZoneFeather
    {
        private readonly int _activeViews;

        public Vector3Int Position { get; }

        internal ZoneFeather(Vector3Int position, int activeViews)
        {
            Position = position;
            _activeViews = activeViews;
        }

        public bool IsActive(ViewDirection direction) =>
            (_activeViews & ViewDirectionMask.For(direction)) != 0;
    }
}
