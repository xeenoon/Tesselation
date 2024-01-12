using System;
using System.Collections.Generic;

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
            {1, 1, 1, 1, 1}
        };

        List<List<(int, int)>> emptyAreas = FindEmptyAreas(board, width, height);

        Console.WriteLine("Empty Areas:");

        for (int i = 0; i < emptyAreas.Count; i++)
        {
            Console.WriteLine($"Area {i + 1}:");
            foreach (var coordinate in emptyAreas[i])
            {
                Console.WriteLine($"({coordinate.Item2}, {coordinate.Item1})");
            }
        }
    }

    static List<List<(int, int)>> FindEmptyAreas(int[,] board, int width, int height)
    {
        List<List<(int, int)>> emptyAreas = new List<List<(int, int)>>();

        bool[,] visited = new bool[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (board[i, j] == 0 && !visited[i, j])
                {
                    List<(int, int)> emptyArea = new List<(int, int)>();
                    DFS(board, i, j, width, height, visited, emptyArea);
                    emptyAreas.Add(emptyArea);
                }
            }
        }

        // Filter out only the smallest areas
        int minAreaSize = int.MaxValue;
        List<List<(int, int)>> smallestAreas = new List<List<(int, int)>>();

        foreach (var area in emptyAreas)
        {
            if (area.Count < minAreaSize)
            {
                smallestAreas.Clear();
                smallestAreas.Add(area);
                minAreaSize = area.Count;
            }
            else if (area.Count == minAreaSize)
            {
                smallestAreas.Add(area);
            }
        }

        return smallestAreas;
    }

    static void DFS(int[,] board, int x, int y, int width, int height, bool[,] visited, List<(int, int)> emptyArea)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || board[x, y] != 0)
        {
            return;
        }

        visited[x, y] = true;
        emptyArea.Add((x, y));

        DFS(board, x + 1, y, width, height, visited, emptyArea);
        DFS(board, x - 1, y, width, height, visited, emptyArea);
        DFS(board, x, y + 1, width, height, visited, emptyArea);
        DFS(board, x, y - 1, width, height, visited, emptyArea);
    }
}
