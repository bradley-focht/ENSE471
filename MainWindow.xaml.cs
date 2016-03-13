using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Text.RegularExpressions;

namespace WpfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //private variables that i don't know where to put
        private cesEntities ces;    //EF entity
        SolidColorBrush defaultBack = new SolidColorBrush(Color.FromRgb(5, 20, 35));
        SolidColorBrush midToneBack = new SolidColorBrush(Color.FromRgb(10, 40, 70));
        SolidColorBrush hlightBack = new SolidColorBrush(Color.FromRgb(20, 80, 200));
        SolidColorBrush hlightLightBack = new SolidColorBrush(Color.FromRgb(120, 200, 255));
        private int selectedId; //save the state for selected button on a map

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ces = new cesEntities();

            //initial screen setup ... such a bad way of doing it
            dckLogin.Visibility = Visibility.Visible;
            
            dckMap.Visibility = Visibility.Collapsed;
            dckTopButtons.Visibility = Visibility.Collapsed;
            btnNavPri.Visibility = Visibility.Collapsed;
            btnNavSec.Visibility = Visibility.Collapsed;
            btnNavTitle.Visibility = Visibility.Collapsed;

            //set background colour
            dckMain.Background = defaultBack;
            selectedId = 0;
 
            //grid margin 
            gridMap.Margin = new Thickness(10, 10, 10, 10);
            gridMap.Background = new  SolidColorBrush(Color.FromRgb(5, 20, 35));

            //build menu
            map_buildPriMenu();
            //clear map of default colours
            map_clearColours();

            //create title section (in code of course!)
            map_buildPriMenuTitleSel();

            //clear the table
            table_buildCanadaWideAggregation();
        }

        //map functions
        void map_ColourPrograms(int prog_id)    //send the id of a university program to colour by
        {
            using (var context = new cesEntities())
            {

                //get data -> province and demand for the program
                var demands = from d in ces.job_demand
                              join p in ces.provinces on d.province_id equals p.id
                              where d.program_id == prog_id
                              select new { d.demand, p.acronym };


                //heat map colours
                foreach (var d in demands) {
                    byte r = 0;
                    byte g = 0;
                    byte b = 0;


                    //RED
                    if (d.demand < 0)
                    {
                        if (d.demand <= -50)
                            r = (byte)(300 + d.demand * 2);
                        else if (d.demand > -50 && d.demand < 0)
                        {
                            r = 200;
                        }
                    } else if (d.demand > 50)
                    {
                        r = (byte)(-50 + d.demand);
                    }

                    //BLUE
                   if (d.demand < -50) {                       
                            b = 100;
                    }
                    else if (d.demand > 50)
                    {
                        g = (byte)(-100 + d.demand*2);
                    }


                    //GREEN
                    if (d.demand >0)
                    {
                        g = (byte)(100 + 1.05*d.demand);   
                    } else if (d.demand < 0 && d.demand > -50) {
                        g = (byte)(50 + d.demand * 2);
                    }

                    //demand == 0
                    if (d.demand == 0)
                    {
                        r = g = b = 200;
                    }

                    

                    //colour the map province by province with gigantor switch cause i don't know how to do data binding
                    SolidColorBrush c = new SolidColorBrush(Color.FromRgb(r, g, b));
                    switch (d.acronym)
                    {
                        case "ab":
                            fillAb(c);
                            break;
                        case "bc":
                            fillBc(c);
                            break;
                        case "mb":
                            fillMb(c);
                            break;
                        case "nb":
                            fillNb(c);
                            break;
                        case "ns":
                            fillNs(c);
                            break;
                        case "nf":
                            fillNf(c);
                            break;
                        case "pe":
                            fillPe(c);
                            break;
                        case "on":
                            fillOn(c);
                            break;
                        case "qc":
                            fillPq(c);
                            break;
                        case "sk":
                            fillSk(c);
                            break;
                        case "Yk":
                            fillYk(c);
                            break;
                        case "Nv":
                            fillNvt(c);
                            break;
                        case "Nw":
                            fillNwt(c);
                            break;
                    }
                }
            }
        }   
        void map_clearColours()
        {
            SolidColorBrush nearWhite = new SolidColorBrush(Color.FromRgb(225, 225, 225));
            fillBc(nearWhite);
            fillAb(nearWhite);
            fillSk(nearWhite);
            fillMb(nearWhite);

            fillPq(nearWhite);
            fillOn(nearWhite);
            fillNf(nearWhite);
            fillNb(nearWhite);
            fillNs(nearWhite);
            fillPe(nearWhite);
            fillNwt(nearWhite);
            fillNvt(nearWhite);
            fillYk(nearWhite);
        }   //clear the whole map


        private void map_buildPriMenu(int clickedBtn=0)
        {
            //clear the menu and start again
            btnNavPri.Children.Clear();

               
            //get data from the db
            using (var context = new cesEntities())
            {
                var programs = from p in ces.programs
                               where p.parent == 0
                               select new { p.name, p.id };

                //make the buttons
                foreach (var p in programs)
                {
                    Button b = new Button();
                    b.Name = "btnPri_" + p.id;
                    b.Content = p.name;
                    b.Style = this.FindResource("btnUnselStyle") as Style;

                    if (clickedBtn == p.id)
                        b.Background = hlightBack;
                    else
                        b.Background = midToneBack;
                    b.Click += btnPri_Click;
                    btnNavPri.Children.Add(b);
                }
            }
        }
        private void map_buildSecMenu(int parentId, int clickedBtn=0)
        {
            btnNavSec.Children.Clear();
            using (var context = new cesEntities())
            {
                var programs = from p in ces.programs
                               where p.parent == parentId
                               select new { p.name, p.id };

                foreach (var p in programs)
                {
                    Button b = new Button();
                    b.Name = "btnPri_" + p.id + "_" + parentId;
                    b.Click += btnSec_Click;
                    b.Content = p.name;
                    b.Style = this.FindResource("btnUnselStyle") as Style;
                    if (clickedBtn == p.id)
                        b.Background = hlightLightBack;
                    else
                        b.Background = hlightBack;

                    btnNavSec.Children.Add(b);
                }
            }
        }
        private void map_buildPriMenuTitleSel()
        {
            //add a title... i guess use a button?

            Button bTitle = new Button();
            bTitle.Width = 100;
            bTitle.Content = "Program";
            bTitle.Style = this.FindResource("btnUnselStyle") as Style;
            bTitle.Background = new SolidColorBrush(Color.FromRgb(40, 160, 255));
            bTitle.ToolTip = "Select a province below, then click on the map to the right";

            TextBlock t = new TextBlock();
            t.Text = " Year ";
            t.Width = 40;
            t.VerticalAlignment = VerticalAlignment.Center;
            t.Foreground = new SolidColorBrush(Color.FromRgb(250, 250, 125));
            

            ComboBox cmb = new ComboBox();
            cmb.Name = "cmbYear";
            cmb.Items.Add("2020");
            cmb.Width = 60;
            cmb.SelectedIndex = 0;
            

            btnNavTitle.Children.Add(bTitle);
            btnNavTitle.Children.Add(t);
            btnNavTitle.Children.Add(cmb);

        }

        //table in top right corner
        //sending a zero hides the table - still takes up the space, not collapsed
        private void table_buildCanadaWideAggregation(int progId=0) {
            if (progId == 0) {
                Section s = new Section();
                Paragraph p0 = new Paragraph(new Run("Canada Education Stragy"));
                Paragraph p1 = new Paragraph(new Run("Select program then click on the provinces to view information"));
                Paragraph p2 = new Paragraph(new Run("Other useful info..... hardcoded in"));
                Paragraph p3 = new Paragraph(new Run("cause there is no better way..."));
                p0.Foreground = p1.Foreground = p2.Foreground = p3.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 100));
                p0.FontSize += 10;
                s.Blocks.Add(p0);
                s.Blocks.Add(p1);
                s.Blocks.Add(p2);
                s.Blocks.Add(p3);
                s.TextAlignment = TextAlignment.Left;
                flowDoc.Blocks.Add(s);
            }
            
        }
        //context menu
        private void showPopupMenu(string prov, string acronym)
        {
            if (selectedId == 0)
                return;

            ContextMenu c = new ContextMenu();
            c.Style = this.FindResource("cxMenuStyle") as Style;
            //title


            using (var context = new cesEntities())
            {
                //demand for the province info
                var provId = (from p in ces.provinces
                              where p.acronym == acronym
                              select p.id).First();

                var demands = (from d in ces.job_demand
                               where d.province_id == provId && d.program_id == selectedId
                               select new { d.demand }).First();

                Label l = new Label();
                l.FontSize = 15;
                l.FontWeight = FontWeights.Bold;
                l.Foreground = new SolidColorBrush(Color.FromRgb(0, 25, 90));
                l.Content = prov;

                //title item
                MenuItem mTitle = new MenuItem();
                mTitle.Header = l;
                mTitle.Background = new SolidColorBrush(Color.FromRgb(220, 240, 255));
                mTitle.IsEnabled = false;

                //new menu item
                MenuItem dem = new MenuItem();

                if (demands.demand > 0)
                {
                    dem.Header = "Demand: +" + demands.demand;
                    dem.Background = new SolidColorBrush(Color.FromRgb(150, 200, 150));
                }
                else if (demands.demand < 0)
                {
                    dem.Header = "Demand: " + demands.demand;
                    dem.Background = new SolidColorBrush(Color.FromRgb(200, 150, 150));
                }
                else {
                    dem.Header = "Demand: 0";
                    dem.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                }
                //add stuff 
                c.Items.Add(mTitle);
                c.Items.Add(dem);

                //jobs data stuff
                MenuItem m = new MenuItem();
                MenuItem n = new MenuItem();
                MenuItem o = new MenuItem();

                m.Items.Add(n);
                n.Items.Add(o);
                m.Header = "want this job?";
                n.Header = "go to this school";
                o.Header = "here is some more info";
                c.StaysOpen = true;

                c.Items.Add(m);

            }


            c.IsOpen = true;

        }



        //event handlers go here
        private void btnSec_Click(object sender, RoutedEventArgs e) //first level of button on the map
        {
            Button b = (Button)sender;

            Match m = new Regex(@"\d+").Match(b.Name);

            selectedId = int.Parse(m.Value.ToString());

            int parentId = int.Parse(m.NextMatch().Value);
            map_buildSecMenu(parentId, selectedId);
            map_clearColours();
            map_ColourPrograms(selectedId);
        }
        private void btnPri_Click(object sender, RoutedEventArgs e)
        {
            selectedId = 0;

            Button b = (Button)sender;   

            Match m = new Regex(@"\d+").Match(b.Name);
            int id = int.Parse(m.Value);

            map_buildPriMenu(id);
            map_buildSecMenu(id);
            map_clearColours();
        }//second level button on the map
        private void btnEducation_Click(object sender, RoutedEventArgs e)
        {
            dckMap.Visibility = Visibility.Collapsed;
        }
        private void btnMap_Click(object sender, RoutedEventArgs e)
        {
            dckMap.Visibility = Visibility.Visible;
        }
        private void btnJobs_Click(object sender, RoutedEventArgs e)
        {
            dckMap.Visibility = Visibility.Collapsed;
        }
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            dckLogin.Visibility = Visibility.Collapsed;

            dckMap.Visibility = Visibility.Visible;
            dckTopButtons.Visibility = Visibility.Visible;
            btnNavPri.Visibility = Visibility.Visible;
            btnNavSec.Visibility = Visibility.Visible;
            btnNavTitle.Visibility = Visibility.Visible;
        }

        //province colouring
        //some provinces have islands
        private void fillPe(SolidColorBrush c)
        {
            pei.Fill = c;
        }
        private void fillPq(SolidColorBrush c)
        {
            qc.Fill = c;
            qc_qi.Fill = c;
        }
        private void fillNvt(SolidColorBrush c)
        {
            nv.Fill = c;
            nv_ti10.Fill = c;
            nv_ti2.Fill = c;
            nv_ti3.Fill = c;
            nv_ti4.Fill = c;
            nv_ti5.Fill = c;
            nv_ti6.Fill = c;
            nv_ti7.Fill = c;
            nv_ti8.Fill = c;
            nv_ti9.Fill = c;
            nv_tvi.Fill = c;
            nv_bfi.Fill = c;
            nv_ei.Fill = c;
        }
        private void fillBc(SolidColorBrush c)
        {
            bc.Fill = c;
            bc_hg.Fill = c;
        }
        private void fillNwt(SolidColorBrush c)
        {
            nw.Fill = c;
            nw_tv.Fill = c;
            nw_bki.Fill = c;
            nw_mi.Fill = c;
            nw_ai.Fill = c;
            nw_ai2.Fill = c;
        }
        private void fillYk(SolidColorBrush c)
        {
            yk.Fill = c;
        }
        private void fillNs(SolidColorBrush c)
        {
            ns.Fill = c;
        }
        private void fillNf(SolidColorBrush c)
        {
            nf.Fill = c;
        }
        private void fillNb(SolidColorBrush c)
        {
            nb.Fill = c;
        }
        private void fillOn(SolidColorBrush c)
        {
            on.Fill = c;
        }
        private void fillSk(SolidColorBrush c)
        {
            sk.Fill = c;
        }
        private void fillMb(SolidColorBrush c)
        {
            mb.Fill = c;
        }
        private void fillAb(SolidColorBrush c)
        {
            ab.Fill = c;
        }



  
        //Provincial/Territory mousedown events
        private void bc_MouseDown(object sender, MouseEventArgs e)
        {
            showPopupMenu("British Columbia", "bc");
        }
        private void yk_MouseDown(object sender, MouseEventArgs e)
        {
            showPopupMenu("Yukon", "yk");
        }
        private void ab_MouseDown(object sender, MouseEventArgs e)
        {
            showPopupMenu("Alberta", "ab");
        }
        private void sk_MouseDown(object sender, MouseEventArgs e)
        {
            showPopupMenu("Saskatchewan", "sk");
        }
        private void nwt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Northwest Territories", "nw");
        }
        private void pei_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Prince Edward Island", "pe");
        }
        private void nf_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Newfoundland and Labrador", "nf");
        }
        private void ns_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Nova Scotia", "ns");
        }
        private void nb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("New Brunswick", "nb");
        }
        private void nvt_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Nunavut", "nv");
        }
        private void pq_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Quebec", "qc");
        }
        private void mb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Manitoba", "mb");
        }
        private void on_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showPopupMenu("Ontario", "on");
        }


    }
}
