/* Курдюков Алексей, 21.08.2013 */
//картинки отсюда: http://findicons.com/pack/1722/gnome_2_18_icon_theme
using System;
using System.Diagnostics;
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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NCanvas ncanvas;
        public MainWindow()
        {            
            InitializeComponent();
            ncanvas = new NCanvas(canvas);
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;
            Debug.Indent();
            Debug.WriteLine("started");            
            cbColor1.SelectedItem = cbColor1.Items[1];
            cbColor2.SelectedItem = cbColor1.Items[2];
            cbLineThickness.SelectedItem = cbLineThickness.Items[2];
        }

        private void btnAddRect_Click(object sender, RoutedEventArgs e)
        {
            if (sender == btnAddRect)
                ncanvas.ToolMode = NCanvas.NToolMode.Rect;
            else if (sender == btnSelect)
                ncanvas.ToolMode = NCanvas.NToolMode.Select;
            else if (sender == btnDel)
                ncanvas.ToolMode = NCanvas.NToolMode.Delete;
            else if (sender == btnScale)
                ncanvas.ToolMode = NCanvas.NToolMode.Scale;
            else if (sender == btnRotate)
                ncanvas.ToolMode = NCanvas.NToolMode.Rotate;
            else if (sender == btnLine)
                ncanvas.ToolMode = NCanvas.NToolMode.Line;
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ncanvas.MouseUp(Mouse.GetPosition(canvas));
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            ncanvas.MouseMove(Mouse.GetPosition(canvas), e.LeftButton);
        }

        private void cbColor1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == cbColor1) ncanvas.ColorStart = (Color)((sender as ComboBox).SelectedItem);
            else if (sender == cbColor2) ncanvas.ColorStop = (Color)((sender as ComboBox).SelectedItem);
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("click: " + e.ClickCount);
            ncanvas.MouseDown(Mouse.GetPosition(canvas), Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), e.ClickCount==2);
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ncanvas.Reset();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();            
            dialog.FileName = "untitled";
            dialog.DefaultExt = ".npaint";
            dialog.Filter = "npaint (.npaint)|*.npaint";
            if (dialog.ShowDialog(this) == true)
            {
                ncanvas.SaveToFile(dialog.FileName);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "untitled";
            dialog.DefaultExt = ".npaint";
            dialog.Filter = "npaint (.npaint)|*.npaint";
            if (dialog.ShowDialog(this) == true)
            {
                ncanvas.LoadFromFile(dialog.FileName);
            }
        }

        private void canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ncanvas.ToolMode = NCanvas.NToolMode.Move;
        }

        private void cbLineThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ncanvas.LineThickness = (int)((sender as ComboBox).SelectedItem);
        }  
    }
}
