using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using KickBlastJudoLogic; // Business logic layer
using System.Data.SqlClient;

namespace KickBlastJudoApp
{
    /// <summary>
    /// MainForm handles the GUI for KickBlast Judo: registering athletes,
    /// calculating fees, and performing CRUD operations against the database.
    /// </summary>
    public partial class MainForm : Form
    {
        // Connection string: ideally stored in app.config rather than hard-coded.
        private readonly string _connectionString =
            @"Data Source=DESKTOP-SSNOE6V\SQLEXPRESS;Initial Catalog=KickBlastJudoDB;Integrated Security=True";

        // Holds the currently selected athlete's ID for update/delete operations.
        private int _selectedAthleteId = -1;

        /// <summary>
        /// Constructor: initializes components and wires up load and selection events.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
            dgvResults.SelectionChanged += dgvResults_SelectionChanged;
        }

        /// <summary>
        /// On form load: populate lookup ComboBoxes and load athlete grid.
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadLookupData();
            LoadAthletesToGrid();
        }

        /// <summary>
        /// Loads training plans and weight categories from database into ComboBoxes.
        /// </summary>
        private void LoadLookupData()
        {
            // Use 'using' to ensure connection is closed/disposed.
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();

                    // Load Plans
                    DataTable dtPlans = new DataTable();
                    new SqlDataAdapter("SELECT PlanId, PlanName FROM [Plan]", con).Fill(dtPlans);
                    cmbPlan.DisplayMember = "PlanName";
                    cmbPlan.ValueMember = "PlanId";
                    cmbPlan.DataSource = dtPlans;

                    // Load Weight Categories
                    DataTable dtCats = new DataTable();
                    new SqlDataAdapter("SELECT CategoryId, CategoryName FROM WeightCategory", con).Fill(dtCats);
                    cmbCategory.DisplayMember = "CategoryName";
                    cmbCategory.ValueMember = "CategoryId";
                    cmbCategory.DataSource = dtCats;
                }
                catch (Exception ex)
                {
                    // Display user-friendly message; log full exception for diagnostics.
                    MessageBox.Show("Error loading lookup data: " + ex.Message, "Error");
                }
            }
        }

        /// <summary>
        /// Loads active athletes from database, computes cost columns, and binds to DataGridView.
        /// </summary>
        private void LoadAthletesToGrid()
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();

                    // Base query: retrieve core athlete info joined to plan/category names.
                    string sql = @"
                        SELECT a.AthleteId, a.Name, p.PlanName, a.CurrentWeightKg, c.CategoryName, a.DateJoined
                        FROM Athlete a
                        JOIN [Plan] p ON a.PlanId = p.PlanId
                        JOIN WeightCategory c ON a.CategoryId = c.CategoryId
                        WHERE a.Active = 1
                        ORDER BY a.Name";

                    DataTable dt = new DataTable();
                    new SqlDataAdapter(sql, con).Fill(dt);

                    // Add columns for computed values
                    dt.Columns.Add("TrainingCost", typeof(string));
                    dt.Columns.Add("ExtrasCost", typeof(string));
                    dt.Columns.Add("TotalCost", typeof(string));
                    dt.Columns.Add("WeightStatus", typeof(string));

                    // Prepare commands to query this month's competition and coaching data
                    SqlCommand cmdComp = new SqlCommand(
                        "SELECT COUNT(*) FROM CompetitionEntry " +
                        "WHERE AthleteId = @id AND MONTH(CompetitionDate)=@m AND YEAR(CompetitionDate)=@y", con);
                    cmdComp.Parameters.Add("@id", SqlDbType.Int);
                    cmdComp.Parameters.Add("@m", SqlDbType.Int).Value = DateTime.Today.Month;
                    cmdComp.Parameters.Add("@y", SqlDbType.Int).Value = DateTime.Today.Year;

                    SqlCommand cmdCoach = new SqlCommand(
                        "SELECT ISNULL(SUM(Hours), 0) FROM CoachingSession " +
                        "WHERE AthleteId = @id AND MONTH(SessionDate)=@m AND YEAR(SessionDate)=@y", con);
                    cmdCoach.Parameters.Add("@id", SqlDbType.Int);
                    cmdCoach.Parameters.Add("@m", SqlDbType.Int).Value = DateTime.Today.Month;
                    cmdCoach.Parameters.Add("@y", SqlDbType.Int).Value = DateTime.Today.Year;

                    // Iterate each row to compute costs
                    foreach (DataRow row in dt.Rows)
                    {
                        int athleteId = Convert.ToInt32(row["AthleteId"]);
                        string planName = row["PlanName"].ToString();
                        double weightKg = Convert.ToDouble(row["CurrentWeightKg"]);
                        string categoryName = row["CategoryName"].ToString();

                        // Query competition count for this athlete this month
                        cmdComp.Parameters["@id"].Value = athleteId;
                        int numComp = Convert.ToInt32(cmdComp.ExecuteScalar());

                        // Query coaching hours sum for this athlete this month
                        cmdCoach.Parameters["@id"].Value = athleteId;
                        double hours = Convert.ToDouble(cmdCoach.ExecuteScalar());

                        // Use business logic layer for fee calculations
                        double trainingCost = FeeCalculator.CalculateTrainingCost(planName);
                        double extrasCost = FeeCalculator.CalculateExtrasCost(planName, numComp, hours);
                        double totalCost = trainingCost + extrasCost;
                        string weightStatus = WeightHelper.Compare(weightKg, categoryName);

                        // Store formatted strings (prepend "Rs. ")
                        row["TrainingCost"] = "Rs. " + trainingCost.ToString("0.00");
                        row["ExtrasCost"] = "Rs. " + extrasCost.ToString("0.00");
                        row["TotalCost"] = "Rs. " + totalCost.ToString("0.00");
                        row["WeightStatus"] = weightStatus;
                    }

                    // Bind to DataGridView
                    dgvResults.DataSource = dt;

                    // Hide internal ID column
                    if (dgvResults.Columns.Contains("AthleteId"))
                    {
                        dgvResults.Columns["AthleteId"].Visible = false;
                    }

                    // Rename headers for clarity
                    if (dgvResults.Columns.Contains("TrainingCost"))
                        dgvResults.Columns["TrainingCost"].HeaderText = "Training Cost";
                    if (dgvResults.Columns.Contains("ExtrasCost"))
                        dgvResults.Columns["ExtrasCost"].HeaderText = "Extras Cost";
                    if (dgvResults.Columns.Contains("TotalCost"))
                        dgvResults.Columns["TotalCost"].HeaderText = "Total Cost";
                    if (dgvResults.Columns.Contains("WeightStatus"))
                        dgvResults.Columns["WeightStatus"].HeaderText = "Weight Status";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading athletes with costs: " + ex.Message, "Error");
                }
            }
            // Reset selected athlete
            _selectedAthleteId = -1;
        }

        /// <summary>
        /// Handles selection change in DataGridView: populates input controls from selected row.
        /// </summary>
        private void dgvResults_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvResults.CurrentRow == null)
            {
                return;
            }

            var drv = dgvResults.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null)
            {
                return;
            }

            // Parse selected AthleteId for future update/delete
            _selectedAthleteId = Convert.ToInt32(drv["AthleteId"]);

            // Populate Name
            txtName.Text = drv["Name"].ToString();

            // Select the appropriate plan in ComboBox
            string planName = drv["PlanName"].ToString();
            cmbPlan.SelectedIndex = cmbPlan.FindStringExact(planName);

            // Select the appropriate category in ComboBox
            string catName = drv["CategoryName"].ToString();
            cmbCategory.SelectedIndex = cmbCategory.FindStringExact(catName);

            // Populate weight value if within control bounds
            decimal weightValue = Convert.ToDecimal(drv["CurrentWeightKg"]);
            if (weightValue >= nudWeight.Minimum && weightValue <= nudWeight.Maximum)
            {
                nudWeight.Value = weightValue;
            }
            // Note: numCompetitions and nudCoaching can be loaded from DB if desired
        }

        /// <summary>
        /// Calculates and previews the fee breakdown for current input values.
        /// </summary>
        private void btnCalculate_Click(object sender, EventArgs e)
        {
            // Validates inputs
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter an athlete name.", "Input Error");
                txtName.Focus();
                return;
            }
            if (cmbPlan.SelectedItem == null)
            {
                MessageBox.Show("Please select a training plan.", "Input Error");
                cmbPlan.Focus();
                return;
            }
            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a weight category.", "Input Error");
                cmbCategory.Focus();
                return;
            }

            string planName = cmbPlan.Text;
            double weightKg = (double)nudWeight.Value;
            string categoryName = cmbCategory.Text;
            int numComp = (int)nudCompetitions.Value;
            double hours = (double)nudCoaching.Value;

            // Compute costs via business logic layer
            double trainingCost = FeeCalculator.CalculateTrainingCost(planName);
            double extrasCost = FeeCalculator.CalculateExtrasCost(planName, numComp, hours);
            double totalCost = trainingCost + extrasCost;
            string weightStatus = WeightHelper.Compare(weightKg, categoryName);

            // Show preview in MessageBox
            string msg = String.Format(
                "Training Cost: Rs. {0:0.00}\n" +
                "Extras Cost: Rs. {1:0.00}\n" +
                "Total Cost: Rs. {2:0.00}\n" +
                "Weight Status: {3}",
                trainingCost, extrasCost, totalCost, weightStatus);
            MessageBox.Show(msg, "Cost Preview");
        }

        /// <summary>
        /// Adds a new athlete record and related competition/coaching entries to the database.
        /// </summary>
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter athlete name.", "Input Error");
                txtName.Focus();
                return;
            }
            if (cmbPlan.SelectedItem == null)
            {
                MessageBox.Show("Please select a training plan.", "Input Error");
                cmbPlan.Focus();
                return;
            }
            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a weight category.", "Input Error");
                cmbCategory.Focus();
                return;
            }

            string name = txtName.Text.Trim();
            int planId = (int)cmbPlan.SelectedValue;
            double weightKg = (double)nudWeight.Value;
            int categoryId = (int)cmbCategory.SelectedValue;
            int numComp = (int)nudCompetitions.Value;
            double hours = (double)nudCoaching.Value;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();

                    // Insert athlete record using parameterized query (secure approach)
                    string insertAthleteSql =
                        "INSERT INTO Athlete (Name, PlanId, CurrentWeightKg, CategoryId) " +
                        "VALUES (@name, @planId, @weightKg, @categoryId)";
                    using (SqlCommand cmd = new SqlCommand(insertAthleteSql, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@planId", planId);
                        cmd.Parameters.AddWithValue("@weightKg", weightKg);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);
                        cmd.ExecuteNonQuery();
                    }

                    // Retrieve newly inserted AthleteId
                    int newId = Convert.ToInt32(
                        new SqlCommand("SELECT SCOPE_IDENTITY()", con).ExecuteScalar());

                    // Insert competition entries if allowed
                    string planName = cmbPlan.Text;
                    if ((planName == "Intermediate" || planName == "Elite") && numComp > 0)
                    {
                        // Parameterized INSERT in loop
                        string insertCompSql =
                            "INSERT INTO CompetitionEntry (AthleteId, CompetitionDate, Fee) " +
                            "VALUES (@athleteId, @compDate, @fee)";
                        using (SqlCommand cmdComp = new SqlCommand(insertCompSql, con))
                        {
                            cmdComp.Parameters.Add("@athleteId", SqlDbType.Int).Value = newId;
                            cmdComp.Parameters.Add("@compDate", SqlDbType.Date);
                            cmdComp.Parameters.Add("@fee", SqlDbType.Decimal).Value = 220.00m;

                            DateTime today = DateTime.Today;
                            string dateStr = today.ToString("yyyy-MM-dd");
                            for (int i = 0; i < numComp; i++)
                            {
                                cmdComp.Parameters["@compDate"].Value = dateStr;
                                cmdComp.ExecuteNonQuery();
                            }
                        }
                    }

                    // Insert coaching session if hours > 0, capping at monthly max
                    if (hours > 0)
                    {
                        // Cap hours at 20 for month
                        if (hours > 20)
                        {
                            MessageBox.Show(
                                "Private coaching hours capped at 20 for the month.",
                                "Info");
                            hours = 20;
                        }

                        string insertCoachSql =
                            "INSERT INTO CoachingSession (AthleteId, SessionDate, Hours, Rate) " +
                            "VALUES (@athleteId, @sessionDate, @hours, @rate)";
                        using (SqlCommand cmdCoach = new SqlCommand(insertCoachSql, con))
                        {
                            cmdCoach.Parameters.Add("@athleteId", SqlDbType.Int).Value = newId;
                            cmdCoach.Parameters.Add("@sessionDate", SqlDbType.Date)
                                .Value = DateTime.Today.ToString("yyyy-MM-dd");
                            cmdCoach.Parameters.Add("@hours", SqlDbType.Decimal).Value = hours;
                            cmdCoach.Parameters.Add("@rate", SqlDbType.Decimal).Value = 90.50m;
                            cmdCoach.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Athlete registered successfully.", "Success");
                }
                catch (Exception ex)
                {
                    // Log and inform user
                    MessageBox.Show("Error adding athlete: " + ex.Message, "Error");
                }
            }

            LoadAthletesToGrid();
            btnClear_Click(sender, e);
        }

        /// <summary>
        /// Searches athletes by name and displays matching results in the grid.
        /// </summary>
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string searchTerm = txtName.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                MessageBox.Show("Enter name to search.", "Input Error");
                txtName.Focus();
                return;
            }

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();

                    // Use parameterized query to avoid injection
                    string sql = @"
                        SELECT a.AthleteId, a.Name, p.PlanName, a.CurrentWeightKg, c.CategoryName, a.DateJoined
                        FROM Athlete a
                        JOIN [Plan] p ON a.PlanId = p.PlanId
                        JOIN WeightCategory c ON a.CategoryId = c.CategoryId
                        WHERE a.Active = 1 AND a.Name LIKE @searchPattern
                        ORDER BY a.Name";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@searchPattern", "%" + searchTerm + "%");
                        DataTable dt = new DataTable();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }

                        dgvResults.DataSource = dt;
                        if (dgvResults.Columns.Contains("AthleteId"))
                        {
                            dgvResults.Columns["AthleteId"].Visible = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Search error: " + ex.Message, "Error");
                }
            }

            _selectedAthleteId = -1;
        }

        /// <summary>
        /// Updates the selected athlete’s record with new input values.
        /// </summary>
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedAthleteId < 0)
            {
                MessageBox.Show("Please select an athlete from the list first.", "Selection Required");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter athlete name.", "Input Error");
                txtName.Focus();
                return;
            }
            if (cmbPlan.SelectedItem == null)
            {
                MessageBox.Show("Please select a training plan.", "Input Error");
                cmbPlan.Focus();
                return;
            }
            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a weight category.", "Input Error");
                cmbCategory.Focus();
                return;
            }

            // Read updated values
            string name = txtName.Text.Trim();
            int planId = (int)cmbPlan.SelectedValue;
            double weightKg = (double)nudWeight.Value;
            int categoryId = (int)cmbCategory.SelectedValue;

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();

                    // Use parameterized UPDATE for security
                    string updateSql =
                        "UPDATE Athlete SET Name = @name, PlanId = @planId, CurrentWeightKg = @weightKg, CategoryId = @categoryId " +
                        "WHERE AthleteId = @athleteId";
                    using (SqlCommand cmd = new SqlCommand(updateSql, con))
                    {
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@planId", planId);
                        cmd.Parameters.AddWithValue("@weightKg", weightKg);
                        cmd.Parameters.AddWithValue("@categoryId", categoryId);
                        cmd.Parameters.AddWithValue("@athleteId", _selectedAthleteId);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Athlete updated successfully.", "Success");
                        }
                        else
                        {
                            MessageBox.Show("No row updated. Perhaps data unchanged or invalid ID.", "Update Failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating athlete: " + ex.Message, "Error");
                }
            }

            // Refresh grid and clear selection
            LoadAthletesToGrid();
            btnClear_Click(sender, e);
            _selectedAthleteId = -1;
        }

        /// <summary>
        /// deletes the selected athlete by setting Active = 0.
        /// </summary>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedAthleteId < 0)
            {
                MessageBox.Show("Please select an athlete to delete.", "Selection Required");
                return;
            }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete this athlete?",
                "Confirm Delete",
                MessageBoxButtons.YesNo);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();

                    string deleteSql =
                        "UPDATE Athlete SET Active = 0 WHERE AthleteId = @athleteId";
                    using (SqlCommand cmd = new SqlCommand(deleteSql, con))
                    {
                        cmd.Parameters.AddWithValue("@athleteId", _selectedAthleteId);
                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Athlete deleted successfully.", "Success");
                        }
                        else
                        {
                            MessageBox.Show("Delete failed. Invalid AthleteId?", "Delete Failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting athlete: " + ex.Message, "Error");
                }
            }

            LoadAthletesToGrid();
            btnClear_Click(sender, e);
            _selectedAthleteId = -1;
        }

        /// <summary>
        /// Clears input controls and resets selection state.
        /// </summary>
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtName.Clear();
            cmbPlan.SelectedIndex = -1;
            nudWeight.Value = nudWeight.Minimum;
            cmbCategory.SelectedIndex = -1;
            nudCompetitions.Value = nudCompetitions.Minimum;
            nudCoaching.Value = nudCoaching.Minimum;
            _selectedAthleteId = -1;
        }

        /// <summary>
        /// Closes the application.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
