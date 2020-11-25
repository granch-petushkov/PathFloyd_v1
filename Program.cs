using Npgsql;
using System;
using System.Collections.Generic;

namespace PathFloyd
{
    class Program
    {
        //Вывод на консоль данных из списка 
        private static void ShowData(IList<object[]> data, string? notanotation = null)
        {
            Console.WriteLine(notanotation);
            foreach (var array in data)
            {
                foreach (var item in array)
                    Console.Write(item.ToString() + "\t");
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        //Вывод на консоль данных из матрици смежности
        private static void ShowData(double[,] data, string? notanotation = null)
        {
            Console.WriteLine(notanotation);
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (data[i, j] != 0 && data[i, j] != double.PositiveInfinity)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write(data[i, j] + "\t");
                        Console.ResetColor();
                    }
                    else Console.Write(data[i, j] + "\t");
                }
                Console.WriteLine();
            }
        }
        //Вывод на консоль данных из матрици узлов кратчайших растояний
        private static void ShowData(int[,] data, string? notanotation = null)
        {
            Console.WriteLine(notanotation);
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (data[i, j] != 0 )
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write(data[i, j] + "\t");
                        Console.ResetColor();
                    }
                    else Console.Write(data[i, j] + "\t");
                }
                Console.WriteLine();
            }
        }
        //Получение данных об размерности матрицы смежности
        private static int GetAdjacencyMatrixSize(IList<object[]> data)
        {
            int matrixSize = 0;
            for (int i = 0; i < data[i].Length; i += 4)
            {
                for (int j = 0; j < data.Count - 1; j++)
                {
                    if ((int)data[j].GetValue(i) < (int)data[j + 1].GetValue(i))
                        matrixSize = (int)data[j + 1].GetValue(i);
                    else matrixSize = (int)data[j].GetValue(i);
                }
            }
            return matrixSize;
        }
        //Получение длинны ребра: по хорошему создать структуру и сократить параметры метода до 2 или 3 - если вынести величину округления в параметры
        private static double GetDistance(double xPos1, double yPos1, double zPos1, double xPos2, double yPos2, double zPos2)
        {
            return Math.Round(Math.Sqrt(Math.Pow(xPos2 - xPos1, 2) + Math.Pow(yPos2 - yPos1, 2) + Math.Pow(zPos2 - zPos1, 2)), 2);
        }
        //Инициализация массива смежности
        private static void InitializAdjacencyMatrix(double[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if (i == j)
                        matrix[i, j] = 0;
                    else matrix[i, j] = double.PositiveInfinity;
                    //Console.Write(matrix[i, j] + "\t");
                }
                //Console.WriteLine();
            }
        }

        //Непосредственно алгоритм Флойда-Уолшера - округление тоже можно вынисти в параметры метода
        private static void StartFloydAlg(double[,] adjacencyMatrix, int[,]? mapMatrix = null) 
        {
            for (int k = 0; k < 10; k++)
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if (adjacencyMatrix[i, j] > adjacencyMatrix[i, k] + adjacencyMatrix[k, j])
                        {
                            adjacencyMatrix[i, j] = Math.Round(adjacencyMatrix[i, k] + adjacencyMatrix[k, j], 2);
                            if (mapMatrix != null)
                                mapMatrix[i, j] = k + 1;
                        }
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            string host = "Host=localhost";
            //string server = "Server=127.0.0.1";
            //string port = "Port=5432";
            string user = "Username=postgres";
            string password = "Password=Cascade";
            string database = "Database=graphstorage";

            //объявление списка в котором будут хранится данные из БД
            IList<object[]> graphData;

            // объявление матрицы смежности
            double[,] adjacencyMatrix;

            //объявление матрицы c указанием узлов для кратчайших путей
            int[,] mapMatrix;

            //var connectionStr = String.Join(";", new[] { server, port, user, password, database});
            var connectionStr = String.Join(";", new[] { host, user, password, database });

            var sqlQuery = @"select graph_edge.vertexstartid, graph_vertex.x, graph_vertex.y, graph_vertex.z, graph_edge.vertexendid,
                       (select graph_vertex.x
                         from graph_vertex
                         where graph_vertex.vertexid = graph_edge.vertexendid),
                       (select graph_vertex.y
                         from graph_vertex
                         where graph_vertex.vertexid = graph_edge.vertexendid),
                       (select graph_vertex.Z
                         from graph_vertex
                         where graph_vertex.vertexid = graph_edge.vertexendid)
                                from graph_edge, graph_vertex
                                where graph_vertex.vertexid = graph_edge.vertexstartid;";

            using (var connection = new NpgsqlConnection(connectionStr))
            {
                connection.Open();

                using var sqlCommand = new NpgsqlCommand(sqlQuery, connection);
                var reader = sqlCommand.ExecuteReader();
                graphData = new List<object[]>();

                //Заполняем список данными о узлах и их координатах у графа
                while (reader.Read())
                {
                    object[] column = new object[reader.FieldCount];
                    reader.GetValues(column);
                    graphData.Add(column);
                }

            }
            //Проверим, что запрос выполнился и данные считались с бд
            ShowData(graphData, "Список ребер с координатами вершин");

            int size = GetAdjacencyMatrixSize(graphData);

            //Инициализация матрицы смежности и матрицы адресов
            adjacencyMatrix = new double[size, size];
            mapMatrix = new int[size, size];           
            InitializAdjacencyMatrix(adjacencyMatrix);

            //Проведем начальное заполнение матрицы смежности весами ребер
            for (int n = 0; n < graphData.Count; n++)
            {
                int i = (int)graphData[n].GetValue(0) - 1;
                int j = (int)graphData[n].GetValue(4) - 1;
                double x1 = (float)graphData[n].GetValue(1);
                double y1 = (float)graphData[n].GetValue(2);
                double z1 = (float)graphData[n].GetValue(3);
                double x2 = (float)graphData[n].GetValue(5);
                double y2 = (float)graphData[n].GetValue(6);
                double z2 = (float)graphData[n].GetValue(7);
                adjacencyMatrix[i, j] = GetDistance(x1, y1, z1, x2, y2, z2);
                adjacencyMatrix[j, i] = GetDistance(x1, y1, z1, x2, y2, z2);
            }
            ShowData(adjacencyMatrix, "Начальное заполнение матрицы смежности V(ij0)");
            StartFloydAlg(adjacencyMatrix, mapMatrix);

            ShowData(adjacencyMatrix, "Матрица смежности с кратчайшими путями");
            Console.WriteLine();
            ShowData(mapMatrix, "Матрица узлов кратчайших растояний");

            Console.ReadKey();
        }
    }
}
