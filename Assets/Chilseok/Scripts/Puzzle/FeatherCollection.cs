using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chilseok.Puzzle
{
    public sealed class FeatherCollection
    {
        private const int MaximumFeathers = 5;
        private readonly IReadOnlyList<ZoneFeather> _feathers;

        public int FullMask { get; }

        public FeatherCollection(ZoneData zone)
        {
            _feathers = zone?.Feathers ?? throw new ArgumentNullException(nameof(zone));
            if (_feathers.Count > MaximumFeathers)
            {
                throw new ArgumentException($"구간 깃털은 최대 {MaximumFeathers}개입니다.", nameof(zone));
            }

            FullMask = (1 << _feathers.Count) - 1;
        }

        public int Collect(int mask, Vector3Int feet, ViewDirection view)
        {
            for (var index = 0; index < _feathers.Count; index++)
            {
                var feather = _feathers[index];
                if (feet == feather.Position && feather.IsActive(view))
                {
                    return mask | (1 << index);
                }
            }

            return mask;
        }
    }
}
