using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace carbonsimulator
{
    public partial class Form1 : Form
    {
        private static double _totalCO2 = 0;
        private DistributionCenter hub;
        private List<City> cities;

        public Form1()
        {
            InitializeComponent();
            InitializeLogisticsData();
        }

        private void InitializeLogisticsData()
        {
            hub = new DistributionCenter("DSU Central Hub");
            hub.Fleet.Add(new ElectricVan("EV-LIGHT", "Eco Mini E-Courier", 2000, 3.5));
            hub.Fleet.Add(new DieselTruck("DSL-LIGHT", "Urban Diesel Light-Truck", 2000, 12.0));
            hub.Fleet.Add(new ElectricVan("EV-MEDIUM", "Urban Delivery E-Van", 6000, 3.0));
            hub.Fleet.Add(new DieselTruck("DSL-MEDIUM", "Standard Cargo Hauler", 6000, 9.0));
            hub.Fleet.Add(new ElectricVan("EV-HEAVY", "Mega E-Cargo Transporter", 15000, 2.0));
            hub.Fleet.Add(new DieselTruck("DSL-HEAVY", "Super Maxi Freight Rig", 15000, 4.5));

            cities = new List<City> {
                new City("Hyderabad", 160), new City("Sukkur", 495), new City("Multan", 945),
                new City("Lahore", 1260), new City("Islamabad", 1540), new City("Peshawar", 1690)
            };

            foreach (var city in cities)
            {
                cmbDestination.Items.Add($"Karachi to {city.Name} ({city.Distance} KM)");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            rtbFleetOverview.Text = $"=== FLEET CAPABILITIES AT {hub.CenterName.ToUpper()} ===\n" +
                                   $"-> Light Weight Category  (Up to 2,000 kg)  [EV & Diesel]\n" +
                                   $"-> Medium Weight Category (Up to 6,000 kg)  [EV & Diesel]\n" +
                                   $"-> Heavy Weight Category  (Up to 15,000 kg) [EV & Diesel]\n" +
                                   $"-----------------------------------------------------------------------------";
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            if (cmbDestination.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a destination city!", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            City dest = cities[cmbDestination.SelectedIndex];
            string cType = txtCargoType.Text.Trim();

            if (string.IsNullOrEmpty(cType))
            {
                MessageBox.Show("Cargo Type cannot be empty!", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double weight = (double)nudWeight.Value;
            if (weight <= 0)
            {
                MessageBox.Show("Invalid Weight!", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StandardCargo cargo = new StandardCargo(cType, weight);

            try
            {
                cargo.ValidateCargo(hub.GetMaxFleetCapacity());
                Vehicle selectedVehicle = hub.MatchVehicle(cargo.Weight, rdoElectric.Checked);

                if (selectedVehicle == null)
                {
                    MessageBox.Show("No matching vehicle found for this criteria.", "Allocation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                double co2 = selectedVehicle.CalculateEmissions(dest.Distance, cargo.Weight);
                double energyUsed = selectedVehicle.CalculateEnergyUsed(dest.Distance, cargo.Weight);
                _totalCO2 += co2;

                string results = $"[TRIP DISPATCHED SUCCESSFULLY]\n" +
                                 $"Route : Karachi to {dest.Name} ({dest.Distance} KM)\n" +
                                 $"Selected Vehicle Model : {selectedVehicle.DisplayName} ({selectedVehicle.VehicleId})\n";

                if (selectedVehicle.IsElectric)
                {
                    double alternativeDieselCO2 = dest.Distance * 0.25 * (1 + (cargo.Weight / selectedVehicle.MaxWeightLimit));
                    results += $"Energy Consumed : {energyUsed:F1}% Battery\n" +
                               $"Your Trip CO2 Output : 0.00 kg CO2 (Excellent!)\n" +
                               $"[COMPARATIVE REPORT] : If you had chosen a DIESEL truck, it would have emitted {alternativeDieselCO2:F2} kg CO2!\n";
                }
                else
                {
                    results += $"Fuel Consumed : {energyUsed:F1} Liters Diesel\n" +
                               $"Your Trip CO2 Output : {co2:F2} kg CO2\n" +
                               $"[COMPARATIVE REPORT] : If you had chosen an EV asset, it would have emitted 0.00 kg CO2!\n";
                }

                results += $"\n-------------------------------------------------------------\n" +
                           $"[Global Accumulated Fleet Network CO2 Footprint: {_totalCO2:F2} kg]";

                rtbEmissionResults.Text = results;
            }
            catch (OverloadException)
            {
                rtbEmissionResults.Text = $"[ERROR: EXCEEDS INFRASTRUCTURE CAPACITIES]\n" +
                                         $"{cargo.Weight} kg is too heavy for our entire commercial fleet network.\n\n" +
                                         $"[ALERT] Initiate a customized Railway Freight Line or split the cargo slots.";
                MessageBox.Show("Infrastructure Overload Triggered!", "Safety Exception Block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbDestination.SelectedIndex = -1;
            txtCargoType.Clear();
            nudWeight.Value = 0;
            rdoElectric.Checked = true;
            rtbEmissionResults.Clear();
        }

        // --- YEH HAIN WO DUMMY FUNCTIONS JO DESIGNER CRASH ROKENGE ---
        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        // -------------------------------------------------------------
    }

    public class OverloadException : Exception
    {
        public OverloadException(string message) : base(message) { }
    }

    public interface IDeliverable
    {
        string CargoType { get; }
        double Weight { get; }
        void ValidateCargo(double maxLimit);
    }

    public class StandardCargo : IDeliverable
    {
        public string CargoType { get; set; }
        public double Weight { get; set; }
        public StandardCargo(string type, double weight)
        {
            CargoType = type;
            Weight = weight < 0 ? 0 : weight;
        }
        public void ValidateCargo(double maxLimit)
        {
            if (Weight > maxLimit)
                throw new OverloadException($"[ERROR] Weight ({Weight} kg) exceeds maximum fleet limit ({maxLimit} kg)!");
        }
    }

    public abstract class Vehicle
    {
        public string VehicleId { get; set; }
        public string DisplayName { get; set; }
        public double MaxWeightLimit { get; set; }
        public double FuelEfficiency { get; set; }
        public bool IsElectric { get; set; }

        protected Vehicle(string id, string name, double maxWeight, double efficiency, bool isElectric)
        {
            VehicleId = id;
            DisplayName = name;
            MaxWeightLimit = maxWeight;
            FuelEfficiency = efficiency;
            IsElectric = isElectric;
        }
        public abstract double CalculateEmissions(double distance, double cargoWeight);
        public double CalculateEnergyUsed(double distance, double cargoWeight) => (distance / FuelEfficiency) * (1 + (cargoWeight / MaxWeightLimit));
    }

    public class DieselTruck : Vehicle
    {
        public DieselTruck(string id, string name, double maxWeight, double efficiency)
            : base(id, name, maxWeight, efficiency, false) { }
        public override double CalculateEmissions(double distance, double cargoWeight) => distance * 0.25 * (1 + (cargoWeight / MaxWeightLimit));
    }

    public class ElectricVan : Vehicle
    {
        public ElectricVan(string id, string name, double maxWeight, double efficiency)
            : base(id, name, maxWeight, efficiency, true) { }
        public override double CalculateEmissions(double distance, double cargoWeight) => 0.0;
    }

    public class City
    {
        public string Name { get; set; }
        public double Distance { get; set; }
        public City(string name, double dist) { Name = name; Distance = dist; }
    }

    public class DistributionCenter
    {
        public string CenterName { get; set; }
        public List<Vehicle> Fleet { get; set; } = new List<Vehicle>();
        public DistributionCenter(string name) => CenterName = name;

        public Vehicle MatchVehicle(double weight, bool wantsElectric)
        {
            foreach (var v in Fleet)
            {
                if (v.IsElectric == wantsElectric && v.MaxWeightLimit >= weight) return v;
            }
            return null;
        }

        public double GetMaxFleetCapacity()
        {
            double max = 0;
            foreach (var v in Fleet) if (v.MaxWeightLimit > max) max = v.MaxWeightLimit;
            return max;
        }
    }
}