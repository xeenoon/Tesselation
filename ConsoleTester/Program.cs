using System;
using System.Collections.Generic;
using System.Drawing;

class Program
{
    static void Main()
    {
        const int width = 5;
        const int height = 5;

        int[,] board = new int[width, height]
        {
            {1, 0, 0, 1, 1},
            {1, 0, 0, 1, 1},
            {1, 1, 0, 0, 1},
            {1, 1, 0, 0, 0},
            {0, 0, 1, 1, 1}
        };

        List<Point> emptyArea = FindEmptyArea(board, width, height);

        Console.WriteLine("Empty Areas:");

        foreach (var coordinate in emptyArea)
        {
            Console.WriteLine($"({coordinate.X}, {coordinate.Y})");
        }
    }

    static List<Point> FindEmptyArea(int[,] board, int width, int height)
    {
        List<List<Point>> emptyAreas = new List<List<Point>>();

        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == 0 && !visited[x, y])
                {
                    List<Point> emptyArea = new List<Point>();
                    DFS(board, x, y, width, height, visited, emptyArea);
                    emptyAreas.Add(emptyArea);
                }
            }
        }

        // Filter out only the smallest areas
        int minAreaSize = int.MaxValue;
        List<Point> smallestArea = new List<Point>();

        foreach (var area in emptyAreas)
        {
            if (area.Count < minAreaSize)
            {
                smallestArea.Clear();
                smallestArea = area;
                minAreaSize = area.Count;
            }
            else if (area.Count == minAreaSize)
            {
                smallestArea = area;
            }
        }

        return smallestArea;
    }

    static void DFS(int[,] board, int x, int y, int width, int height, bool[,] visited, List<Point> emptyArea)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || board[x, y] != 0)
        {
            return;
        }

        visited[x, y] = true;
        emptyArea.Add(new Point(x, y));

        DFS(board, x + 1, y, width, height, visited, emptyArea);
        DFS(board, x - 1, y, width, height, visited, emptyArea);
        DFS(board, x, y + 1, width, height, visited, emptyArea);
        DFS(board, x, y - 1, width, height, visited, emptyArea);
    }
}
