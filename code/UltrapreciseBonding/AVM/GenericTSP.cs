using DataStruct;
using System;
using System.Collections.Generic;
using IniFileHelper;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.FusionCollections.TSP
{
    /// <summary>
    /// 遗传算法路径规划
    /// </summary>
    public static class TspTool
    {
        /// <summary>
        /// 计算最短路径顺序
        /// </summary>
        /// <param name="pointsIn">输入路径上的所有点</param>
        /// <param name="reOrder">输出最短的遍历顺序</param>
        /// <returns>AVM_INPUT_POINT_NUM_ERROR：输入点数要大于3</returns>
        public static Errortype CalcTSP(List<Point> pointsIn, out List<int> reOrder)
        {
            int populationSize = 10000;     // 当前参数规模适合少于35点
            int maxGenerations = 100000;    // 当前参数规模适合少于35点
            int mutation = 3;   // 3%
            int groupSize = 5;
            int seed = 0;
            int numberOfCloseCities = 5;
            int chanceUseCloseCity = 90;
            reOrder = new List<int>();
            if (pointsIn.Count < 3)
            {
                return Errortype.AVM_INPUT_POINT_NUM_ERROR;
            }

            SpotList spotList = new SpotList();
            foreach (Point pt in pointsIn)
            {
                spotList.Add(new Spot((int)pt.X, (int)pt.Y));
            }

            spotList.CalculateSpotDistances(numberOfCloseCities);
            Tsp tsp = new Tsp();
            tsp.Begin(populationSize, maxGenerations, groupSize, mutation, seed, chanceUseCloseCity, spotList, out List<int> reOrderedIndex);
            tsp = null;
            reOrder = reOrderedIndex;
            return Errortype.OK;
        }

        /// <summary>
        /// 计算最短路径顺序
        /// </summary>
        /// <param name="pointsIn">输入路径上的所有点</param>
        /// <param name="reOrder">输出最短的遍历顺序</param>
        /// <returns>AVM_INPUT_POINT_NUM_ERROR：输入点数要大于3</returns>
        public static Errortype CalcTSPUseFarPairPoints(List<Point> pointsIn, out List<int> reOrder)
        {
            int populationSize = 10000;     // 当前参数规模适合少于35点
            int maxGenerations = 100000;    // 当前参数规模适合少于35点
            int mutation = 3;   // 3%
            int groupSize = 5;
            int seed = 0;
            int numberOfCloseCities = 5;
            int chanceUseCloseCity = 90;
            reOrder = new List<int>();
            if (pointsIn.Count < 3)
            {
                return Errortype.AVM_INPUT_POINT_NUM_ERROR;
            }

            double maxDist = 0;
            int maxDistIndex = 0;
            for (int i = 1; i < pointsIn.Count; i++)
            {
                double dist = ComAlgo.Dist(pointsIn[0], pointsIn[i]);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxDistIndex = i;
                }
            }

            SpotList spotList = new SpotList();
            spotList.Add(new Spot((int)pointsIn[maxDistIndex].X, (int)pointsIn[maxDistIndex].Y)); //起点设置为距离第一个点最远的点

            for (int i = 1; i < pointsIn.Count; i++)
            {
                if (i != maxDistIndex)
                {
                    spotList.Add(new Spot((int)pointsIn[i].X, (int)pointsIn[i].Y)); //第一个点不参与路径规划
                }
            }

            spotList.CalculateSpotDistances(numberOfCloseCities);
            Tsp tsp = new Tsp();
            tsp.Begin(populationSize, maxGenerations, groupSize, mutation, seed, chanceUseCloseCity, spotList, out List<int> reOrderedIndex);
            tsp = null;
            reOrder.Add(0); //第一个点塞入路径中
            reOrder.Add(maxDistIndex); //第一个点塞入路径中
            for (int i = 1; i < reOrderedIndex.Count; i++)
            {
                if (reOrderedIndex[i] < maxDistIndex)
                {
                    reOrder.Add(reOrderedIndex[i]);
                }
                else
                {
                    reOrder.Add(reOrderedIndex[i] + 1);
                }
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// TSP类
    /// </summary>
    internal class Tsp
    {
        private Random _rand;
        private SpotList _pointList;
        private Population _population;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Tsp()
        {
        }

        /// <summary>
        /// 开始计算路径
        /// </summary>
        /// <param name="populationSize">随机序列总数量</param>
        /// <param name="maxIterations">基因迭代轮数</param>
        /// <param name="groupSize">每次迭代时检查的序列数量</param>
        /// <param name="mutation">突变概率</param>
        /// <param name="seed">0</param>
        /// <param name="chanceToUseCloseSpot">临近点数量</param>
        /// <param name="spotList">点位list</param>
        /// <param name="finalListIndex">输出最终遍历顺序</param>
        public void Begin(int populationSize, int maxIterations, int groupSize, int mutation, int seed, int chanceToUseCloseSpot, SpotList spotList, out List<int> finalListIndex)
        {
            finalListIndex = new List<int>();
            _rand = new Random(seed);

            this._pointList = spotList;
            _population = new Population();

            //生成随机序列组
            _population.CreateRandomPopulation(populationSize, spotList, _rand, chanceToUseCloseSpot);
            bool foundNewBestTour = false;
            int generation;

            //迭代出最优序列组
            for (generation = 0; generation < maxIterations; generation++)
            {
                foundNewBestTour = MakeChildren(groupSize, mutation);
            }

            //使用最优基因里的序列顺序
            int lastPointId = 0;
            int nextPointId = _population.BestTour[0].Connection1;
            foreach (Spot spot in _pointList)
            {
                finalListIndex.Add(lastPointId);
                if (lastPointId != _population.BestTour[nextPointId].Connection1)
                {
                    lastPointId = nextPointId;
                    nextPointId = _population.BestTour[nextPointId].Connection1;
                }
                else
                {
                    lastPointId = nextPointId;
                    nextPointId = _population.BestTour[nextPointId].Connection2;
                }
            }
        }

        /// <summary>
        /// 从所有基因中选择一部分
        /// 选择其中两组长度最短的组合(基因)作为父本然后进行交叉操作
        /// 完成交叉后替换这组组合(基因)中长度最长的两组
        /// </summary>
        /// <param name="groupSize">分组长度</param>
        /// <param name="mutation">子组合突变概率</param>
        /// <returns>true:最短路径计算成功</returns>
        internal bool MakeChildren(int groupSize, int mutation)
        {
            int[] tourGroup = new int[groupSize];
            int tourCount, i, topTour, childPosition, tempTour;

            // pick random tours to be in the neighborhood city group
            // we allow for the same tour to be included twice
            for (tourCount = 0; tourCount < groupSize; tourCount++)
            {
                tourGroup[tourCount] = _rand.Next(_population.Count);
            }

            // bubble sort on the neighborhood city group
            for (tourCount = 0; tourCount < groupSize - 1; tourCount++)
            {
                topTour = tourCount;
                for (i = topTour + 1; i < groupSize; i++)
                {
                    if (_population[tourGroup[i]].Fitness < _population[tourGroup[topTour]].Fitness)
                    {
                        topTour = i;
                    }
                }

                if (topTour != tourCount)
                {
                    tempTour = tourGroup[tourCount];
                    tourGroup[tourCount] = tourGroup[topTour];
                    tourGroup[topTour] = tempTour;
                }
            }

            bool foundNewBestTour = false;

            // 提取两组最优序列作为父本，执行序列交叉，交叉完成后，替换这一批序列中最差(路径最长)的序列
            childPosition = tourGroup[groupSize - 1];
            _population[childPosition] = Tour.Crossover(_population[tourGroup[0]], _population[tourGroup[1]], _pointList, _rand);
            if (_rand.Next(100) < mutation)
            {
                _population[childPosition].Mutate(_rand);
            }

            _population[childPosition].DetermineFitness(_pointList);

            // 检查生成的子序列是否比目前的最优序列更好(路径长度更短)；
            if (_population[childPosition].Fitness < _population.BestTour.Fitness)
            {
                _population.BestTour = _population[childPosition];
                foundNewBestTour = true;
            }

            // take the best 2 tours (opposite order), do crossover, and replace the 2nd worst tour with it
            childPosition = tourGroup[groupSize - 2];
            _population[childPosition] = Tour.Crossover(_population[tourGroup[1]], _population[tourGroup[0]], _pointList, _rand);
            if (_rand.Next(100) < mutation)
            {
                _population[childPosition].Mutate(_rand);
            }

            _population[childPosition].DetermineFitness(_pointList);

            // now see if the second new tour has the best fitness
            if (_population[childPosition].Fitness < _population.BestTour.Fitness)
            {
                _population.BestTour = _population[childPosition];
                foundNewBestTour = true;
            }

            return foundNewBestTour;
        }
    }

    /// <summary>
    /// 路径上的单个节点
    /// </summary>
    internal class Spot
    {
        /// <summary>
        /// 单点
        /// </summary>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        public Spot(int x, int y)
        {
            Location = new Point(x, y);
        }

        private Point _location;

        /// <summary>
        /// Gets or sets 节点的坐标
        /// </summary>
        public Point Location
        {
            get
            {
                return _location;
            }

            set
            {
                _location = value;
            }
        }

        /// <summary>
        /// 该点的邻节点距离
        /// </summary>
        private List<double> _distances = new List<double>();

        /// <summary>
        /// Gets or sets 节点间距离
        /// </summary>
        public List<double> Distances
        {
            get
            {
                return _distances;
            }

            set
            {
                _distances = value;
            }
        }

        /// <summary>
        /// 该点的邻节点序号
        /// </summary>
        private List<int> _closeSpot = new List<int>();

        /// <summary>
        /// Gets 获取临近节点
        /// </summary>
        public List<int> CloseCities
        {
            get
            {
                return _closeSpot;
            }
        }

        /// <summary>
        /// 查找该点的临近点
        /// </summary>
        /// <param name="numberOfCloseSpot">临近点数量</param>
        public void FindClosestSpot(int numberOfCloseSpot)
        {
            double shortestDistance;
            int shortestSpot = 0;
            double[] dist = new double[Distances.Count];
            Distances.CopyTo(dist);

            if (numberOfCloseSpot > Distances.Count - 1)
            {
                numberOfCloseSpot = Distances.Count - 1;
            }

            _closeSpot.Clear();

            for (int i = 0; i < numberOfCloseSpot; i++)
            {
                shortestDistance = Double.MaxValue;
                for (int spotNum = 0; spotNum < Distances.Count; spotNum++)
                {
                    if (dist[spotNum] < shortestDistance)
                    {
                        shortestDistance = dist[spotNum];
                        shortestSpot = spotNum;
                    }
                }

                _closeSpot.Add(shortestSpot);
                dist[shortestSpot] = Double.MaxValue;
            }
        }
    }

    /// <summary>
    /// 节点集合
    /// </summary>
    internal class SpotList : List<Spot>
    {
        /// <summary>
        /// 定义两点间距离
        /// </summary>
        /// <param name="numberOfCloseCities">设定每个点周围临近点的数量</param>
        public void CalculateSpotDistances(int numberOfCloseCities)
        {
            foreach (Spot spot in this)
            {
                spot.Distances.Clear();

                for (int i = 0; i < Count; i++)
                {
                    spot.Distances.Add(Math.Sqrt(Math.Pow((double)(spot.Location.X - this[i].Location.X), 2D) +
                                       Math.Pow((double)(spot.Location.Y - this[i].Location.Y), 2D)));
                }
            }

            foreach (Spot spot in this)
            {
                spot.FindClosestSpot(numberOfCloseCities);
            }
        }
    }

    /// <summary>
    /// 两个坐标点之间的链接
    /// This Coordinate connects to 2 other Coordinates.
    /// </summary>
    internal class Link
    {
        private int _connection1;

        /// <summary>
        /// Gets or sets Connection to the first Coordinate.
        /// </summary>
        public int Connection1
        {
            get
            {
                return _connection1;
            }

            set
            {
                _connection1 = value;
            }
        }

        private int _connection2;

        /// <summary>
        /// Gets or sets Connection to the second Coordinate.
        /// </summary>
        public int Connection2
        {
            get
            {
                return _connection2;
            }

            set
            {
                _connection2 = value;
            }
        }
    }

    /// <summary>
    /// This class represents one instance of a tour through all the cities.
    /// </summary>
    internal class Tour : List<Link>
    {
        /// <summary>
        /// Constructor that takes a default capacity.
        /// </summary>
        /// <param name="capacity">Initial size of the tour. Should be the number of cities in the tour.</param>
        public Tour(int capacity)
            : base(capacity)
        {
            ResetTour(capacity);
        }

        /// <summary>
        /// Private copy of this _fitness of this tour.
        /// </summary>
        private double _fitness;

        /// <summary>
        /// Gets or sets _fitness of this tour
        /// </summary>
        public double Fitness
        {
            get
            {
                return _fitness;
            }

            set
            {
                _fitness = value;
            }
        }

        /// <summary>
        /// Creates the tour with the correct number of cities and creates initial connections of all -1.
        /// </summary>
        /// <param name="numberOfCities">节点数量</param>
        private void ResetTour(int numberOfCities)
        {
            this.Clear();
            Link link;
            for (int i = 0; i < numberOfCities; i++)
            {
                link = new Link
                {
                    Connection1 = -1,
                    Connection2 = -1,
                };
                this.Add(link);
            }
        }

        /// <summary>
        /// Determine the _fitness (total length) of an individual tour.
        /// </summary>
        /// <param name="cities">The cities in this tour. Used to get the distance between each city.</param>
        public void DetermineFitness(SpotList cities)
        {
            Fitness = 0;

            int lastSpot = 0;
            int nextSpot = this[0].Connection1;

            foreach (Link link in this)
            {
                Fitness += cities[lastSpot].Distances[nextSpot];

                // figure out if the next city in the list is [0] or [1]
                if (lastSpot != this[nextSpot].Connection1)
                {
                    lastSpot = nextSpot;
                    nextSpot = this[nextSpot].Connection1;
                }
                else
                {
                    lastSpot = nextSpot;
                    nextSpot = this[nextSpot].Connection2;
                }
            }
        }

        /// <summary>
        /// Creates a link between 2 cities in a tour, and then updates the city usage.
        /// </summary>
        /// <param name="tour">The incomplete child tour.</param>
        /// <param name="spotUsage">Number of times each city has been used in this tour. Is updated when cities are joined.</param>
        /// <param name="spot1">The first city in the link.</param>
        /// <param name="spot2">The second city in the link.</param>
        private static void JoinSpot(Tour tour, int[] spotUsage, int spot1, int spot2)
        {
            // Determine if the [0] or [1] link is available in the tour to make this link.
            if (tour[spot1].Connection1 == -1)
            {
                tour[spot1].Connection1 = spot2;
            }
            else
            {
                tour[spot1].Connection2 = spot2;
            }

            if (tour[spot2].Connection1 == -1)
            {
                tour[spot2].Connection1 = spot1;
            }
            else
            {
                tour[spot2].Connection2 = spot1;
            }

            spotUsage[spot1]++;
            spotUsage[spot2]++;
        }

        /// <summary>
        /// Find a link from a given city in the parent tour that can be placed in the child tour.
        /// If both links in the parent aren't valid links for the child tour, return -1.
        /// </summary>
        /// <param name="parent">The parent tour to get the link from.</param>
        /// <param name="child">The child tour that the link will be placed in.</param>
        /// <param name="spotList">The list of cities in this tour.</param>
        /// <param name="spotUsage">Number of times each city has been used in the child.</param>
        /// <param name="spot">City that we want to link from.</param>
        /// <returns>The city to link to in the child tour, or -1 if none are valid.</returns>
        private static int FindNextSpot(Tour parent, Tour child, SpotList spotList, int[] spotUsage, int spot)
        {
            if (TestConnectionValid(child, spotList, spotUsage, spot, parent[spot].Connection1))
            {
                return parent[spot].Connection1;
            }
            else if (TestConnectionValid(child, spotList, spotUsage, spot, parent[spot].Connection2))
            {
                return parent[spot].Connection2;
            }

            return -1;
        }

        /// <summary>
        /// Determine if it is OK to connect 2 cities given the existing connections in a child tour.
        /// If the two cities can be connected already (witout doing a full tour) then it is an invalid link.
        /// </summary>
        /// <param name="tour">The incomplete child tour.</param>
        /// <param name="spotList">The list of cities in this tour.</param>
        /// <param name="spotUsage">Array that contains the number of times each city has been linked.</param>
        /// <param name="spot1">The first city in the link.</param>
        /// <param name="spot2">The second city in the link.</param>
        /// <returns>True if the connection can be made.</returns>
        private static bool TestConnectionValid(Tour tour, SpotList spotList, int[] spotUsage, int spot1, int spot2)
        {
            // Quick check to see if cities already connected or if either already has 2 links
            if ((spot1 == spot2) || (spotUsage[spot1] == 2) || (spotUsage[spot2] == 2))
            {
                return false;
            }

            // A quick check to save CPU. If haven't been to either city, connection must be valid.
            if ((spotUsage[spot1] == 0) || (spotUsage[spot2] == 0))
            {
                return true;
            }

            // Have to see if the cities are connected by going in each direction.
            for (int direction = 0; direction < 2; direction++)
            {
                int lastSpot = spot1;
                int currentSpot;
                if (direction == 0)
                {
                    currentSpot = tour[spot1].Connection1;  // on first pass, use the first connection
                }
                else
                {
                    currentSpot = tour[spot1].Connection2;  // on second pass, use the other connection
                }

                int tourLength = 0;
                while ((currentSpot != -1) && (currentSpot != spot2) && (tourLength < spotList.Count - 2))
                {
                    tourLength++;

                    // figure out if the next city in the list is [0] or [1]
                    if (lastSpot != tour[currentSpot].Connection1)
                    {
                        lastSpot = currentSpot;
                        currentSpot = tour[currentSpot].Connection1;
                    }
                    else
                    {
                        lastSpot = currentSpot;
                        currentSpot = tour[currentSpot].Connection2;
                    }
                }

                // if cities are connected, but it goes through every city in the list, then OK to join.
                if (tourLength >= spotList.Count - 2)
                {
                    return true;
                }

                // if the cities are connected without going through all the cities, it is NOT OK to join.
                if (currentSpot == spot2)
                {
                    return false;
                }
            }

            // if cities weren't connected going in either direction, we are OK to join them
            return true;
        }

        /// <summary>
        /// Perform the crossover operation on 2 parent tours to create a new child tour.
        /// This function should be called twice to make the 2 children.
        /// In the second call, the parent parameters should be swapped.
        /// </summary>
        /// <param name="parent1">The first parent tour.</param>
        /// <param name="parent2">The second parent tour.</param>
        /// <param name="spotList">The list of cities in this tour.</param>
        /// <param name="rand">Random number generator. We pass around the same random number generator, so that results between runs are consistent.</param>
        /// <returns>The child tour.</returns>
        public static Tour Crossover(Tour parent1, Tour parent2, SpotList spotList, Random rand)
        {
            Tour child = new Tour(spotList.Count);      // the new tour we are making
            int[] spotUsage = new int[spotList.Count];  // how many links 0-2 that connect to this city
            int spot;                                   // for loop variable
            int nextSpot;                               // the other city in this link

            for (spot = 0; spot < spotList.Count; spot++)
            {
                spotUsage[spot] = 0;
            }

            // Take all links that both parents agree on and put them in the child
            for (spot = 0; spot < spotList.Count; spot++)
            {
                if (spotUsage[spot] < 2)
                {
                    if (parent1[spot].Connection1 == parent2[spot].Connection1)
                    {
                        nextSpot = parent1[spot].Connection1;
                        if (TestConnectionValid(child, spotList, spotUsage, spot, nextSpot))
                        {
                            JoinSpot(child, spotUsage, spot, nextSpot);
                        }
                    }

                    if (parent1[spot].Connection2 == parent2[spot].Connection2)
                    {
                        nextSpot = parent1[spot].Connection2;
                        if (TestConnectionValid(child, spotList, spotUsage, spot, nextSpot))
                        {
                            JoinSpot(child, spotUsage, spot, nextSpot);
                        }
                    }

                    if (parent1[spot].Connection1 == parent2[spot].Connection2)
                    {
                        nextSpot = parent1[spot].Connection1;
                        if (TestConnectionValid(child, spotList, spotUsage, spot, nextSpot))
                        {
                            JoinSpot(child, spotUsage, spot, nextSpot);
                        }
                    }

                    if (parent1[spot].Connection2 == parent2[spot].Connection1)
                    {
                        nextSpot = parent1[spot].Connection2;
                        if (TestConnectionValid(child, spotList, spotUsage, spot, nextSpot))
                        {
                            JoinSpot(child, spotUsage, spot, nextSpot);
                        }
                    }
                }
            }

            // The parents don't agree on whats left, so we will alternate between using links from parent 1 and then parent 2.
            for (spot = 0; spot < spotList.Count; spot++)
            {
                if (spotUsage[spot] < 2)
                {
                    // we prefer to use parent 1 on odd cities
                    if (spot % 2 == 1)
                    {
                        nextSpot = FindNextSpot(parent1, child, spotList, spotUsage, spot);

                        // but if thats not possible we still go with parent 2
                        if (nextSpot == -1)
                        {
                            nextSpot = FindNextSpot(parent2, child, spotList, spotUsage, spot);
                        }
                    }
                    else
                    {
                        // use parent 2 instead
                        nextSpot = FindNextSpot(parent2, child, spotList, spotUsage, spot);
                        if (nextSpot == -1)
                        {
                            nextSpot = FindNextSpot(parent1, child, spotList, spotUsage, spot);
                        }
                    }

                    if (nextSpot != -1)
                    {
                        JoinSpot(child, spotUsage, spot, nextSpot);

                        // not done yet. must have been 0 in above case.
                        if (spotUsage[spot] == 1)
                        {
                            // use parent 1 on even cities
                            if (spot % 2 != 1)
                            {
                                nextSpot = FindNextSpot(parent1, child, spotList, spotUsage, spot);

                                // use parent 2 instead
                                if (nextSpot == -1)
                                {
                                    nextSpot = FindNextSpot(parent2, child, spotList, spotUsage, spot);
                                }
                            }

                            // use parent 2
                            else
                            {
                                nextSpot = FindNextSpot(parent2, child, spotList, spotUsage, spot);
                                if (nextSpot == -1)
                                {
                                    nextSpot = FindNextSpot(parent1, child, spotList, spotUsage, spot);
                                }
                            }

                            if (nextSpot != -1)
                            {
                                JoinSpot(child, spotUsage, spot, nextSpot);
                            }
                        }
                    }
                }
            }

            // Remaining links must be completely random.
            // Parent's links would cause multiple disconnected loops.
            for (spot = 0; spot < spotList.Count; spot++)
            {
                while (spotUsage[spot] < 2)
                {
                    do
                    {
                        nextSpot = rand.Next(spotList.Count);  // pick a random city, until we find one we can link to
                    }
                    while (!TestConnectionValid(child, spotList, spotUsage, spot, nextSpot));

                    JoinSpot(child, spotUsage, spot, nextSpot);
                }
            }

            return child;
        }

        /// <summary>
        /// Randomly change one of the links in this tour.
        /// </summary>
        /// <param name="rand">Random number generator. We pass around the same random number generator, so that results between runs are consistent.</param>
        public void Mutate(Random rand)
        {
            int spotNumber = rand.Next(this.Count);
            Link link = this[spotNumber];
            int tmpSpotNumber;

            // Find which 2 cities connect to cityNumber, and then connect them directly
            // Conn 1 on Conn 1 link points back to us.
            if (this[link.Connection1].Connection1 == spotNumber)
            {
                // Conn 1 on Conn 2 link points back to us.
                if (this[link.Connection2].Connection1 == spotNumber)
                {
                    tmpSpotNumber = link.Connection2;
                    this[link.Connection2].Connection1 = link.Connection1;
                    this[link.Connection1].Connection1 = tmpSpotNumber;
                }

                // Conn 2 on Conn 2 link points back to us.
                else
                {
                    tmpSpotNumber = link.Connection2;
                    this[link.Connection2].Connection2 = link.Connection1;
                    this[link.Connection1].Connection1 = tmpSpotNumber;
                }
            }

            // Conn 2 on Conn 1 link points back to us.
            else
            {
                // Conn 1 on Conn 2 link points back to us.
                if (this[link.Connection2].Connection1 == spotNumber)
                {
                    tmpSpotNumber = link.Connection2;
                    this[link.Connection2].Connection1 = link.Connection1;
                    this[link.Connection1].Connection2 = tmpSpotNumber;
                }

                // Conn 2 on Conn 2 link points back to us.
                else
                {
                    tmpSpotNumber = link.Connection2;
                    this[link.Connection2].Connection2 = link.Connection1;
                    this[link.Connection1].Connection2 = tmpSpotNumber;
                }
            }

            int replaceSpotNumber = -1;
            do
            {
                replaceSpotNumber = rand.Next(this.Count);
            }
            while (replaceSpotNumber == spotNumber);
            Link replaceLink = this[replaceSpotNumber];

            // Now we have to reinsert that city back into the tour at a random location
            tmpSpotNumber = replaceLink.Connection2;
            link.Connection2 = replaceLink.Connection2;
            link.Connection1 = replaceSpotNumber;
            replaceLink.Connection2 = spotNumber;

            if (this[tmpSpotNumber].Connection1 == replaceSpotNumber)
            {
                this[tmpSpotNumber].Connection1 = spotNumber;
            }
            else
            {
                this[tmpSpotNumber].Connection2 = spotNumber;
            }
        }
    }

    /// <summary>
    /// 路径基因
    /// </summary>
    internal class Population : List<Tour>
    {
        /// <summary>
        /// Private copy of the best tour found so far by the Genetic Algorithm.
        /// </summary>
        private Tour _bestTour = null;

        /// <summary>
        /// Gets or Sets The best tour found so far by the Genetic Algorithm.
        /// </summary>
        public Tour BestTour
        {
            get
            {
                return _bestTour;
            }

            set
            {
                _bestTour = value;
            }
        }

        /// <summary>
        /// Create the initial set of random tours.
        /// </summary>
        /// <param name="populationSize">Number of tours to create.</param>
        /// <param name="spotList">The list of cities in this tour.</param>
        /// <param name="rand">Random number generator. We pass around the same random number generator, so that results between runs are consistent.</param>
        /// <param name="chanceToUseCloseSpot">The odds (out of 100) that a city that is known to be close will be used in any given link.</param>
        public void CreateRandomPopulation(int populationSize, SpotList spotList, Random rand, int chanceToUseCloseSpot)
        {
            int firstSpot, lastSpot, nextSpot;

            for (int tourCount = 0; tourCount < populationSize; tourCount++)
            {
                Tour tour = new Tour(spotList.Count);

                // Create a starting point for this tour
                firstSpot = rand.Next(spotList.Count);
                lastSpot = firstSpot;

                for (int spot = 0; spot < spotList.Count - 1; spot++)
                {
                    do
                    {
                        // Keep picking random cities for the next city, until we find one we haven't been to.
                        if ((rand.Next(100) < chanceToUseCloseSpot) && (spotList[spot].CloseCities.Count > 0))
                        {
                            // 75% chance will will pick a city that is close to this one
                            nextSpot = spotList[spot].CloseCities[rand.Next(spotList[spot].CloseCities.Count)];
                        }
                        else
                        {
                            // Otherwise, pick a completely random city.
                            nextSpot = rand.Next(spotList.Count);
                        }

                        // Make sure we haven't been here, and make sure it isn't where we are at now.
                    }
                    while ((tour[nextSpot].Connection2 != -1) || (nextSpot == lastSpot));

                    // When going from city A to B, [1] on A = B and [1] on city B = A
                    tour[lastSpot].Connection2 = nextSpot;
                    tour[nextSpot].Connection1 = lastSpot;
                    lastSpot = nextSpot;
                }

                // Connect the last 2 cities.
                tour[lastSpot].Connection2 = firstSpot;
                tour[firstSpot].Connection1 = lastSpot;

                tour.DetermineFitness(spotList);

                Add(tour);

                if ((_bestTour == null) || (tour.Fitness < _bestTour.Fitness))
                {
                    BestTour = tour;
                }
            }
        }
    }
}