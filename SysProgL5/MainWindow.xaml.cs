using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ThreadsInSemaphoreSimulation.UserControls;

namespace SysProgL5
{
    
    public partial class MainWindow : Window
    {
        public ObservableCollection<Thread> idleThreads { get; set; }
        public ObservableCollection<Thread> waitingThreads { get; set; }
        public ObservableCollection<Thread> workingThreads { get; set; }
        private readonly Semaphore _semaphore;
        private decimal upDownValue;
        private int availableThreadsCount;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;


            idleThreads = new();
            waitingThreads = new();
            workingThreads = new();

            upDownValue = UpDown.Value;
            _semaphore = new(2, 10, "SEMAPHOREEEEEEEEEE");
            availableThreadsCount = 2;
        }

        private void MySimulation(object? semaphore)
        {
            if (semaphore is Semaphore s)
            {
                Thread.Sleep(3000);

                if (s.WaitOne())
                {
                    var t = Thread.CurrentThread;
                    Dispatcher.Invoke(() => waitingThreads.Remove(t));
                    Dispatcher.Invoke(() => workingThreads.Add(t));
                    Thread.Sleep(7000);
                    Dispatcher.Invoke(() => workingThreads.Remove(t));
                    s.Release();
                }
            }
        }

        private void createNewBtn_Click(object sender, RoutedEventArgs e)
        {
            var t = new Thread(MySimulation);
            t.Name = "Thread number " + (t.ManagedThreadId-10).ToString();

            idleThreads.Add(t);
        }

        private void idleList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (idleList.SelectedItem is Thread t)
            {
                idleThreads.Remove(t);

                waitingThreads.Add(t);
                t.Start(_semaphore);
            }
        }

        private void UC_NumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (sender is UC_NumericUpDown upDown)
            {
                if (upDownValue < upDown.Value)
                {
                    _semaphore?.Release();
                    availableThreadsCount++;
                }
                else
                {
                    if (availableThreadsCount == 0)
                    {
                        upDown.Value = upDownValue;
                        return;
                    }

                    availableThreadsCount--;
                    _semaphore?.WaitOne();
                }


                upDownValue = upDown.Value;
            }
        }
    }
}