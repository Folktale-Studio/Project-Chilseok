using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Chilseok.Puzzle
{
    public static class ZoneParser
    {
        private const int MaximumAxisLength = 8;

        public static ZoneData Parse(string text)
        {
            var lines = ReadLines(text, out var endLine);
            var headers = new Dictionary<string, SourceLine>(StringComparer.Ordinal);
            var featherHeaders = new List<SourceLine>();
            var index = 0;
            while (index < lines.Count && !lines[index].Text.StartsWith("y=", StringComparison.Ordinal))
            {
                ReadHeader(lines[index++], headers, featherHeaders);
            }

            var gridStartLine = index < lines.Count ? lines[index].Number : endLine;
            var name = RequiredHeader(headers, "name", gridStartLine).Text;
            var teaches = RequiredHeader(headers, "teaches", gridStartLine).Text;
            var intent = RequiredHeader(headers, "intent", gridStartLine).Text;
            var size = ParseSize(RequiredHeader(headers, "size", gridStartLine));
            var rotations = ParseRotations(RequiredHeader(headers, "rotations", gridStartLine));
            var startView = RequiredHeader(headers, "start-view", gridStartLine);
            var direction = ParseView(startView.Text, startView.Number);

            var grid = ReadGrid(lines, index, endLine, size);
            var feathers = ReadFeathers(featherHeaders, grid.FeatherPositions, size);
            return new ZoneData(name, teaches, intent, size, rotations.Minimum, rotations.Maximum,
                direction, grid.Blocks, grid.Start, grid.Magpie, feathers);
        }

        private static List<SourceLine> ReadLines(string text, out int endLine)
        {
            using var reader = new StringReader(text ?? throw new ArgumentNullException(nameof(text)));
            var lines = new List<SourceLine>();
            var lineNumber = 0;
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                lineNumber++;
                var content = lineNumber == 1 ? line.TrimStart('\uFEFF').Trim() : line.Trim();
                if (content.Length != 0)
                {
                    lines.Add(new SourceLine(lineNumber, content));
                }
            }

            endLine = lineNumber + 1;
            return lines;
        }

        private static void ReadHeader(SourceLine line, Dictionary<string, SourceLine> headers,
            List<SourceLine> featherHeaders)
        {
            if (line.Text[0] == '#')
            {
                return;
            }

            var separator = line.Text.IndexOf(':');
            if (separator < 1)
            {
                throw new ZoneParseException(line.Number, "헤더는 이름: 값 형식이어야 합니다.");
            }

            var key = line.Text.Substring(0, separator).TrimEnd();
            var value = new SourceLine(line.Number, line.Text.Substring(separator + 1).Trim());
            if (value.Text.Length == 0)
            {
                throw new ZoneParseException(line.Number, $"{key} 헤더에 값이 필요합니다.");
            }

            switch (key)
            {
                case "name":
                case "teaches":
                case "size":
                case "rotations":
                case "start-view":
                case "intent":
                    if (!headers.TryAdd(key, value))
                    {
                        throw new ZoneParseException(line.Number, $"{key} 헤더가 중복되었습니다.");
                    }

                    break;
                case "feather-views":
                    featherHeaders.Add(value);
                    break;
                default:
                    throw new ZoneParseException(line.Number, $"알 수 없는 헤더입니다: {key}");
            }
        }

        private static SourceLine RequiredHeader(Dictionary<string, SourceLine> headers, string key, int lineNumber)
        {
            if (headers.TryGetValue(key, out var header))
            {
                return header;
            }

            throw new ZoneParseException(lineNumber, $"필수 헤더가 없습니다: {key}");
        }

        private static Vector3Int ParseSize(SourceLine line)
        {
            if (TryParseCoordinate(line.Text, out var size) &&
                size.x >= 1 && size.x <= MaximumAxisLength &&
                size.y >= 1 && size.y <= MaximumAxisLength &&
                size.z >= 1 && size.z <= MaximumAxisLength)
            {
                return size;
            }

            throw new ZoneParseException(line.Number, $"격자 크기는 각 축이 1~{MaximumAxisLength}인 x y z 형식이어야 합니다.");
        }

        private static (int Minimum, int Maximum) ParseRotations(SourceLine line)
        {
            var values = line.Text.Split('-');
            if (values.Length == 2 && TryParseInteger(values[0], out var minimum) &&
                TryParseInteger(values[1], out var maximum) && minimum >= 0 && maximum >= minimum)
            {
                return (minimum, maximum);
            }

            throw new ZoneParseException(line.Number, "rotations는 0 이상의 최소-최대 형식이어야 합니다.");
        }

        private static (bool[,,] Blocks, Vector3Int Start, Vector3Int Magpie, List<Vector3Int> FeatherPositions)
            ReadGrid(List<SourceLine> lines, int index, int endLine, Vector3Int size)
        {
            var blocks = new bool[size.x, size.y, size.z];
            var featherPositions = new List<Vector3Int>();
            Vector3Int? start = null;
            Vector3Int? magpie = null;
            var startLine = 0;

            for (var y = 0; y < size.y; y++)
            {
                var expectedLayer = FormattableString.Invariant($"y={y}");
                var layer = ReadGridLine(lines, ref index, endLine, expectedLayer);
                if (layer.Text != expectedLayer)
                {
                    throw new ZoneParseException(layer.Number, $"{expectedLayer} 층이 필요합니다.");
                }

                for (var z = 0; z < size.z; z++)
                {
                    var expectedRow = FormattableString.Invariant($"z{z}");
                    var line = ReadGridLine(lines, ref index, endLine, expectedRow);
                    var row = ParseRow(line, expectedRow, size.x);
                    for (var x = 0; x < row.Length; x++)
                    {
                        var position = new Vector3Int(x, y, z);
                        switch (row[x])
                        {
                            case '#':
                                blocks[x, y, z] = true;
                                break;
                            case '.':
                                break;
                            case 'S':
                                SetMarker(ref start, position, line.Number, 'S');
                                startLine = line.Number;
                                break;
                            case 'M':
                                SetMarker(ref magpie, position, line.Number, 'M');
                                break;
                            case 'F':
                                featherPositions.Add(position);
                                break;
                            default:
                                throw new ZoneParseException(line.Number, $"알 수 없는 격자 문자입니다: '{row[x]}' (x={x})");
                        }
                    }
                }
            }

            if (index < lines.Count)
            {
                throw new ZoneParseException(lines[index].Number, "격자 크기를 넘는 행 또는 층이 있습니다.");
            }

            if (!start.HasValue || !magpie.HasValue)
            {
                throw new ZoneParseException(endLine, "시작점 S와 까치 M을 각각 한 칸씩 지정해야 합니다.");
            }

            var startPosition = start.Value;
            if (startPosition.y == 0 || !blocks[startPosition.x, startPosition.y - 1, startPosition.z])
            {
                throw new ZoneParseException(startLine, "시작점 S 바로 아래에 별 블록이 필요합니다.");
            }

            return (blocks, startPosition, magpie.Value, featherPositions);
        }

        private static SourceLine ReadGridLine(List<SourceLine> lines, ref int index, int endLine, string expected)
        {
            if (index < lines.Count)
            {
                return lines[index++];
            }

            throw new ZoneParseException(endLine, $"{expected} 전에 격자가 끝났습니다.");
        }

        private static string ParseRow(SourceLine line, string expectedRow, int width)
        {
            var separator = line.Text.IndexOf(':');
            if (separator < 1 || line.Text.Substring(0, separator).TrimEnd() != expectedRow)
            {
                throw new ZoneParseException(line.Number, $"{expectedRow} 행이 필요합니다.");
            }

            var row = line.Text.Substring(separator + 1).Trim();
            if (row.Length != width)
            {
                throw new ZoneParseException(line.Number, $"{expectedRow} 행 길이는 {width}여야 합니다. 입력 길이: {row.Length}");
            }

            return row;
        }

        private static void SetMarker(ref Vector3Int? marker, Vector3Int position, int lineNumber, char symbol)
        {
            if (marker.HasValue)
            {
                throw new ZoneParseException(lineNumber, $"{symbol} 표시가 중복되었습니다.");
            }

            marker = position;
        }

        private static ZoneFeather[] ReadFeathers(List<SourceLine> headers, List<Vector3Int> positions, Vector3Int size)
        {
            var viewsByPosition = new Dictionary<Vector3Int, int>();
            foreach (var header in headers)
            {
                var parts = header.Text.Split('=');
                if (parts.Length != 2 || !TryParseCoordinate(parts[0], out var position))
                {
                    throw new ZoneParseException(header.Number, "feather-views는 x y z = 시점,시점 형식이어야 합니다.");
                }

                if (!ZoneData.IsInside(position, size))
                {
                    throw new ZoneParseException(header.Number, $"feather-views 좌표가 격자 밖입니다: {position}");
                }

                if (!positions.Contains(position))
                {
                    throw new ZoneParseException(header.Number, $"feather-views 좌표에 F가 없습니다: {position}");
                }

                var activeViews = ParseFeatherViews(parts[1], header.Number);
                if (!viewsByPosition.TryAdd(position, activeViews))
                {
                    throw new ZoneParseException(header.Number, $"feather-views 좌표가 중복되었습니다: {position}");
                }
            }

            var feathers = new ZoneFeather[positions.Count];
            for (var i = 0; i < feathers.Length; i++)
            {
                var position = positions[i];
                var activeViews = viewsByPosition.TryGetValue(position, out var mask) ? mask : ViewDirectionMask.All;
                feathers[i] = new ZoneFeather(position, activeViews);
            }

            return feathers;
        }

        private static int ParseFeatherViews(string text, int lineNumber)
        {
            var views = 0;
            foreach (var token in text.Split(','))
            {
                var mask = ViewDirectionMask.For(ParseView(token, lineNumber));
                if ((views & mask) != 0)
                {
                    throw new ZoneParseException(lineNumber, $"feather-views 시점이 중복되었습니다: {token.Trim()}");
                }

                views |= mask;
            }

            return views;
        }

        private static ViewDirection ParseView(string text, int lineNumber)
        {
            if (TryParseInteger(text, out var degrees) && ViewDirectionMask.For((ViewDirection)degrees) != 0)
            {
                return (ViewDirection)degrees;
            }

            throw new ZoneParseException(lineNumber, "시점은 0, 90, 180, 270 중 하나여야 합니다.");
        }

        private static bool TryParseCoordinate(string text, out Vector3Int position)
        {
            var coordinates = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (coordinates.Length == 3 && TryParseInteger(coordinates[0], out var x) &&
                TryParseInteger(coordinates[1], out var y) && TryParseInteger(coordinates[2], out var z))
            {
                position = new Vector3Int(x, y, z);
                return true;
            }

            position = default;
            return false;
        }

        private static bool TryParseInteger(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        private readonly struct SourceLine
        {
            public int Number { get; }
            public string Text { get; }

            public SourceLine(int number, string text)
            {
                Number = number;
                Text = text;
            }
        }
    }

    public sealed class ZoneParseException : FormatException
    {
        public int LineNumber { get; }

        internal ZoneParseException(int lineNumber, string reason) : base($"{lineNumber}줄: {reason}")
        {
            LineNumber = lineNumber;
        }
    }
}
