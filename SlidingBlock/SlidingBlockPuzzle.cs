using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;

namespace SlidingPuzzle
{
    class SlidingBlockType
    {
        // Template block
        public ObservableCollection<Point> SubBlocks { get; set; }
        public int SizeX { get; private set; }
        public int SizeY { get; private set; }
        public int TypeID { get; private set; }

        public SlidingBlockType(SlidingBlockType copy)
        {
            SubBlocks = new ObservableCollection<Point>(copy.SubBlocks);        // Point is a struct so values will be copied
            SizeX = copy.SizeX;
            SizeY = copy.SizeY;
            TypeID = copy.TypeID;
        }

        public SlidingBlockType(SlidingBlockType copy, int typeID)
        {
            SubBlocks = new ObservableCollection<Point>(copy.SubBlocks);        // Point is a struct so values will be copied
            SizeX = copy.SizeX;
            SizeY = copy.SizeY;
            TypeID = typeID;
        }

        public SlidingBlockType(IEnumerable<Point> subBlocks, int typeID)
        {
            SubBlocks = new ObservableCollection<Point>(subBlocks);
            if (subBlocks.Any())           // If sequence is not empty
            {
                SizeX = subBlocks.Max(t => t.X) + 1;
                SizeY = subBlocks.Max(t => t.Y) + 1;
                TypeID = typeID;
            }
        }

        public void AddSubBlock(int x, int y)
        {
            int xdiff = x < 0 ? -x : 0;
            int ydiff = y < 0 ? -y : 0;
            for (int i = 0; i < SubBlocks.Count; i++)
                SubBlocks[i] = new Point(SubBlocks[i].X + xdiff, SubBlocks[i].Y + ydiff);
            Point p = new Point(x + xdiff, y + ydiff);
            int index = SubBlocks.Count;
            while (index > 0 && comparePoints(SubBlocks[index - 1], p))
                index--;
            SubBlocks.Insert(index, p);         // Insert block keeping them sorted
            SizeX += xdiff;
            SizeY += ydiff;
            if (x >= SizeX) SizeX = x + 1;
            if (y >= SizeY) SizeY = y + 1;
        }

        bool comparePoints(Point p, Point q)
        {
            return p.Y == q.Y ? p.X > q.X : p.Y > q.Y;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            SlidingBlockType b = (SlidingBlockType)obj;
            return b.SubBlocks.SequenceEqual(SubBlocks);
        }
    }

    class SlidingBlock : INotifyPropertyChanged
    {
        // Concrete block
        public event PropertyChangedEventHandler PropertyChanged;

        int cellX;
        public int CellX
        {
            get { return cellX; }
            set { cellX = value; OnPropertyChanged("CellX"); }
        }
        int cellY;
        public int CellY
        {
            get { return cellY; }
            set { cellY = value; OnPropertyChanged("CellY"); }
        }
        SlidingBlockType blockType;
        public SlidingBlockType BlockType
        {
            get { return blockType; }
            set { blockType = value; OnPropertyChanged("BlockType"); }
        }
        public int BlockID { get; private set; }

        public SlidingBlock(int cellX, int cellY, SlidingBlockType blockType, int blockID)
        {
            CellX = cellX;
            CellY = cellY;
            BlockType = blockType;
            BlockID = blockID;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class Position
    {
        // Compact puzzle state representation used when solving
        public byte[] Blocks;               // Each element is CellX, CellY, BlockType
        public int BlockNr;                 // Last move
        public int Xdir;                    // Last move
        public int Ydir;                    // Last move
        public Position PreviousPosition;

        static Random rng = new Random();
        static int[] rnums = new int[256];

        static Position()
        {
            for (int i=0; i<rnums.Length;i++)
                rnums[i] = rng.Next(0x8000000);
        }

        public Position(byte[] blocks, int blockNr, int xdir, int ydir, Position previousPosition)
        {
            Blocks = new byte[blocks.Length];
            blocks.CopyTo(Blocks, 0);
            this.BlockNr = blockNr;
            this.Xdir = xdir;
            this.Ydir = ydir;
            PreviousPosition = previousPosition;
        }

        public override int GetHashCode()
        {
            int n = 0;
            for (int i = 0; i < Blocks.Length; i+=3)
            {
                //n = n % 0x58b72f4a;
                n += rnums[(Blocks[i] + Blocks[i+1]*16)] + rnums[Blocks[i+2]];
                //n += (Blocks[i+1] + 19) * rnums[Blocks[i+2] * 2 + 1];   // (CellY+19) * rnums[BlockType*2]
            }
            return n;
        }

        public override bool Equals(object obj)
        {
            // Gets here when the hashes are equal
            byte[] blocks = ((Position)obj).Blocks;
            int[] grid = new int[256];
            for (int i = 0; i < Blocks.Length; i += 3)
            {
                grid[Blocks[i + 1] * 16 + Blocks[i]] += (Blocks[i + 2] + 1) * (Blocks[i + 2] + 1);      // Add (BlockType+1)^2 to grid[x,y]
            }
            // It's done this way so that different blocks of the same type at x,y yield the same position
            // Beware many non-overlapping blocks can share the same grid location! So we square the (BlockType+1).
            bool equal = true;
            for (int i = 0; i < blocks.Length; i += 3)
            {
                grid[blocks[i + 1] * 16 + blocks[i]] -= (blocks[i + 2] + 1) * (blocks[i + 2] + 1);      // Sub (BlockType+1)^2 from grid[x,y]
                if (grid[blocks[i + 1] * 16 + blocks[i]] < 0) equal = false;                            // If grid[x,y] goes < 0 then the positions are not equal
            }
            return equal;
        }
    }

    class Move
    {
        public int BlockID { get; set; }
        public int Xdir { get; set; }
        public int Ydir { get; set; }

        public Move(int blockNr, int xdir, int ydir)
        {
            BlockID = blockNr;
            Xdir = xdir;
            Ydir = ydir;
        }

        public override string ToString()
        {
            return BlockID.ToString() + " " + Xdir.ToString() + " " + Ydir.ToString();
        }
    }

    class SlidingBlockPuzzle : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        int cellsX = 4;
        public int CellsX { get { return cellsX; } set { cellsX = value; OnPropertyChanged("CellsX"); } }
        int cellsY = 5;
        public int CellsY { get { return cellsY; } set { cellsY = value; OnPropertyChanged("CellsY"); } }
        bool[,] occupied;
        Queue<Position> queue = new Queue<Position>();
        HashSet<Position> visited = new HashSet<Position>();
        SlidingBlock currentBlock;
        SlidingBlockType currentType;
        public ObservableCollection<SlidingBlock> Blocks { get; set; }
        public ObservableCollection<SlidingBlock> TargetBlocks { get; set; }
        List<SlidingBlockType> blockPalette = new List<SlidingBlockType>();
        public ObservableCollection<Move> Solution { get; set; }
        Position solutionFound;
        int currentSolutionIndex;
        public ICommand NewGridSizeCommand { get; private set; }
        public ICommand AddNewBlockCommand { get; private set; }
        public ICommand AddToBlockCommand { get; private set; }
        public ICommand StopAddingToBlockCommand { get; private set; }
        public ICommand AddTargetBlockCommand { get; private set; }
        public ICommand SolveCommand { get; private set; }
        public ICommand ClearMainCommand { get; private set; }
        public ICommand ClearTargetCommand { get; private set; }
        public ICommand NavigateSolutionCommand { get; private set; }

        public SlidingBlockPuzzle()
        {
            Blocks = new ObservableCollection<SlidingBlock>();
            TargetBlocks = new ObservableCollection<SlidingBlock>();
            Solution = new ObservableCollection<Move>();
            currentSolutionIndex = -1;
            gridSizeChanged();
            NewGridSizeCommand = new RelayCommand(newGridSize);
            AddNewBlockCommand = new RelayCommand(addNewBlock, canAddNewBlock);
            AddToBlockCommand = new RelayCommand(addToBlock, canAddBlock);
            AddTargetBlockCommand = new RelayCommand(addTargetBlock, canAddTargetBlock);
            StopAddingToBlockCommand = new RelayCommand(p => stopAddingToBlock(), p => canStopAddingToBlock());
            SolveCommand = new RelayCommand(p=>solve(), p=>canSolve());
            ClearMainCommand = new RelayCommand(p => clearMain());
            ClearTargetCommand = new RelayCommand(p => clearTarget());
            NavigateSolutionCommand = new RelayCommand(navigateSolution, canNavigateSolution);
        }

        void gridSizeChanged()
        {
            occupied = new bool[cellsX, cellsY];
            clearMain();
            blockPalette.Clear();
            blockPalette.Add(new SlidingBlockType(new List<Point>() { new Point(0, 0) }, blockPalette.Count));          // 1x1 block for zeroth item in palette
        }

        void newGridSize(object o)
        {
            object[] args = (object[])o;
            CellsX = (int)args[0];
            CellsY = (int)args[1];
            gridSizeChanged();
        }

        void addNewBlock(object o)
        {
            // Clear the target block and start the creation of a new block on the main grid
            object[] args = (object[])o;        // int cellX, int cellY
            int cellX = (int)args[0];
            int cellY = (int)args[1];
            clearTarget();
            if (!occupied[cellX, cellY])
            {
                currentBlock = new SlidingBlock(cellX, cellY,blockPalette[0], Blocks.Count);          // Create 1x1 block, BlockType 0, blockID is the index into Blocks
                currentType = new SlidingBlockType(blockPalette[0]);
                Blocks.Add(currentBlock);
                occupied[cellX, cellY] = true;
            }
        }

        bool canAddNewBlock(object o)
        {
            return true;
        }

        void addToBlock(object o)
        {
            // Continue the creation of a new block on the main grid
            if (currentBlock == null) return;   // Return if creating a new block is not in progress
            object[] args = (object[])o;        // int cellX, int cellY, IEnumerable<Tuple<int, int>> subBlocks
            int cellX = (int)args[0];
            int cellY = (int)args[1];
            if (occupied[cellX, cellY]) return;
            clearTarget();
            occupied[cellX, cellY] = true;
            currentType.AddSubBlock(cellX - currentBlock.CellX, cellY - currentBlock.CellY);
            if (cellX < currentBlock.CellX)
                currentBlock.CellX = cellX;
            if (cellY < currentBlock.CellY)
                currentBlock.CellY = cellY;
            SlidingBlockType blockType = blockPalette.SingleOrDefault(b => b.Equals(currentType));
            if (blockType == null)
            {
                // Create and add a copy of currentType
                blockType = new SlidingBlockType(currentType, blockPalette.Count);
                blockPalette.Add(blockType);
            }
            currentBlock.BlockType = blockType;         // Current block's BlockType = matching BlockType from the palette
        }

        bool canAddBlock(object o)
        {
            // AddBlockCommand.CanExecute
            return true;
        }

        void stopAddingToBlock()
        {
            currentBlock = null;
        }

        bool canStopAddingToBlock()
        {
            return true;
        }

        void addTargetBlock(object o)
        {
            // Copy block to the target grid and add a new BlockType for it so that the target BlockType is unique for hashing purposes
            object[] args = (object[])o;
            int cellX = (int)args[0];
            int cellY = (int)args[1];
            int blockID = (int)args[2];
            clearTarget();
            // Create another BlockType clone of the target block
            SlidingBlockType targetBlockType = new SlidingBlockType(Blocks[blockID].BlockType, blockPalette.Count);
            // Add it to the palette
            blockPalette.Add(targetBlockType);
            // Update the BlockType of the target block on the main grid to this new BlockType
            Blocks[blockID].BlockType = targetBlockType;
            // Copy the block and add it to the target grid
            SlidingBlock block = new SlidingBlock(cellX, cellY, targetBlockType, blockID);
            TargetBlocks.Add(block);
        }

        bool canAddTargetBlock(object o)
        {
            return true;
        }

        void solve()
        {
            navigateSolution(new object[] { -1 });
            queue.Clear();
            visited.Clear();
            Solution.Clear();
            solutionFound = null;
            // Create intial Position and push it
            //Position.SlidingBlock[] bs = new Position.SlidingBlock[Blocks.Count];
            byte[] bs = new byte[Blocks.Count * 3];
            //for (int i = 0; i < Blocks.Count; i++)
                //bs[i] = new Position.SlidingBlock((byte)Blocks[i].CellX, (byte)Blocks[i].CellY, (byte)Blocks[i].BlockType.TypeID);
            for (int i = 0; i < Blocks.Count; i++)
            {
                bs[i * 3] = (byte)Blocks[i].CellX;
                bs[i * 3 + 1] = (byte)Blocks[i].CellY;
                bs[i * 3 + 2] = (byte)Blocks[i].BlockType.TypeID;
            }
            Position pos = new Position(bs, 0, 0, 0, null);
            queue.Enqueue(pos);
            visited.Add(pos);
            while (queue.Count > 0 && solutionFound == null)
            {
                pos = queue.Dequeue();
                for (int y = 0; y < CellsY; y++) for (int x = 0; x < CellsX; x++) occupied[x, y] = false;           // Clear the occupied grid
                for (int i = 0; i < pos.Blocks.Length; i+=3)
                    putBlock(pos.Blocks[i], pos.Blocks[i+1], blockPalette[pos.Blocks[i+2]].SubBlocks, true);              // Fill the occupied grid
                for (int i=0; i<pos.Blocks.Length;i+=3)
                {
                    SlidingBlockType block = blockPalette[pos.Blocks[i+2]];
                    if (pos.Blocks[i] > 0) tryPushMove(i, -1, 0, pos);
                    if (pos.Blocks[i+1] > 0) tryPushMove(i, 0, -1, pos);
                    if (pos.Blocks[i] + block.SizeX < CellsX) tryPushMove(i, 1, 0, pos);
                    if (pos.Blocks[i+1] + block.SizeY < CellsY) tryPushMove(i, 0, 1, pos);
                }
            }
            if (solutionFound != null)
            {
                Stack<Move> stack = new Stack<Move>();
                while (solutionFound.PreviousPosition != null)
                {
                    stack.Push(new Move(solutionFound.BlockNr, solutionFound.Xdir, solutionFound.Ydir));
                    solutionFound = solutionFound.PreviousPosition;
                }
                while (stack.Count > 0)
                    Solution.Add(stack.Pop());          // Reverse order
            }
            for (int y = 0; y < CellsY; y++) for (int x = 0; x < CellsX; x++) occupied[x, y] = false;           // Clear the occupied grid
            foreach (SlidingBlock block in Blocks)
                putBlock(block.CellX, block.CellY, block.BlockType.SubBlocks, true);              // Fill the occupied grid
        }

        bool canSolve()
        {
            return TargetBlocks.Count > 0;
        }

        void tryPushMove(int blockPos, int xdir, int ydir, Position pos)
        {
            int x = pos.Blocks[blockPos];
            int y = pos.Blocks[blockPos+1];
            int blockType = pos.Blocks[blockPos+2];
            IEnumerable<Point> subBlocks = blockPalette[blockType].SubBlocks;
            putBlock(x, y, subBlocks, false);                       // Remove it from occupied
            if (subBlocks.All(t => !occupied[x + xdir + t.X, y + ydir + t.Y]))          // Can it move?
            {
                pos.Blocks[blockPos] += (byte)xdir;
                pos.Blocks[blockPos+1] += (byte)ydir;
                Position newPos = new Position(pos.Blocks, blockPos/3, xdir, ydir, pos);       // Create newPos with the moved block
                if (blockType == TargetBlocks[0].BlockType.TypeID && pos.Blocks[blockPos] == TargetBlocks[0].CellX && pos.Blocks[blockPos+1] == TargetBlocks[0].CellY)
                    solutionFound = newPos;
                pos.Blocks[blockPos] -= (byte)xdir;
                pos.Blocks[blockPos+1] -= (byte)ydir;
                if (visited.Add(newPos))
                    queue.Enqueue(newPos);                      // Push it
            }
            putBlock(x, y, subBlocks, true);                    // Fill occupied back in
        }

        void putBlock(int cellX, int cellY, IEnumerable<Point> subBlocks, bool fill)
        {
            foreach (Point sub in subBlocks)
                occupied[cellX + sub.X, cellY + sub.Y] = fill;
        }

        bool willItFit(int cellX, int cellY, IEnumerable<Tuple<int, int>> subBlocks)
        {
            return subBlocks.All(b => cellX + b.Item1 >= 0 && cellX + b.Item1 < cellsX && cellY + b.Item2 >= 0 && cellY + b.Item2 < cellsY && !occupied[cellX + b.Item1, cellY + b.Item2]);
        }

        void clearMain()
        {
            clearTarget();          // Clear target before clearing Blocks
            Blocks.Clear();
            Array.Clear(occupied, 0, occupied.Length);
        }

        void clearTarget()
        {
            navigateSolution(new object[] { -1 });      // Navigate to start position
            if (TargetBlocks.Count == 1)
            {
                Blocks[TargetBlocks[0].BlockID].BlockType = blockPalette[blockPalette.IndexOf(TargetBlocks[0].BlockType)];
                int i = blockPalette.FindIndex(b => b.TypeID == TargetBlocks[0].BlockType.TypeID);
                blockPalette.RemoveAt(i);
            }
            TargetBlocks.Clear();
            Solution.Clear();
        }

        void navigateSolution(object o)
        {
            int index =(int)((object[])o)[0];
            while (currentSolutionIndex <= index - 1)
            {
                ++currentSolutionIndex;
                Blocks[Solution[currentSolutionIndex].BlockID].CellX += Solution[currentSolutionIndex].Xdir;
                Blocks[Solution[currentSolutionIndex].BlockID].CellY += Solution[currentSolutionIndex].Ydir;
            }
            while (currentSolutionIndex > index)
            {
                Blocks[Solution[currentSolutionIndex].BlockID].CellX -= Solution[currentSolutionIndex].Xdir;
                Blocks[Solution[currentSolutionIndex].BlockID].CellY -= Solution[currentSolutionIndex].Ydir;
                currentSolutionIndex--;
            }
        }

        bool canNavigateSolution(object o)
        {
            return true;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
