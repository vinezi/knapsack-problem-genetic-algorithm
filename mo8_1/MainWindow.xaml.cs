using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace mo8_1
{
    public partial class MainWindow : Window
    {
        BackgroundWorker bw;
        private static int maximumPermissibleWeight, numberOfItems, numberOfIndividuals, probabilityOfMutation, numberOfTheBest, percentageOfQuality, numberOfRuns, 
            allItemPrice, allItemWeight;
        
        ObservableCollection<Item> rowDataItems = new ObservableCollection<Item>();
        ObservableCollection<Item> rowDataSelectedItems = new ObservableCollection<Item>();

        ObservableCollection<Chromosome> rowPopulation = new ObservableCollection<Chromosome>();
        ObservableCollection<Chromosome> rowsTheBestPopulation = new ObservableCollection<Chromosome>();
        public MainWindow()
        {
            InitializeComponent();
        }

        #region GeneticAlgoritmFunction  
        private int getQuality(int weight, int price)
        {
            int result;
            if (weight <= maximumPermissibleWeight)
                result = 100;
            else
                result = maximumPermissibleWeight - weight;
            result += price * 100 / allItemPrice;

            if (result < 0)
                result = 0;
            else
                result /= 2;
            return result;
        }
        private void mutation()
        {
            Random rand = new Random();
            int editNum;
            string patern;
            foreach (Chromosome item in rowPopulation)
            {
                if (rand.Next(101) <= probabilityOfMutation)
                {
                    editNum = rand.Next(0, numberOfItems);
                    string right = item.chromosome.Substring(editNum + 1);
                    patern = item.chromosome[editNum] == '0' ? "1" : "0";
                    item.chromosome = item.chromosome.Remove(editNum) + patern + right;
                }
            }
            updateData();
        }
        private void crossing()
        {
            string parent1, parent2;
            int firstSelect, secondSelect;
            Random rand = new Random();
            List<Chromosome> crossList = rowPopulation.ToList();
            for (int i = 0; i < (crossList.Count / 2); i++)
            {
                firstSelect = rand.Next(0, crossList.Count);    //0 and crossList.Count-1
                secondSelect = rand.Next(0, crossList.Count);
                parent1 = crossList[firstSelect].chromosome;
                parent2 = crossList[secondSelect].chromosome;
                int cut = parent1.Length / 2;
                string parent1Right = parent1.Substring(cut);
                string parent2Right = parent2.Substring(cut);
                crossList[firstSelect].chromosome = parent1.Remove(cut) + parent2Right;
                crossList[secondSelect].chromosome = parent2.Remove(cut) + parent1Right;
                
            }
            rowPopulation = new ObservableCollection<Chromosome>(crossList);
            updateData();
        }
        #endregion

        #region Events
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F4)
            {
                status.Background = new SolidColorBrush(Color.FromRgb(90, 33, 204));
                textStatus.Text = "Running";
                valueMax.Text = "20";
                valueNum.Text = "5";
                valuePopulation.Text = "20";
                valueBest.Text = "1";
                valueMutation.Text = "5";
                valueStopCycle.Text = "10";
                valueStopPercent.Text = "90";
                generateThings();
                generatePopulation();
                Calculate();
            }
            if (e.Key == Key.F9)
            {
                valueMax.Text = "30";
                valueNum.Text = "50";
                valuePopulation.Text = "2000";
                valueBest.Text = "1";
                valueMutation.Text = "5";
                valueStopCycle.Text = "5000";
                valueStopPercent.Text = "70";
                generateThings();
                generatePopulation();
                Calculate();
            }
        }
        private void generateItems_Click(object sender, RoutedEventArgs e)
        {
            generateThings();
        }

        private void generatePopulationBtn_Click(object sender, RoutedEventArgs e)
        {
            generatePopulation();
        }

        private void calculate_Click(object sender, RoutedEventArgs e)
        {
            Calculate();
        }
        private void dataGTheBestInPopulation_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dataGTheBestInPopulation.SelectedItem != null)
            {
                Chromosome chromosome = (Chromosome)dataGTheBestInPopulation.SelectedItem;
                showItems(chromosome.chromosome);
            }
        }
        private void dataGPopulation_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dataGPopulation.SelectedItem != null)
            {
                Chromosome chromosome = (Chromosome)dataGPopulation.SelectedItem;
                showItems(chromosome.chromosome);
            }
           
        }
        #endregion

        #region ButtonFunction
        private void Calculate()
        {
            if (chekingInput("start"))
            {
                bw = new BackgroundWorker();

                // this allows our worker to report progress during work
                // это позволяет нашему воркеру сообщать о прогрессе во время работы
                bw.WorkerReportsProgress = true;

                // what to do in the background thread
                // что делать в фоновом потоке
                bw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    BackgroundWorker b = o as BackgroundWorker;

                    crossing();
                    mutation();
                    saveBest();
                    for (int i = 0; i < numberOfRuns; i++)
                    {
                        b.ReportProgress(i * 100 / (numberOfRuns - 1));
                        crossing();
                        mutation();
                        if (compareWithTheBest())
                        {
                            saveBest();
                        }
                        if (rowsTheBestPopulation.ToList()[0].percent >= percentageOfQuality)
                        {
                            MessageBox.Show("Завершение по достижению требуемого процента");
                            break;
                        }
                        if (i == numberOfRuns - 1)
                        {
                            MessageBox.Show("Завершение по количеству выполнений");
                        }

                        //Application.Current.Dispatcher.Invoke(new Action(() =>
                        //{
                        //    status.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                        //    textStatus.Text = "Ready";
                        //}));
                    }
                    RefreshDatagrids();



                });
                bw.ProgressChanged += new ProgressChangedEventHandler(
                delegate (object o, ProgressChangedEventArgs args)
                {
                    textStatus.Text = string.Format("{0}% Completed", args.ProgressPercentage);
                    //status.Background = new SolidColorBrush(Color.FromRgb(255, 122, 204));
                    status.Background = new SolidColorBrush(Color.FromRgb(202, 81, 0));

                    //RefreshDatagrids();
                });
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                delegate (object o, RunWorkerCompletedEventArgs args)
                {
                    status.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                    textStatus.Text = "Finished!";
                });
                
                bw.RunWorkerAsync();
            }
            else
                MessageBox.Show("biba");           
        }



        private void generateThings()
        {
            if (chekingInput("backpack"))
            {
                rowDataItems.Clear();
                int priceTmp, weightTmp;
                int priceTotal = 0, weightTotal = 0;

                Random rnd = new Random();
                for (int i = 0; i < numberOfItems; i++)
                {
                    priceTmp = rnd.Next(1, 15);
                    weightTmp = rnd.Next(1, 20);
                    rowDataItems.Add(new Item { name = "Item " + rnd.Next(501), price = priceTmp, weight = weightTmp });

                    priceTotal += priceTmp;
                    weightTotal += weightTmp;
                }
                allItemPrice = priceTotal;
                allItemWeight = weightTotal;
                priceInfo.Text = priceTotal.ToString();
                weightInfo.Text = weightTotal.ToString();
                dataGRBackpack.ItemsSource = rowDataItems;
            }
        }
        private void generatePopulation()
        {
            if(chekingInput("population")){
                rowPopulation.Clear();
                Random rnd = new Random();
                long rndArg = (long)Math.Pow(2, numberOfItems), tmp;
                string chromosome;
                for (int i = 0; i < numberOfIndividuals; i++)
                {
                    tmp = (long)(rnd.NextDouble() * rndArg);
                    chromosome = Convert.ToString(tmp, 2);
                    rowPopulation.Add(new Chromosome { name = "name " + rnd.Next(5000), chromosome = addNonSignificantZero(chromosome) });
                }
                dataGPopulation.ItemsSource = rowPopulation;
                updateData();
            }
        }
        #endregion

        #region HelperFunction
        private void updateData()
        {
            int weightTmp, priceTmp;
            foreach (Chromosome item in rowPopulation)
            {
                weightTmp = calculateWeight(item.chromosome);
                priceTmp = calculatePrice(item.chromosome);
                item.weight = weightTmp;
                item.price = priceTmp;
                item.percent = getQuality(weightTmp, priceTmp);
            }

            //dataGPopulation.Items.Refresh();
        }
        private void saveBest()
        {
            //rowsTheBestPopulation.Clear();
            rowsTheBestPopulation = new ObservableCollection<Chromosome>();
            List<Chromosome> SortedList = rowPopulation.ToList().OrderByDescending(o => o.percent).ToList();
            for (int i = 0; i < numberOfTheBest; i++)
            {
                rowsTheBestPopulation.Add(SortedList[i]);
            }
            //dataGTheBestInPopulation.ItemsSource = rowsTheBestPopulation;
            //dataGTheBestInPopulation.Items.Refresh();
        }
        private bool compareWithTheBest()
        {
            List<Chromosome> SortedList = rowPopulation.ToList().OrderByDescending(o => o.percent).ToList();
            return SortedList[0].percent >= rowsTheBestPopulation.ToList()[0].percent;
        }
        private void showItems(string chromosome)
        {
            rowDataSelectedItems.Clear();
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (chromosome[i] == '1')
                    rowDataSelectedItems.Add(rowDataItems.ToList()[i]);
            }
            dataGSelected.ItemsSource = rowDataSelectedItems;
            weightSelectedInfo.Text = calculateWeight(chromosome).ToString();
            priceSelectedInfo.Text = calculatePrice(chromosome).ToString();
        }
        private int calculateWeight(string chromosome)
        {
            int weight = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (chromosome[i] == '1')
                {
                    weight += rowDataItems.ToList()[i].weight;
                }
            }
            return weight;
        }
        private int calculatePrice(string chromosome)
        {
            int price = 0;
            for (int i = 0; i < chromosome.Length; i++)
            {
                if (chromosome[i] == '1')
                {
                    price += rowDataItems.ToList()[i].price;
                }
            }
            return price;
        }
        private string addNonSignificantZero(string chromosome)
        {
            string completeChromosome = chromosome;
            if (chromosome.Length < numberOfItems)
            {
                for (int i = 0; i < numberOfItems - chromosome.Length; i++)
                {
                    completeChromosome = "0" + completeChromosome;
                }
            }
            return completeChromosome;
        }
        private bool chekingInput(string test)
        {
            bool result = true;
            switch (test)
            {
                case "backpack":
                    if (!int.TryParse(valueNum.Text, out numberOfItems) || !int.TryParse(valueMax.Text, out maximumPermissibleWeight))
                        result = false;
                    break;
                case "population":
                    if (!int.TryParse(valuePopulation.Text, out numberOfIndividuals) || !int.TryParse(valueBest.Text, out numberOfTheBest)
                        || !int.TryParse(valueMutation.Text, out probabilityOfMutation) || rowDataItems.Count == 0)
                        result = false;
                    goto case "backpack";
                case "start":
                    if (!int.TryParse(valueStopPercent.Text, out percentageOfQuality) || !int.TryParse(valueStopCycle.Text, out numberOfRuns) ||
                        rowDataItems.Count == 0 || rowPopulation.Count == 0)
                        result = false;
                    goto case "population";
                default:
                    result = false;
                    break;
            }
            if (!result)
            {
                textStatus.Text = "Error!";
                status.Background = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            }
            else
            {
                textStatus.Text = "Ready";
                status.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            }
            return result;
        }
        #endregion


        private void RefreshDatagrids()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                dataGPopulation.Items.Refresh();
                dataGTheBestInPopulation.ItemsSource = rowsTheBestPopulation;
                dataGTheBestInPopulation.Items.Refresh();
            }));
        }
    }
}
