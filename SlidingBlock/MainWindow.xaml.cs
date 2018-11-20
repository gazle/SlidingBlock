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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Canvas draggedBlock;        // Used as the visual under the mouse when dragging a block from main to target
        Point dragPos;
        ICommand newGridSizeCommand;
        ICommand navigateSolutionCommand;

        public MainWindow()
        {
            InitializeComponent();
            AddHandler(BlockControl.BlockDragEvent, new BlockDragEventHandler(SlidingPuzzle_BlockDrag));         // Subscribe to the BlockControl custom routed event so we can start the drag
            PreviewDragOver += new DragEventHandler(MainWindow_PreviewDragOver);            // Tunnel version so Child gets to set the cursor last
        }

        private void Window_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            newGridSizeCommand = ((SlidingBlockPuzzle)e.NewValue).NewGridSizeCommand;
            navigateSolutionCommand = ((SlidingBlockPuzzle)e.NewValue).NavigateSolutionCommand;
        }

        void SlidingPuzzle_BlockDrag(object sender, BlockDragEventArgs e)
        {
            // Start a drag of the passed clonedBlock
            draggedBlock = e.ClonedBlock;
            dragPos = e.Position;
            Grid.SetColumnSpan(draggedBlock, 10);
            Grid.SetRowSpan(draggedBlock, 10);
            grdMain.Children.Add(draggedBlock);
            DragDropEffects effect = DragDrop.DoDragDrop(pzcMain, e, DragDropEffects.None | DragDropEffects.Link);
            // Gets here when the drag is over
            grdMain.Children.Remove(draggedBlock);
            draggedBlock = null;
        }

        void MainWindow_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Adjust the position of the draggedBlock as it's dragged
            if (draggedBlock == null) return;
            e.Effects = DragDropEffects.None;
            Point p = e.GetPosition(grdMain);
            draggedBlock.Margin = new Thickness(p.X - dragPos.X, p.Y - dragPos.Y, 100000, 100000);
        }

        private void lbxSolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = (ListBox)sender;
            int index = box.SelectedIndex;
            navigateSolutionCommand.Execute(new object[] { index });
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lbxSolution.SelectedIndex = -1;
        }

        private void tbxGrid_TextChanged(object sender, TextChangedEventArgs e)
        {
            int resX, resY;
            if (tbxGridX == null || tbxGridY == null) return;
            if (!int.TryParse(tbxGridX.Text, out resX)) return;
            if (!int.TryParse(tbxGridY.Text, out resY)) return;
            newGridSizeCommand.Execute(new object[] { resX, resY });
        }
    }
}
