using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;

namespace WpfTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		//private variables that i don't know where to put
		private newCesModel ces;    //EF entity
		SolidColorBrush defaultBack = new SolidColorBrush(Color.FromRgb(5, 20, 35));
		SolidColorBrush midToneBack = new SolidColorBrush(Color.FromRgb(10, 40, 70));
		SolidColorBrush hlightBack = new SolidColorBrush(Color.FromRgb(20, 80, 200));
		SolidColorBrush hlightLightBack = new SolidColorBrush(Color.FromRgb(120, 200, 255));
		SolidColorBrush redSolidColorBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
		private SolidColorBrush saveBrush = new SolidColorBrush();
		ColorAnimation saveAnimation = new ColorAnimation();
	
		//saved for the map
		private int selectedId; //save the state for selected button on a map
		
		//Jobs Data variables, easier to figure out
		private int jbProvSelectedId;
		private int jbProgSelectedId;

		//education data variables
		private int edUnivSelectedId;
		private int edProgSelectedId;

		//login info
		private String username;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			ces = new newCesModel();

			//initial screen setup ... such a bad way of doing it
			grdLogin.Visibility = Visibility.Visible;
			dckMap.Visibility = Visibility.Collapsed;
			grdTopButtons.Visibility = Visibility.Collapsed;
			btnNavPri.Visibility = Visibility.Collapsed;
			btnNavSec.Visibility = Visibility.Collapsed;
			btnNavTitle.Visibility = Visibility.Collapsed;
			dckPowerData.Visibility = Visibility.Collapsed;

			//set background colour
			dckMain.Background = defaultBack;
			selectedId = 0;
			edUnivSelectedId = 0;
			edProgSelectedId = 0;

			//STUFF TO COMMENT OUT FOR SEAN SO HE CAN USE THIS APP TOO
			//Education data buttons
			ed_buildPriMenu();
			BuildProgramEntry();
			BuildUniv();
			map_buildPriMenu();

			//clear the table
			//table_buildCanadaWideAggregation();
			//grid margin 
			gridMap.Margin = new Thickness(10, 10, 10, 10);
			dckMain.Background = defaultBack;

			//clear map of default colours
			map_clearColours();

			//create title section (in code of course!)
			map_buildPriMenuTitleSel();

			BuildDataEntryGrids();
			SetupSaveAnimations();
		}

		private void SetupSaveAnimations()
		{
			saveBrush = new SolidColorBrush();
			saveBrush.Color = Colors.Blue;

			saveAnimation.From = midToneBack.Color;
			saveAnimation.To = Colors.Green;
			saveAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
			saveAnimation.AutoReverse = true;

			saveBrush.BeginAnimation(SolidColorBrush.ColorProperty, saveAnimation);

			btnEducationSave.Background = saveBrush;
			btnJobsSave.Background = saveBrush;
		}

		/// <summary>
		/// builds the province buttons on the education screen
		/// </summary>
		/// <param name="clickedBtn">highlighted button</param>
		private void ed_buildPriMenu(int clickedBtn = 0)
		{
			//clear the menu and start again
			navScreen.Children.Clear();

			//get data from the db
			using (var context = new newCesModel())
			{
				var provinces = from p in ces.provinces
								orderby p.name ascending
								select new { p.name, p.id };

				//make the buttons
				foreach (var p in provinces)
				{
					Button b = new Button();
					b.Name = "btnProv_" + p.id;
					b.Content = p.name;

					if (username != "somebody" || username == null)
					{
						if (clickedBtn == p.id)
						{       //highlight button selection
							b.Background = hlightBack;

						}
						else
							b.Background = midToneBack;

					} else {
						if (p.name != "Saskatchewan")
						{
							b.IsEnabled = false;
						}
						else
							b.Background = hlightBack;
					}

						b.Click += btnEdProv_Click;
						navScreen.Children.Add(b);
					
				}
			}
		}
		/// <summary>
		/// event handler for province buttons on education screen
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnEdProv_Click(object sender, RoutedEventArgs e)
		{
			Button b = (Button)sender;

			Match m = new Regex(@"\d+").Match(b.Name);

			jbProvSelectedId = int.Parse(m.Value.ToString());

			ed_buildPriMenu(jbProvSelectedId);

			BuildUniv();
		}

		/// <summary>
		/// Does anything needed to initialize the data entry grids
		/// </summary>
		private void BuildDataEntryGrids()
		{
			cmbProjection.Items.Add("2020");
			cmbProjection.Items.Add("2050");
		}
		/// <summary>
		/// Adds univeristy programs to the cmbDiscipline combobox in the Jobs screen
		/// call this method from the window load i guess
		/// </summary>
		private void BuildProgramEntry()
		{
			using (var context = new newCesModel())
			{
				var programs = from p in ces.programs
							   where p.parent == 0
							   select new { p.name, p.id };

				//add a bogey
				ComboBoxItem b = new ComboBoxItem();
				ComboBoxItem b2 = new ComboBoxItem();		//can't reuse the same, has to be detached
				b.Content = b2.Content = "---select---";
				b.Name = b2.Name = "bogey";
				cmbDiscipline.Items.Add(b);
				cmbRelatedCollege.Items.Add(b2);

				foreach (var p in programs)
				{
					ComboBoxItem c = new ComboBoxItem();
					ComboBoxItem c2 = new ComboBoxItem();
					c.Content = c2.Content = p.name;
					c.Name = c2.Name = "cmbProgramId_" + p.id.ToString();

					cmbDiscipline.Items.Add(c);
					cmbRelatedCollege.Items.Add(c2);
				}
			}
			cmbDiscipline.SelectedValue = cmbDiscipline.Items[0];
			cmbDiscipline.SelectedValue = cmbDiscipline.Items[0];
		}

		//map functions
		void map_ColourPrograms(int prog_id)    //send the id of a university program to colour by
		{
			using (var context = new newCesModel())
			{

				//get data -> province and demand for the program
				var demands = from d in ces.job_demand
							  join p in ces.provinces on d.province_id equals p.id
							  where d.program_id == prog_id
							  select new { d.demand, p.acronym };


				//heat map colours
				foreach (var d in demands)
				{
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
					}
					else if (d.demand > 50)
					{
						r = (byte)(-50 + d.demand);
					}

					//BLUE
					if (d.demand < -50)
					{
						b = 100;
					}
					else if (d.demand > 50)
					{
						b = (byte)(-150 + d.demand * 3);
					}


					//GREEN
					if (d.demand > 0)
					{
						g = (byte)(100 + 1.05 * d.demand);
					}
					else if (d.demand < 0 && d.demand > -50)
					{
						//g = 0;
						g = (byte)(50 + d.demand);
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
						case "yk":
							fillYk(c);
							break;
						case "nv":
							fillNvt(c);
							break;
						case "nw":
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


		private void map_buildPriMenu(int clickedBtn = 0)
		{
			//clear the menu and start again
			btnNavPri.Children.Clear();


			//get data from the db
			using (var context = new newCesModel())
			{
				var programs = from p in ces.programs
							   orderby p.name ascending
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
		private void map_buildSecMenu(int parentId, int clickedBtn = 0)
		{
			btnNavSec.Children.Clear();
			using (var context = new newCesModel())
			{
				var programs = from p in ces.programs
							   where p.parent == parentId
							   orderby p.name ascending
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


		/// <summary>
		/// This is the context menu that pops up for each province
		/// </summary>
		/// <param name="prov"></param>
		/// <param name="acronym"></param>
		private void showPopupMenu(string prov, string acronym)
		{
			if (selectedId == 0)
				return;

			ContextMenu c = new ContextMenu();
			c.Style = this.FindResource("cxMenuStyle") as Style;
			//title

			using (var context = new newCesModel())
			{
				//demand for the province info
				var provId = (from p in ces.provinces
							  where p.acronym == acronym
							  select p.id).First();

				var demand = (from d in ces.job_demand
							   where d.province_id == provId && d.program_id == selectedId
							   orderby d.id descending
							   select d).FirstOrDefault();

				var universities = from u in ces.universities
								   join p in ces.provinces on u.province_id equals p.id
								   where p.acronym.Equals(acronym)
								   orderby u.name ascending
								   select u;
				var programs = from w in ces.university_programs
							   where w.program_id == selectedId
							   orderby w.id descending
							   select w;
				

				List < MenuItem > arr = new List<MenuItem>();
				foreach (var prog in programs)
				{
					MenuItem y = new MenuItem();

					StackPanel st = new StackPanel();
					st.Orientation = Orientation.Vertical;
					y.Header = st;

					TextBlock t2 = new TextBlock();
					t2.Text = "Enrollment " + prog.current_enrollment + " of " + prog.available_seats + "  Seats ";

					y.Name = "id_" + prog.university_id.ToString();
					y.Header = t2;

					arr.Add(y);
				}


				if (demand == null)
				{
					return;
				}
				else { }

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

				if (demand.demand > 0)
				{
					dem.Header = "Demand: +" + demand.demand;
					dem.Background = new SolidColorBrush(Color.FromRgb(150, 200, 150));
				}
				else if (demand.demand < 0)
				{
					dem.Header = "Demand: " + demand.demand;
					dem.Background = new SolidColorBrush(Color.FromRgb(200, 150, 150));
				}
				else {
					dem.Header = "Demand: 0";
					dem.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
				}
				//add stuff 
				c.Items.Add(mTitle);
				c.Items.Add(dem);

				//Education stuff
				MenuItem m = new MenuItem();
				m.Header = "Universities";
				m.Style = this.FindResource("cxMenuItemStyle") as Style;
				m.Width = c.Width;
				foreach (var u in universities)
				{
					MenuItem i = new MenuItem();
					i.Header = u.name;
					m.Items.Add(i);
					foreach (MenuItem g in arr)
					{
						if (g.Name == "id_" + u.id)
						{
							i.Items.Add(g);
							break;
						}
					}	
				}
				c.Items.Add(m);

				//Jobs stuff
				MenuItem j = new MenuItem();
				j.Header = "Average Wage: $" + demand.wage;
				c.Items.Add(j);
				MenuItem k = new MenuItem();
				k.Header = "Employed: " + demand.employment;
				c.Items.Add(k);

			}
			c.IsOpen = true;
		}

		/// <summary>
		/// Shows the screen corresponding to the sender of the event.
		/// All screen panels hide initially then the correct screen becomes visible
		/// 
		/// The DataEntry dockpanel contains both the Education and Jobs Grid and shares 
		/// the left hand navigation bar
		/// </summary>
		/// <param name="sender">Object sending the event</param>
		private void ShowScreen(object sender)
		{
			grdLogin.Visibility = Visibility.Collapsed;
			dckMap.Visibility = Visibility.Collapsed;
			dckDataEntry.Visibility = Visibility.Collapsed;
			grdEducation.Visibility = Visibility.Collapsed;
			dckPowerData.Visibility = Visibility.Collapsed;
			grdJobs.Visibility = Visibility.Collapsed;

			if (sender.Equals(btnEducation))
			{
				dckDataEntry.Visibility = Visibility.Visible;
				grdEducation.Visibility = Visibility.Visible;
			}
			else if (sender.Equals(btnMap))
			{
				dckMap.Visibility = Visibility.Visible;
			}
			else if (sender.Equals(btnJobs))
			{
				dckDataEntry.Visibility = Visibility.Visible;
				grdJobs.Visibility = Visibility.Visible;
			}
			else if (sender.Equals(btnLogin))
			{
				//totally the right place for this code
				if (txtUsername.Text == "")
				{
					username = "nobody";

				}
				else {
					username = "somebody";        //i have no idea how username keeps getting set. 
					ed_buildPriMenu();
				}

				dckMap.Visibility = Visibility.Visible;
				grdTopButtons.Visibility = Visibility.Visible;
				btnNavPri.Visibility = Visibility.Visible;
				btnNavSec.Visibility = Visibility.Visible;
				btnNavTitle.Visibility = Visibility.Visible;
			}
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

		/// <summary>
		/// Event for buttons in the primary navigation (and LogIn) being selected
		/// </summary>
		/// <param name="sender">Button sending the event</param>
		/// <param name="e"></param>
		private void ChangeScreen(object sender, RoutedEventArgs e)
		{
			ShowScreen(sender);
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

		/// <summary>
		/// Attempts to save job data to the database
		/// Prompt displayed showing save status
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SubmitJobsData(object sender, RoutedEventArgs e)
		{
			int demand = 0;
			int employment = 0;
			long wage = 0;
			try
			{
				demand = int.Parse(txtForecast.Text)%100;
				employment = int.Parse(txtCurrentEmployment.Text)%100;
				wage = long.Parse(txtAverageSalary.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Please ensure Forecast, Employment, and Salary Data is valid.");
				return;
			}

			if (demand > 100)
				txtForecast.Text = "100";


			if (rdDown.IsChecked == true)
				demand = -demand;

			using (var context = new newCesModel())
			{
				var j = new job_demand();
				j.job_field_id = 1;
				j.program_id = jbProgSelectedId;
				j.province_id = jbProvSelectedId;
				j.forecast_year = 2020;
				j.demand = demand;
				j.wage = wage;
				j.employment = employment;

				context.job_demand.Add(j);
				if (context.SaveChanges() > 0)
				{
					saveBrush.BeginAnimation(SolidColorBrush.ColorProperty, saveAnimation);
					ClearJobsData(sender, e);
				}
				else
				{
					MessageBox.Show("Error saving changes.");
				}
			}

		}
		/// <summary>
		/// Clears all fields in the jobs grid
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ClearJobsData(object sender, RoutedEventArgs e)
		{
			cmbDiscipline.SelectedValue = cmbDiscipline.Items[0];
			cmbField.SelectedItem = cmbField.Items[0];
			cmbProjection.SelectedItem = null;
			rdUp.IsChecked = false;
			rdDown.IsChecked = false;
			txtForecast.Text = String.Empty;
			txtCurrentEmployment.Text = String.Empty;
			txtAverageSalary.Text = String.Empty;
			
		}

		/// <summary>
		/// Attempts to save education data to database.
		/// Prompt displayed showing save status.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void SubmitEducationData(object sender, RoutedEventArgs e)
		{
				using (var context = new newCesModel())
				{
					var j = new university_programs();
					j.program_id = edProgSelectedId;
					j.university_id = edUnivSelectedId;
					try {
						j.available_seats = long.Parse(txtSeats.Text);
						j.current_enrollment = long.Parse(txtCurrentEnrollment.Text);
					} catch (Exception exc)
					{
						MessageBox.Show("invalid data entered");
					}
					context.university_programs.Add(j);
					if (context.SaveChanges() > 0)
					{
						saveBrush.BeginAnimation(SolidColorBrush.ColorProperty, saveAnimation);
						ClearJobsData(sender, e);
					}
					else
					{
						MessageBox.Show("Error saving changes.");
					}
				}
				ClearEducationData(sender, e);

				//Run this when a save is made... Thanks
				saveBrush.BeginAnimation(SolidColorBrush.ColorProperty, saveAnimation);

		}

		/// <summary>
		/// Clears all fields in the education grid
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ClearEducationData(object sender, RoutedEventArgs e)
		{
			cmbUniversity.SelectedValue = cmbUniversity.Items[0];

			cmbRelatedProgram.SelectedItem = cmbRelatedProgram.Items[0];
			cmbRelatedCollege.SelectedItem = cmbRelatedCollege.Items[0];
			txtSeats.Text = String.Empty;
			txtCurrentEnrollment.Text = String.Empty;
			txtGraduatesPerYear.Text = String.Empty;
			txtJobAttainment.Text = String.Empty;
			txtTuition.Text = String.Empty;
		}
		/// <summary>
		/// Selected item in the discipline combo box on the jobs screen
		/// it fills the fields combo box below it using the BuildFieldEntry()
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void cmbDiscipline_SelectChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (ComboBoxItem s in e.AddedItems) //i just want the first item but whatever
			{
				if (s.Name == "bogey")
					return;
				Match m = new Regex(@"\d+").Match(s.Name);
				BuildFieldEntry(int.Parse(m.Value.ToString()));
				return;
			}
		}
		/// <summary>
		/// builds the field combo box on the jobs scren
		/// </summary>
		/// <param name="progId"></param>
		private void BuildFieldEntry(int progId = 0)
		{
			cmbField.Items.Clear();
			if (progId == 0)
				return;
			else
			{
				using (var context = new newCesModel())
				{
					var programs = from p in ces.programs
								   where p.parent == progId
								   select new { p.name, p.id };

					//add a bogey
					ComboBoxItem b = new ComboBoxItem();
					b.Content = "---select---";
					b.Name = "bogey";
					cmbField.Items.Add(b);

					foreach (var p in programs)
					{
						ComboBoxItem c = new ComboBoxItem();
						c.Content = p.name;
						c.Name = "cmbFieldId_" + p.id.ToString();

						cmbField.Items.Add(c);
					}
				}
			}
			cmbField.SelectedItem = cmbField.Items[0];
		}

		/// <summary>
		/// university list on educatin page. builds list based on selected province in jbProvSelectedId variable
		/// </summary>
		private void BuildUniv()
		{
			cmbUniversity.Items.Clear();

			ComboBoxItem i = new ComboBoxItem();
			i.Content = "---select---";
			i.Name = "bogey";
			cmbUniversity.Items.Add(i);

			cmbUniversity.SelectedValue = cmbUniversity.Items[0];

			{
				using (var context = new newCesModel())
				{
					var universities = from p in ces.universities
									   where p.province_id == jbProvSelectedId
									   select new { p.name, p.id };

					//add a bogey
					ComboBoxItem b = new ComboBoxItem();
					b.Content = "---select---";
					b.Name = "bogey";
					cmbField.Items.Add(b);

					foreach (var u in universities)
					{
						ComboBoxItem c = new ComboBoxItem();
						c.Content = u.name;
						c.Name = "cmbFieldId_" + u.id.ToString();

						cmbUniversity.Items.Add(c);
					}
				}
			}

		}
		private void BuildDisc()
		{
			using (var context = new newCesModel())
			{
				var disciplines = from d in ces.university_programs
								  where d.university_id == edUnivSelectedId
								  select new { d.program_id, d.program.name };
				foreach (var d in disciplines)
				{
					ComboBoxItem c = new ComboBoxItem();
					c.Content = d.name;
					c.Name = "cmbDiscItem_" + d.program_id;
					cmbRelatedProgram.Items.Add(c);
				}
			}
		}
		private void cmbField_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (ComboBoxItem s in e.AddedItems) //i just want the first item but whatever
			{
				if (s.Name == "bogey")
					return;

				Match m = new Regex(@"\d+").Match(s.Name);
				jbProgSelectedId = int.Parse(m.Value.ToString());
				return;
			}
		}
		/// <summary>
		/// education screen, select disciplines available at this university
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void cmbUniversity_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (ComboBoxItem s in e.AddedItems) //no .First() method available, can't use [0].property because type is just object
			{
				if (s.Name == "bogey")
					return;
				
				Match m = new Regex(@"\d+").Match(s.Name);
				
				edUnivSelectedId = int.Parse(m.Value.ToString());
				return;
			}
		}

		private void cmbRelatedCollege_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (ComboBoxItem s in e.AddedItems) //no .First() method available, can't use [0].property because type is just object
			{
				if (s.Name == "bogey")
					return;

				Match m = new Regex(@"\d+").Match(s.Name);
				int id = int.Parse(m.Value.ToString());
				BuildEdFields(id);
				return;
			}		
		}

		void BuildEdFields(int progId=0)
		{
			cmbRelatedProgram.Items.Clear();
			if (progId == 0)
				return;
			else
			{
				using (var context = new newCesModel())
				{
					var programs = from p in ces.programs
								   where p.parent == progId
								   select new { p.name, p.id };

					//add a bogey
					ComboBoxItem b = new ComboBoxItem();
					b.Content = "---select---";
					b.Name = "bogey";
					cmbRelatedProgram.Items.Add(b);

					foreach (var p in programs)
					{
						ComboBoxItem c = new ComboBoxItem();
						c.Content = p.name;
						c.Name = "cmbFieldId_" + p.id.ToString();

						cmbRelatedProgram.Items.Add(c);
					}
				}
			}
			cmbRelatedProgram.SelectedItem = cmbRelatedProgram.Items[0];
		}

		private void cmbRelatedProgram_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (ComboBoxItem s in e.AddedItems) //no .First() method available, can't use [0].property because type is just object
			{
				if (s.Name == "bogey")
					return;

				Match m = new Regex(@"\d+").Match(s.Name);
				int id = int.Parse(m.Value.ToString());
				edProgSelectedId = id;

			}
		}

		private void ValidateInt(object sender, RoutedEventArgs e)
		{
			TextBox txt = sender as TextBox;
			int t;
			if (txt != null && (!int.TryParse(txt.Text, out t) && txt.Text != String.Empty))
			{
				txt.BorderBrush = redSolidColorBrush;
				txt.BorderThickness = new Thickness(2);
			}
			else
			{
				txt.BorderBrush = Brushes.Transparent;
				txt.BorderThickness = new Thickness(0);
			}
		}

		private void ValidateLong(object sender, RoutedEventArgs e)
		{
			TextBox txt = sender as TextBox;
			long t;
			if (txt != null && (!long.TryParse(txt.Text, out t) && txt.Text != String.Empty))
			{
				txt.BorderBrush = redSolidColorBrush;
				txt.BorderThickness = new Thickness(2);
			}
			else
			{
				txt.BorderBrush = Brushes.Transparent;
				txt.BorderThickness = new Thickness(0);
			}
		}

		private void ImportData(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("TODO: Brad implement data import wizard.");
		}

		private void ExportData(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("TODO: Sean implement data export wizard.");
		}

		private void btnShowPowerData_Click(object sender, RoutedEventArgs e)
		{
			dckPowerData.Visibility = Visibility.Visible;
		}

		private void btnHidePowerData_Click(object sender, RoutedEventArgs e)
		{
			dckPowerData.Visibility = Visibility.Collapsed;
		}
	}
}
