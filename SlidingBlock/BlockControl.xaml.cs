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

namespace SlidingPuzzle
{
    public delegate void BlockDragEventHandler(object sender, BlockDragEventArgs e);

    /// <summary>
    /// Interaction logic for BlockControl.xaml
    /// </summary>
    /// 
    public partial class BlockControl : UserControl
    {
        public static readonly RoutedEvent BlockDragEvent;

        public event BlockDragEventHandler BlockDrag
        {
            add { AddHandler(BlockDragEvent, value); }
            remove { RemoveHandler(BlockDragEvent, value); }
        }

        static BlockControl()
        {
            BlockDragEvent = EventManager.RegisterRoutedEvent("BlockDrag", RoutingStrategy.Bubble, typeof(BlockDragEventHandler), typeof(BlockControl));
        }

        public static readonly DependencyProperty CellXProperty = DependencyProperty.Register("CellX", typeof(int), typeof(BlockControl), new PropertyMetadata(cellXChangedCallback));
        public static readonly DependencyProperty CellYProperty = DependencyProperty.Register("CellY", typeof(int), typeof(BlockControl), new PropertyMetadata(cellYChangedCallback));

        IntToBrushConverter converter;
        public double RWidth { get { return VisualTreeHelperExtensions.FindAncestor<PuzzleControl>(this).RWidth; } }
        public double RHeight { get { return VisualTreeHelperExtensions.FindAncestor<PuzzleControl>(this).RHeight; } }
        public int XSize { get; set; }
        public int YSize { get; set; }
        public int CellX
        {
            get { return (int)GetValue(CellXProperty); }
            set { SetValue(CellXProperty, value); }
        }
        public int CellY
        {
            get { return (int)GetValue(CellYProperty); }
            set { SetValue(CellYProperty, value); }
        }

        public BlockControl()
        {
            InitializeComponent();
            converter = (IntToBrushConverter)Resources["brushConverter"];
            MouseDown += new System.Windows.Input.MouseButtonEventHandler(CanvasBlock_MouseDown);
        }

        static void cellXChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlockControl b = (BlockControl)sender;
            ContentPresenter c = (ContentPresenter)b.VisualParent;
            Canvas.SetLeft(c, (int)e.NewValue * b.RWidth);
        }

        static void cellYChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            BlockControl b = (BlockControl)sender;
            ContentPresenter c = (ContentPresenter)b.VisualParent;
            Canvas.SetTop(c, (int)e.NewValue * b.RHeight);
        }

        void CanvasBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // sender is this
            // Start dragging a visual
            Canvas clonedBlock = new Canvas();       // Clone it
            clonedBlock.Tag = Tag;                  // BlockID
            clonedBlock.IsHitTestVisible = false;     // So that elements underneath receive drag events
            clonedBlock.Opacity = 0.5;
            SolidColorBrush fillBrush = (SolidColorBrush)converter.Convert(Tag, typeof(SolidColorBrush), null, null);
            int maxX = 0; int maxY = 0;
            foreach (System.Drawing.Point p in itcView.Items)
            {
                maxX = p.X > maxX ? p.X : maxX;
                maxY = p.Y > maxY ? p.Y : maxY;
                Rectangle rect = new Rectangle() { Width = RWidth, Height = RHeight, Fill = fillBrush };
                Canvas.SetLeft(rect, p.X * RWidth);
                Canvas.SetTop(rect, p.Y * RHeight);
                clonedBlock.Children.Add(rect);
            }
            Point q = new Point((maxX + 1) * RWidth / 2, (maxY + 1) * RHeight / 2);           // Grab at midpoint
            // Raise the custom routed event. MainWindow subscribes to this to start the drag operation using the data in the EventArgs.
            RaiseEvent(new BlockDragEventArgs(BlockControl.BlockDragEvent, clonedBlock, q, maxX + 1, maxY + 1));
        }
    }

    public class BlockDragEventArgs : RoutedEventArgs
    {
        // Data for the target block being dragged
        public Canvas ClonedBlock { get; set; }
        public Point Position { get; set; }
        public int SizeX { get; set; }
        public int SizeY { get; set; }

        public BlockDragEventArgs(RoutedEvent routedEvent, Canvas clonedBlock, Point position, int sizeX, int sizeY)
            : base(routedEvent)
        {
            ClonedBlock = clonedBlock;
            Position = position;
            SizeX = sizeX;
            SizeY = sizeY;
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static T FindAncestor<T>(DependencyObject dependencyObject)
            where T : class
        {
            DependencyObject target = dependencyObject;
            do
            {
                target = VisualTreeHelper.GetParent(target);
            }
            while (target != null && !(target is T));
            return target as T;
        }
    }
}
