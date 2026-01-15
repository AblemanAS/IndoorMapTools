/***********************************************************************
Copyright 2026-present Kyuho Son

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
***********************************************************************/

using System;
using System.Collections.Generic;

namespace IndoorMapTools.Algorithm.FGASolver
{
    public class TSPSolver : IFGASolver
    {
        private const int SCORE_SAME_AREA = 1;
        private const int SCORE_NULL_CELL = 999;
        private const int SCORE_DIFF_AREA = 1000;

        // Edge 구조체 (undirected 간선)
        public struct TSPEdge : IComparable<TSPEdge>
        {
            public int U;
            public int V;
            public uint Weight;

            public TSPEdge(int u, int v, uint weight)
            {
                U = u;
                V = v;
                Weight = weight;
            }

            public int CompareTo(TSPEdge other)
                => Weight.CompareTo(other.Weight);
        }

        private List<TSPEdge> GreedyMatching(List<int> oddVertices, uint[,] matrix)
        {
            var result = new List<TSPEdge>();
            var used = new HashSet<int>();

            while(used.Count < oddVertices.Count)
            {
                int bestI = -1, bestJ = -1;
                uint bestCost = uint.MaxValue;

                for(int i = 0; i < oddVertices.Count; i++)
                {
                    if(used.Contains(oddVertices[i])) continue;

                    for(int j = i + 1; j < oddVertices.Count; j++)
                    {
                        if(used.Contains(oddVertices[j])) continue;

                        uint cost = matrix[oddVertices[i], oddVertices[j]];
                        if(cost < bestCost)
                        {
                            bestI = i;
                            bestJ = j;
                            bestCost = cost;
                        }
                    }
                }

                if(bestI == -1 || bestJ == -1)
                    throw new InvalidOperationException("Greedy matching failed: no valid pair found.");

                int u = oddVertices[bestI];
                int v = oddVertices[bestJ];
                result.Add(new TSPEdge(u, v, matrix[u, v]));

                used.Add(u);
                used.Add(v);
            }

            return result;
        }


        // Hierholzer 알고리즘: Eulerian 회로 구하기 (multigraph가 Dictionary<int, List<int>>로 주어짐)
        private List<int> GetEulerianCircuit(Dictionary<int, List<int>> graph, int start)
        {
            var circuit = new List<int>();
            var stack = new Stack<int>();
            stack.Push(start);
            // graph 복제 (간선을 제거하기 위함)
            var g = new Dictionary<int, List<int>>();
            foreach(var kv in graph)
                g[kv.Key] = new List<int>(kv.Value);
            while(stack.Count > 0)
            {
                int v = stack.Peek();
                if(g[v].Count > 0)
                {
                    int u = g[v][0];
                    stack.Push(u);
                    // 양쪽 간선 제거
                    g[v].RemoveAt(0);
                    g[u].Remove(v);
                }
                else circuit.Add(stack.Pop()); 
            }
            circuit.Reverse();
            return circuit;
        }

        // Eulerian 회로에서 shortcut을 적용해 Hamiltonian tour (방문 순서대로 중복 제거)
        private List<int> ShortcutEulerianCircuit(List<int> circuit)
        {
            var visited = new HashSet<int>();
            var tour = new List<int>();
            foreach(int v in circuit)
            {
                if(!visited.Contains(v))
                {
                    visited.Add(v);
                    tour.Add(v);
                }
            }
            // 마지막에 시작점 복귀 추가
            if(tour.Count > 0)
                tour.Add(tour[0]);
            return tour;
        }

        // Prim 알고리즘을 사용해 MST를 구하는 메서드
        private List<TSPEdge> ComputeMST_Prim(uint[,] matrix)
        {
            int n = matrix.GetLength(0);
            bool[] inMST = new bool[n];
            uint[] key = new uint[n];
            int[] parent = new int[n];

            // 초기화: 모든 정점의 key값을 최대값으로 설정하고, parent는 -1로 초기화
            for(int i = 0; i < n; i++)
            {
                key[i] = uint.MaxValue;
                parent[i] = -1;
            }
            // 시작점 0을 선택하여 key 값을 0으로 설정
            key[0] = 0;

            // 모든 정점을 처리 (최대 n번 반복)
            for(int count = 0; count < n; count++)
            {
                // MST에 포함되지 않은 정점 중 key값이 최소인 정점 u 선택
                int u = -1;
                uint minKey = uint.MaxValue;
                for(int v = 0; v < n; v++)
                {
                    if(!inMST[v] && key[v] < minKey)
                    {
                        minKey = key[v];
                        u = v;
                    }
                }

                // 만약 연결되지 않은 경우, u가 -1이면 break
                if(u == -1) break;
                inMST[u] = true;

                // u의 모든 인접 정점 v에 대해 key값 갱신
                for(int v = 0; v < n; v++)
                {
                    if(!inMST[v] && matrix[u, v] < key[v])
                    {
                        key[v] = matrix[u, v];
                        parent[v] = u;
                    }
                }
            }

            // parent 배열을 사용하여 MST 간선 리스트 구성
            List<TSPEdge> mstEdges = new List<TSPEdge>();
            for(int v = 1; v < n; v++)
                if(parent[v] != -1) 
                    mstEdges.Add(new TSPEdge(parent[v], v, matrix[parent[v], v]));

            return mstEdges;
        }

        // Christofides 알고리즘의 전체 구현 (Prim 알고리즘을 사용한 MST 부분 포함)
        // 입력: uint[,] 거리 행렬 (대칭, 완비 그래프; 0-indexed)
        // 반환: Hamiltonian Tour (int 배열)
        public int[] SolveChristofides(uint[,] matrix)
        {
            int n = matrix.GetLength(0);

            // 1. Prim 알고리즘을 이용하여 MST 계산
            List<TSPEdge> mstEdges = ComputeMST_Prim(matrix);

            // 2. MST에서 홀수 차수 정점 추출
            int[] degree = new int[n];
            foreach(var e in mstEdges)
            {
                degree[e.U]++;
                degree[e.V]++;
            }

            List<int> oddVertices = new List<int>();

            for(int i = 0; i < n; i++)
                if(degree[i] % 2 == 1)
                    oddVertices.Add(i);

            // 3. 홀수 정점에 대해 최소 가중치 완전 매칭 (DP로 계산)
            List<TSPEdge> matchingEdges = GreedyMatching(oddVertices, matrix);

            // 4. MST와 matching 간선을 합쳐 Eulerian multigraph 구성 (무방향 그래프)
            var multigraph = new Dictionary<int, List<int>>();
            for(int i = 0; i < n; i++)
                multigraph[i] = new List<int>();

            void AddEdge(int u, int v)
            {
                multigraph[u].Add(v);
                multigraph[v].Add(u);
            }
            foreach(var e in mstEdges) AddEdge(e.U, e.V);
            foreach(var e in matchingEdges) AddEdge(e.U, e.V);

            // 5. Eulerian 회로 계산 (시작 노드는 0)
            List<int> eulerianCircuit = GetEulerianCircuit(multigraph, 0);

            // 6. Eulerian 회로에 shortcut 적용하여 Hamiltonian tour 생성
            List<int> hamiltonianTour = ShortcutEulerianCircuit(eulerianCircuit);

            return hamiltonianTour.ToArray();
        }

        public int[] Solve(int[,] fgaMatrix)
        {
            // TSP를 위한 cost matrix 생성
            int groupCount = fgaMatrix.GetLength(0);
            int floorCount = fgaMatrix.GetLength(1);

            var costMatrix = new uint[groupCount, groupCount];
            for(int g1 = 0; g1 < groupCount; g1++)
            {
                costMatrix[g1, g1] = 0; // 정방향 추가

                for(int g2 = g1 + 1; g2 < groupCount; g2++)
                {
                    uint score = 0;

                    for(int f = 0; f < floorCount; f++)
                    {
                        if(fgaMatrix[g1, f] == 0 || fgaMatrix[g2, f] == 0) score += SCORE_NULL_CELL; // Null cell
                        else if(fgaMatrix[g1, f] == fgaMatrix[g2, f]) score += SCORE_SAME_AREA; // 같은 Area
                        else score += SCORE_DIFF_AREA;
                    }

                    costMatrix[g1, g2] = score; // 정방향 추가
                    costMatrix[g2, g1] = score; // 역방향 추가
                }
            }

            //PrintMatrix(costMatrix); // Cost Matrix 출력

            var order = SolveChristofides(costMatrix); // TSP 해 구하기

            uint maxCost = costMatrix[order[0], order[order.Length - 1]];
            int maxCostIndex = 0;
            for(int groupIndex = 1; groupIndex < order.Length; groupIndex++)
            {
                if(costMatrix[order[0], order[order.Length - 1]] > maxCost)
                {
                    maxCost = costMatrix[order[groupIndex], order[groupIndex - 1]];
                    maxCostIndex = groupIndex;
                }
            }

            var groupOrder = new int[order.Length];
            for(int groupIndex = 1; groupIndex < order.Length; groupIndex++)
                groupOrder[groupIndex] = order[(groupIndex + maxCostIndex) % order.Length];

            //uint[,] testMatrix = GenerateRandomMatrix(24);
            //uint[,] testMatrix = ImportWeightTable("edge_weights.txt");
            /*for(int i = 0; i < testMatrix.GetLength(0); i++)
            {
                for(int j = 0; j < testMatrix.GetLength(1); j++)
                {
                    Console.Write($"{testMatrix[i, j]} ");
                }
                Console.WriteLine();
            }*/
            //ExportEdgeList(testMatrix, "edge_list.txt");
            //ExportWeightTable(testMatrix, "edge_weights.txt");
            /*var order = SolveChristofides(testMatrix); // TSP 해 구하기
            Console.Write($"TSP 해: {order[0] + 1} ");
            uint totalCost = 0;
            for(int i = 1; i < order.Length; i++)
            {
                Console.Write((order[i] + 1) + " ");
                totalCost += testMatrix[order[i], order[i-1]];
            }
            Console.WriteLine($"\nTotal Cost = {totalCost}");

            var answer = new int[] { 1, 11, 4, 8, 24, 3, 17, 20, 22, 10, 14, 23, 15, 13, 5, 16, 2, 18, 9, 7, 19, 21, 6, 12, 1 };
            Console.Write($"TSP 해: {answer[0]} ");
            totalCost = 0;
            for(int i = 1; i < answer.Length; i++)
            {
                Console.Write(answer[i] + " ");
                totalCost += testMatrix[answer[i]-1, answer[i-1]-1];
            }
            Console.WriteLine($"\nTotal Cost = {totalCost}");*/

            return groupOrder;
        }
    }
}
