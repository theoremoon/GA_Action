using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GA_Action
{
    class Pos
    {
        public int X, Y;
        public Pos(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public static Pos operator +(Pos p1, Pos p2)
        {
            return new Pos(p1.X + p2.X, p1.Y + p2.Y);
        }
    }
    // 壁や空間などのタイル
    class Tile
    {
        public const char
            SPACE = ' ',
            GOAL = '|',
            WALL = '#',
            DEATH = 'v',
            PLAYER = '&';
        public static Image SPACE_IMAGE;
        public static Image GOAL_IMAGE;
        public static Image WALL_IMAGE;
        public static Image DEATH_IMAGE;
        public static Image PLAYER_IMAGE;

        public static void Load()
        {
            SPACE_IMAGE = Image.FromFile("space.png");
            GOAL_IMAGE = Image.FromFile("goal.png");
            WALL_IMAGE = Image.FromFile("wall.png");
            DEATH_IMAGE = Image.FromFile("death.png");
            PLAYER_IMAGE = Image.FromFile("player.png");
        }
    }

    enum Action
    {
        STAY,
        GO,
        JUMP,
    }
    class Field
    {
        private List<string> lines;
        private readonly int width;
        private readonly int height;
        private List<int>[,] jumpto;

        public Field(string path)
        {
            lines = new List<string>();
            width = 0;

            // 読み込み
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() >= 0)
                {
                    // 本来は挿入する前に変な文字がないか調べるべき

                    string line = sr.ReadLine();// ReadLine の返り値には\rや\nは含まれない
                    lines.Add(line);
                    width = Math.Max(width, line.Length);
                }
            }

            // 長さを揃える
            lines = lines.Select(line => line + new string(Tile.SPACE, width - line.Length) + Tile.GOAL).ToList();
            lines.Add(new string(Tile.DEATH, width) + Tile.GOAL);
            // 高さを設定
            height = lines.Count;

            // code by dun
            jumpto = new List<int>[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = this.Get(x, y);
                    if ('0' <= c && c <= '9')
                    {
                        for (int yy = 0; yy < height; yy++)
                        {
                            for (int xx = 0; xx < width; xx++)
                            {
                                if (xx == x && yy == y) continue;
                                char cc = this.Get(xx, yy);
                                if (cc == c)
                                {
                                    jumpto[x, y] = new List<int>();
                                    jumpto[x, y].Add(xx);
                                    jumpto[x, y].Add(yy);
                                }
                            }
                        }
                    }
                }
            }
        }

        public char Get(int x, int y)
        {
            return lines.ElementAt(y).ElementAt(x);
        }

        // code by dun
        public Pos GetWarpTo(Pos pos)
        {
            return new Pos(jumpto[pos.X, pos.Y][0], jumpto[pos.X, pos.Y][1]);
        }

        // この辺りよくわからずに使ってるけど不要でしょって思うよ
        public int Width => width;
        public int Height => height;
    }


    class Simulator
    {
        private Pos pos; // player position
        private Field field;
        private int screenWidth;
        private bool end;
        private int score;
        private int turn;

        public bool End { get => end; }
        public int Score { get => score; }

        public Simulator(Field field, int screenWidth)
        {
            this.field = field;
            this.screenWidth = screenWidth;
            pos = new Pos(0, 0);
            end = false;
            score = 0;
            turn = 0;
        }

        public void Update(Action nextAction)
        {
            turn++;

            // 動く
            switch (nextAction)
            {
                case Action.STAY:
                    break;
                // 進む
                case Action.GO:
                    if (field.Get(pos.X + 1, pos.Y) != Tile.WALL)
                    {
                        pos.X++;
                    }
                    break;
                // ジャンプ
                case Action.JUMP:
                    if (field.Get(pos.X, pos.Y + 1) == Tile.WALL)
                    {
                        // 最大3マスジャンプ
                        for (int i = 0; i <= 3; i++)
                        {
                            if (pos.Y - 1 < 0) { break; } // 画面から出ない
                            if (field.Get(pos.X, pos.Y - 1) != Tile.WALL &&
                                field.Get(pos.X, pos.Y - 1) != Tile.DEATH) // これにはならんでしょ
                            {
                                pos.Y--;
                            }
                        }

                    }
                    break;
            }

            // 落ちる
            if (field.Get(pos.X, pos.Y + 1) != Tile.WALL)
            {
                pos.Y++;
            }

            score = pos.X; // 到達した座標がスコア
            // code by dun
            if ('0' <= field.Get(pos.X, pos.Y) && field.Get(pos.X, pos.Y) <= '9')
            {
                pos = field.GetWarpTo(pos);
            }

            // ゴール
            if (field.Get(pos.X, pos.Y) == Tile.GOAL)
            {
                score = pos.X + 1000 - turn; // ボーナスで+1000
                end = true;
            }
            // 死んだ
            else if (field.Get(pos.X, pos.Y) == Tile.DEATH)
            {
                end = true;
            }
        }

        // 現在の画面を返す
        public List<List<char>> GetScreen()
        {
            List<List<char>> screen = new List<List<char>>();

            // 最初
            if (pos.X <= screenWidth / 2)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    List<char> line = new List<char>();
                    for (int x = 0; x < screenWidth; x++)
                    {
                        line.Add(field.Get(x, y));
                    }
                    screen.Add(line);
                }
                screen[pos.Y][pos.X] = Tile.PLAYER;
            }
            // まんなか
            else if (pos.X < field.Width - screenWidth / 2)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    List<char> line = new List<char>();
                    for (int x = -screenWidth / 2; x < screenWidth / 2; x++)
                    {
                        line.Add(field.Get(pos.X + x, y));
                    }
                    screen.Add(line);
                }
                screen[pos.Y][screenWidth / 2] = Tile.PLAYER;
            }
            // 最後
            else
            {
                for (int y = 0; y < field.Height; y++)
                {
                    List<char> line = new List<char>();
                    for (int x = 0; x <= screenWidth; x++)
                    {
                        line.Add(field.Get(field.Width - screenWidth + x, y));
                    }
                    screen.Add(line);
                }
                screen[pos.Y][pos.X - (field.Width - screenWidth)] = Tile.PLAYER;
            }


            return screen;
        }

        // コンソールに現状を描画
        public void Draw()
        {
            List<List<char>> screen = GetScreen();

            Console.SetCursorPosition(0, 0);
            screen.ForEach(line => Console.WriteLine(line.ToArray()));
        }
    }

    class Genom
    {
        private List<Action> actions;
        private int p;
        private int score;
        public Genom(List<Action> actions)
        {
            this.actions = actions;
            p = 0;
            score = 0;
        }

        // ランダムな遺伝子列を作る。
        public static Genom Init(uint length, Random random)
        {
            List<Action> actions = new List<Action>();
            for (int i = 0; i < length; i++)
            {
                // enumの変更に強くないのでだめ
                actions.Add((Action)random.Next(3));
            }
            return new Genom(actions);
        }
        public Action Next()
        {
            Action nextAction = actions.ElementAt(p);
            p++;
            return nextAction;
        }
        public void Reset()
        {
            p = 0;
        }
        public bool End { get => p >= actions.Count; }
        public int Length { get => actions.Count; }
        public int Score { get => score; set => score = value; }
        internal List<Action> Actions { get => actions; }
    }


    class Program
    {
        static List<Genom> NextGeneration(List<Genom> parents)
        {
            List<Genom> children = new List<Genom>();
            Random random = new Random();
            const double e = 0.2;

            while (children.Count < parents.Count)
            {
                // 今回の親子
                Genom p1, p2;

                // これはランダム
                if (e >= random.NextDouble())
                {
                    parents = parents.OrderBy((x) => new Guid()).ToList();
                }
                // こっちがエリート
                else
                {
                    parents.Sort((a, b) => a.Score.CompareTo(b.Score));
                }

                // 親が選択された
                p1 = parents.ElementAt(parents.Count - 1);
                p2 = parents.ElementAt(parents.Count - 2);

                // 一様交差
                Action[] cgenom1 = new Action[p1.Length];
                Action[] cgenom2 = new Action[p1.Length];
                for (int i = 0; i < p1.Length; i++)
                {
                    if (random.Next(2) == 0)
                    {
                        cgenom1[i] = (p1.Actions.ElementAt(i));
                        cgenom2[i] = (p2.Actions.ElementAt(i));
                    }
                    else
                    {
                        cgenom2[i] = (p1.Actions.ElementAt(i));
                        cgenom1[i] = (p2.Actions.ElementAt(i));
                    }
                }

                //// 一点交差
                //int p = random.Next(p1.Length);

                //Action[] cgenom1 = p1.Actions.GetRange(0, p).Concat(p2.Actions.GetRange(p, p1.Length - p)).ToArray();
                //Action[] cgenom2 = p2.Actions.GetRange(0, p).Concat(p1.Actions.GetRange(p, p2.Length - p)).ToArray();

                // 突然変異
                for (int i = 0; i < cgenom1.Length; i++)
                {
                    if (random.NextDouble() <= 0.005)
                    {
                        cgenom1[i] = (Action)random.Next(3);
                    }
                }
                for (int i = 0; i < cgenom1.Length; i++)
                {
                    if (random.NextDouble() <= 0.005)
                    {
                        cgenom2[i] = (Action)random.Next(3);
                    }
                }


                children.Add(new Genom(cgenom1.ToList()));
                children.Add(new Genom(cgenom2.ToList()));
            }

            return children;
        }
        static void GeneticAlgorithm(Field field, int screenWidth, int seed, uint genomLength = 1000, uint groupSize = 10)
        {
            // ほげる
            if (groupSize % 2 != 0)
            {
                throw new Exception("AAN");
            }

            Viewer viewer = new Viewer();
            List<GameScreen> gameScreens = new List<GameScreen>();
            for (int i = 0; i < groupSize; i++)
            {
                GameScreen gameScreen = new GameScreen(screenWidth, field.Height);
                gameScreens.Add(gameScreen);
                viewer.AddScreen(gameScreen);
            }

            Genom maxGenom = null;
            int maxScore = -1;

            // 初期化
            Random random = new Random(seed);
            List<Genom> genoms = new List<Genom>();
            for (int i = 0; i < groupSize; i++)
            {
                genoms.Add(Genom.Init(genomLength, random));
            }

            viewer.Show();

            for (int c = 0; c < 3000; c++)
            {
                // 評価
                for (int i = 0; i < groupSize; i++)
                {
                    Genom genom = genoms.ElementAt((int)i);
                    Simulator simulator = new Simulator(field, screenWidth);
                    while (!simulator.End && !genom.End)
                    {
                        //simulator.Draw();
                        //System.Threading.Thread.Sleep(10);
                        Action nextAction = genom.Next();
                        simulator.Update(nextAction);
                        genom.Score = simulator.Score;
                    }
                    //Console.Clear();
                    //Console.SetCursorPosition(0, 20);
                    //Console.WriteLine("GENERATION:{0}", c);
                    //Console.WriteLine("SCORE:{0}", genom.Score);


                    if (maxScore < genom.Score)
                    {
                        maxScore = genom.Score;
                        maxGenom = new Genom(genom.Actions);
                    }
                }

                // 描画
                List<Simulator> simulators = new List<Simulator>();
                for (int i = 0; i < groupSize; i++)
                {
                    genoms[i].Reset();
                    simulators.Add(new Simulator(field, screenWidth));
                }
                while (true)
                {
                    bool endAll = true;
                    for (int i = 0; i < groupSize; i++)
                    {
                        if (!simulators[i].End)
                        {
                            endAll = false;
                            Action nextAction = genoms[i].Next();
                            simulators[i].Update(nextAction);
                        }
                        gameScreens[i].Update(simulators[i].GetScreen());
                    }
                    viewer.Refresh();
                    System.Threading.Thread.Sleep(100);
                    if (endAll)
                    {
                        break;
                    }
                }

                // 次世代
                genoms = NextGeneration(genoms);
            }
            {
                Genom genom = maxGenom;
                Simulator simulator = new Simulator(field, screenWidth);
                while (!simulator.End && !genom.End)
                {
                    simulator.Draw();
                    System.Threading.Thread.Sleep(20);
                    Action nextAction = genom.Next();
                    simulator.Update(nextAction);
                }
                genom.Score = simulator.Score;
                //Console.Clear();
                //Console.SetCursorPosition(0, 20);
                //Console.WriteLine("GENERATION:{0}", c);
                Console.WriteLine("SCORE:{0}", genom.Score);
            }
            Console.WriteLine("MAX SCORE:{0}", maxScore);
        }
        static void Main(string[] args)
        {
            Tile.Load();

            
            Field field = new Field("field1.txt");
            GeneticAlgorithm(field, 20, 5467890, 1000, 20);
        }

    }



    class GameScreen : Panel
    {
        private const int tileSize = 16;
        private List<List<char>> screen;

        public GameScreen(int width, int height)
        {
            this.Width = width * tileSize;
            this.Height = height * tileSize;
            this.Paint += new PaintEventHandler(OnDraw);
            this.DoubleBuffered = true;
        }
        public void Update(List<List<char>> screen)
        {
            this.screen = screen;
        }
        private void OnDraw(object sender, PaintEventArgs e)
        {
            if (screen is null)
            {
                return;
            }

            Graphics g = e.Graphics;

            for (int y = 0; y < screen.Count; y++)
            {
                for (int x = 0; x < screen[y].Count; x++)
                {
                    Image img = null;
                    switch (screen[y][x])
                    {
                        case Tile.PLAYER:
                            img = Tile.PLAYER_IMAGE;
                            break;
                        case Tile.SPACE:
                            img = Tile.SPACE_IMAGE;
                            break;
                        case Tile.WALL:
                            img = Tile.WALL_IMAGE;
                            break;
                        case Tile.GOAL:
                            img = Tile.GOAL_IMAGE;
                            break;
                        case Tile.DEATH:
                            img = Tile.DEATH_IMAGE;
                            break;
                    }
                    g.DrawImage(img, new Point(x * tileSize, y * tileSize));
                }
            }
        }

    }
    class Viewer : Form
    {
        private List<GameScreen> panels;
        private int x;
        private int y;

        public Viewer()
        {
            this.x = 0;
            this.y = 0;
            this.Width = 1600;
            this.Height = 768;
            panels = new List<GameScreen>();
        }
        public void AddScreen(GameScreen gameScreen)
        {
            if (x + gameScreen.Width > this.Width)
            {
                x = 0;
                y += gameScreen.Height + 10;
            }
            this.Controls.Add(gameScreen);
            gameScreen.Location = new Point(x, y);

            x += gameScreen.Width + 10;
        }
        public void UpdateScreen(int i, List<List<char>> screen)
        {
            if (i < panels.Count)
            {
                panels[i].Update(screen);
            }
        }
    }
}
