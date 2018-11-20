using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;

namespace SlidingPuzzle
{
    /// <summary>
    /// Interaction logic for PuzzleControl.xaml
    /// </summary>
    public partial class PuzzleControl : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(PuzzleControl),
            new PropertyMetadata(propertyChangedCallback));
        public static readonly DependencyProperty GridWidthProperty = DependencyProperty.Register("GridWidth", typeof(int), typeof(PuzzleControl), new PropertyMetadata(sizeChangedCallback));
        public static readonly DependencyProperty GridHeightProperty = DependencyProperty.Register("GridHeight", typeof(int), typeof(PuzzleControl), new PropertyMetadata(sizeChangedCallback));
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public int GridWidth
        {
            get { return (int)GetValue(GridWidthProperty); }
            set { SetValue(GridWidthProperty, value); }
        }
        public int GridHeight
        {
            get { return (int)GetValue(GridHeightProperty); }
            set { SetValue(GridHeightProperty, value); }
        }
        public double RWidth { get { return ActualWidth / GridWidth; } }
        public double RHeight { get { return ActualHeight / GridHeight; } }
        public bool IsMain { get; set; }
        bool draggingNewBlock;
        ICommand addNewBlockCommand;
        ICommand addToBlockCommand;
        ICommand stopAddingToBlockCommand;
        ICommand addTargetBlockCommand;

        public PuzzleControl()
        {
            InitializeComponent();
            IsMain = false;
            rctTarget.Visibility = Visibility.Hidden;
            DataContextChanged += new DependencyPropertyChangedEventHandler(PuzzleControl_DataContextChanged);
            PreviewDragOver += new DragEventHandler(PuzzleControl_PreviewDragOver);
            PreviewDragLeave += new DragEventHandler(PuzzleControl_PreviewDragLeave);
            PreviewDrop += new DragEventHandler(PuzzleControl_PreviewDrop);
        }

        static void sizeChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PuzzleControl visual = (PuzzleControl)sender;
            visual.Width = visual.GridWidth * 40;
            visual.Height = visual.GridHeight * 40;
            visual.InvalidateVisual();
        }

        static void propertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PuzzleControl p = (PuzzleControl)sender;
            ((ItemsControl)p.cvsRoot.Children[0]).ItemsSource = (IEnumerable)e.NewValue;
        }

        void PuzzleControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            addNewBlockCommand = ((SlidingBlockPuzzle)DataContext).AddNewBlockCommand;
            addToBlockCommand = ((SlidingBlockPuzzle)DataContext).AddToBlockCommand;
            stopAddingToBlockCommand = ((SlidingBlockPuzzle)DataContext).StopAddingToBlockCommand;
            addTargetBlockCommand = ((SlidingBlockPuzzle)DataContext).AddTargetBlockCommand;
        }

        void PuzzleControl_PreviewDrop(object sender, DragEventArgs e)
        {
            // Drop the target block on the target grid
            BlockDragEventArgs a = (BlockDragEventArgs)e.Data.GetData(typeof(BlockDragEventArgs));
            if (a == null) return;
            Canvas block = a.ClonedBlock;
            if (!IsMain)
            {
                // Dropping block on target grid
                Point p = e.GetPosition(this);
                int x, y;
                dropPosition(block, p, a.SizeX, a.SizeY, out x, out y);
                addTargetBlockCommand.Execute(new object[] { x, y, block.Tag });        // Tag = BlockID
                CommandManager.InvalidateRequerySuggested();                            // Force reevalution of canExecute
                rctTarget.Visibility = Visibility.Hidden;
            }
        }

        void PuzzleControl_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Draw the target rectangle
            BlockDragEventArgs a = (BlockDragEventArgs)e.Data.GetData(typeof(BlockDragEventArgs));
            if (a == null) return;
            Canvas block = a.ClonedBlock;
            if (IsMain)
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.Link;
                Point p = e.GetPosition(this);
                int x, y;
                dropPosition(block, p, a.SizeX, a.SizeY, out x, out y);
                rctTarget.Width = a.SizeX * RWidth;      // block.Width & Height are Auto (NaN)
                rctTarget.Height = a.SizeY * RHeight;
                Canvas.SetLeft(rctTarget, x * RWidth);
                Canvas.SetTop(rctTarget, y * RHeight);
                rctTarget.Visibility = Visibility.Visible;
            }
        }

        void dropPosition(Canvas block, Point p, int SizeX, int SizeY, out int x, out int y)
        {
            // Get the cell position in terms of mouse position
            p.X -= SizeX * RWidth / 2;
            p.Y -= SizeY * RHeight / 2;
            x = (int)Math.Round(p.X / RWidth);
            y = (int)Math.Round(p.Y / RHeight);
            x = x < 0 ? 0 : x;
            y = y < 0 ? 0 : y;
            x = Math.Min(x, GridWidth - SizeX);
            y = Math.Min(y, GridHeight - SizeY);
        }

        void PuzzleControl_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (!IsMain)
            {
                rctTarget.Visibility = Visibility.Hidden;
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            for (int row = 0; row < GridHeight; row++)
                for (int col = 0; col < GridWidth; col++)
                {
                    double x = col * RWidth;
                    double y = row * RHeight;
                    dc.DrawRectangle(Brushes.LightGray, new Pen(Brushes.DarkGray, 3), new Rect(x, y, RWidth, RHeight));
                }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            // Start creation of a new block on main grid
            base.OnMouseDown(e);
            if (!IsMain || !(e.Source == this)) return;
            Point p = e.GetPosition(this);
            int cellX = (int)Math.Floor(p.X * GridWidth / ActualWidth);
            int cellY = (int)Math.Floor(p.Y * GridHeight / ActualHeight);
            if (cellX >= 0 && cellX < GridWidth && cellY >= 0 && cellY < GridHeight)
            {
                draggingNewBlock = true;
                CaptureMouse();
                addNewBlockCommand.Execute(new object[] { cellX, cellY });
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // Continue the create of a block on main grid
            base.OnMouseMove(e);
            if (!draggingNewBlock) return;
            Point p = e.GetPosition(this);
            int cellX = (int)Math.Floor(p.X * GridWidth / ActualWidth);
            int cellY = (int)Math.Floor(p.Y * GridHeight / ActualHeight);
            if (cellX >= 0 && cellX < GridWidth && cellY >= 0 && cellY < GridHeight)
            {
                addToBlockCommand.Execute(new object[] { cellX, cellY });
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            // End the creation of a block on main grid
            base.OnMouseUp(e);
            if (!draggingNewBlock) return;
            stopAddingToBlockCommand.Execute(null);
            draggingNewBlock = false;
            ReleaseMouseCapture();      // Release mouse
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            // End the creation of a block on main grid
            base.OnMouseLeave(e);
            if (!draggingNewBlock) return;
            stopAddingToBlockCommand.Execute(null);
            draggingNewBlock = false;
            ReleaseMouseCapture();      // Release mouse
        }
    }
}
