/*
This program is created by Joshua Hulin [Contractor] in contract with Presure Systems International [Client].

The software is provided by the Contractor and any contributors “AS IS” and any express or implied warranties, including,
but not limited to, the implied warranties of merchantability and fitness for a particular purpose are disclaimed. In no 
event shall the Contractor or any contributors be liable to the Client or any third-party for any damages (including but 
not limited to, procurement of substitute goods or services, loss of use, lost profits, lost data, lost savings, business 
interruption, or other incidental, consequential or special damages.) however caused and on any theory of liability, 
whether in contract, strict liability, or tort (including negligence or otherwise) arising in any way out of the use of 
this software, even if advised of the possibility of such damages. 
The Contractor cannot be held responsible for the use of this software by the Client or any third-party obtaining a copy 
of the software, in source or binary form, whether in part or whole, with or without modification, in an unlawful way or 
in valuation with any local, regional, or international regulations.
In no event shall the Contractor or any contributors be liable to the Client or any third-party for hurt or injury 
incurred in the use of this software or any related test apparatus, whether or not the injury occurs through negligence 
or otherwise.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Phidget22;
using MetroSet_UI.Forms;
using System.Threading;
using MetroSet_UI.Controls;
using System.Security.Cryptography;
using Phidget22.Events;
using ScottPlot.Drawing.Colormaps;

namespace ECB_Testing_Program
{
    public partial class me : MetroSetForm
    {
        // TODO MVP: Create ECB Testing Vars in settings
        // TODO MVP: Add Start/Stop control to plot
        // TODO MVP: Bring no_conn image to the front, when done setting up the form
        // TODO: Reduce processing time in VoltageCange event handeler
        // TODO: Simplify up PhidgetTree by reading in all nodes the same if posible
        // TODO: Add controls to when a phidget has been opened to the controls tab
        // TODO: Make the PSI icon white
        // TODO: Expand phidget viewTree
        // TODO: Add light theam
        List<Phidget> avalible_phidgets = new List<Phidget>();
        PhidgetTree phidgetTree;
        // Create a manger object of Phidges
        Manager man = new Manager();
        List<MetroSetComboBox> inChannels = new List<MetroSetComboBox>();
        List<MetroSetComboBox> outChannels = new List<MetroSetComboBox>();
        int dataInterval = 250; // This is the sample rate in milisecounds
        List<PhidgetStream> all_streams = new List<PhidgetStream>();
        List<DigitalOutput> digitalOutputs = new List<DigitalOutput>();
        DateTime startTime;
        List<ScottPlot.Plottable.SignalPlotXY> signalPlots = new List<ScottPlot.Plottable.SignalPlotXY>();
        List<double[]> values = new List<double[]>();
        List<double[]> times = new List<double[]>();
        List<Button> btns_calib = new List<Button>();
        List<MetroSetCheckBox> ckbs_calib = new List<MetroSetCheckBox> ();
        double gain = 1;
        double offset = 0;
        Dictionary<int, double> gains = new Dictionary<int, double>();
        Dictionary<int, double> offsets = new Dictionary<int, double>();
        

        int maxDataCount = 6000;
        bool ignoreVoltageChange = true;

        int currentCalibrationRow;

        int currentNumeric;
        bool ignoreCellChange = false;
        bool hasVoltageListener = false;

        public me()
        {
            // TODO: Remove after testing
            double[] x = { 1, 2, 3, 6, 8 };
            double[] y = {5.03, 8.02, 11.12, 19.97, 27.106};
            
            // Initialize the user form componaints
            InitializeComponent();
            txbEquation.Text = Properties.Settings.Default.equation;
            
            pnl_calibration.Visible = false;
            pnl_calibration.BringToFront();
            pnl_nocon.BringToFront();
            bgwColorMe.RunWorkerAsync();


            // Change the display type of the plot area. This has to be doned
            // here insted of in InitializeComponent b5ecause Windose form
            // generator dose not recognize this function
            plot.Plot.Style(ScottPlot.Style.Gray2);
            plot_calibration.Plot.Style(ScottPlot.Style.Gray2);
            //plot.Plot.Frameless();
            //plot_calibration.Plot.Frameless();
            plot.Plot.Palette = ScottPlot.Palette.OneHalfDark;
            plot_calibration.Plot.Palette = ScottPlot.Palette.OneHalfDark;

            // Set the ECB button to match user settings
            btnIsECB.Checked = Properties.Settings.Default.IsECB;
            if (!Properties.Settings.Default.IsECB)  // Hide the ECB UI panels if not in ECB mode
            {
                panECBcontrol.Visible = false;
                panECBsetUp.Visible = false;
            }


            inChannels.Add(cbxSupplyTank);
            inChannels.Add(cbxECBpressDown);
            inChannels.Add(cbxECBpressUp);
            inChannels.Add(cbxFlow);
            inChannels.Add(cbxDeliveryTank);
            
            outChannels.Add(cbxSolenoidUp);
            outChannels.Add(cbxECBsolenoid);
            outChannels.Add(cbxECBvent);
            outChannels.Add(cbxTankVent);

            btns_calib.Add(btn_calib_tank);
            btns_calib.Add(btn_calib_up);
            btns_calib.Add(btn_calib_down);
            btns_calib.Add(btn_calib_delivery);
            btns_calib.Add(btn_calib_flow);

            ckbs_calib.Add(ckb_supply);
            ckbs_calib.Add(ckb_up);
            ckbs_calib.Add(ckb_down);
            ckbs_calib.Add(ckb_delivery);
            ckbs_calib.Add(ckb_flow);

            getSavedCalibrations();
            setSavedNumerics();
           


            // Set up tags to hold phidgets for each ECB combobox control
            cbxSupplyTank.Tag = new List<Phidget>();
            cbxSolenoidUp.Tag = new List<Phidget>();
            cbxFlow.Tag = new List<Phidget>();
            cbxECBvent.Tag = new List<Phidget>();
            cbxECBsolenoid.Tag = new List<Phidget>();
            cbxECBpressUp.Tag = new List<Phidget>();
            cbxECBpressDown.Tag = new List<Phidget>();
            cbxDeliveryTank.Tag = new List<Phidget>();
            cbxTankVent.Tag = new List<Phidget>();

            // Register for event listener with the manager object before calling open
            man.Attach += Man_Attach;
            man.Detach += Man_Detach;
            treeView1.Nodes.Clear();
            phidgetTree = new PhidgetTree(treeView1);


            // TODO: Remove when finished testing
            if (false)
            {
                pictureBox1.Image = null;
            }

            // Set up the Phidges magiger that will detect attach/detach events
            try
            {
                man.Open();
            }
            catch (PhidgetException ex)
            {
                Console.WriteLine("Failure: " + ex.Message);
            }
        }

        // Phidget event handelers
        #region Phidget Event Handelers       
        //
        // Attach event runs when a Phidget device is detected to have been plugged in
        //
        private void Man_Attach(object sender, Phidget22.Events.ManagerAttachEventArgs e)
        {
            // Access event source via the sender object
            Manager man = (Manager)sender;

            // Access event data via the EventArgs
            Phidget channel = e.Channel;
            phidgetTree.update(channel);
            
            // Console.WriteLine(channel.Parent.DeviceID +  ", " + channel.IsHubPortDevice + ", " + channel.HubPort);


                // Add the now channel to our lists
                avalible_phidgets.Add(channel);
                addECBChanel(channel);

                // Check to see if there are any canels avalible
                if (avalible_phidgets.Count() > 0)
                {
                    // Make the no connection panel invisable
                    pnl_nocon.Visible = false;
                }
                else
                {
                    pnl_nocon.Visible = true;
                }
        }
        //
        // Detach event runs when a Phidget device has been unpluged or diconnected
        //
        private void Man_Detach(object sender, Phidget22.Events.ManagerDetachEventArgs e)
        {
            // Access event source via the sender object
            Manager man = (Manager)sender;

            // Access event data via the EventArgs
            Phidget channel = e.Channel;
            phidgetTree.remove(channel);

            // Remove the disconnected channel from our list
            foreach (Phidget phg in avalible_phidgets.ToList())
            {
                if (phg.ToString() == channel.ToString())
                {
                    avalible_phidgets.Remove(phg);
                    removeECBChanel(phg);
                }
            }


            // Check to see if there are any canels avalible
            if (avalible_phidgets.Count() > 0)
            {
                // Make the no connection panel invisable
                pnl_nocon.Visible = false;
            }
            else
            {
                pnl_nocon.Visible = true;
            }

        }


        //
        // This event fires when a vlotage input changes
        //
        private void voltageChange(object sender, VoltageInputVoltageChangeEventArgs e)
        {
            if (!ignoreVoltageChange)
            {
                // Get the sender as voltageInput
                VoltageInput v = (VoltageInput)sender;
                double voltage;
                // get the time and value of the change event
                try
                {
                    voltage = v.Voltage;
                }
                catch (PhidgetException ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
                TimeSpan duration = DateTime.Now - startTime;
                int n = 0;
                foreach (PhidgetStream pStream in all_streams)
                {
                    bool ignore = false;
                    if (pStream.getName() != "Calculated Tank Pressure" && pStream.phidget.ChannelClass == ChannelClass.VoltageInput)
                    {
                        if (isSame(v, pStream.phidget)) // Identify what stream has changed voltage
                        {
                            pStream.addPoint(voltage, duration.TotalSeconds);
                        }
                        else
                        {
                            try
                            {
                                VoltageInput thisVI = pStream.phidget as VoltageInput;
                                pStream.addPoint(thisVI.Voltage, duration.TotalSeconds);
                            }
                            catch (PhidgetException ex)
                            {
                                Console.WriteLine(ex);
                                ignore = true;
                            }
                        }
                        if (!ignore)
                        {
                            // determain if target voltage or time
                            if (rbnITP.Checked || rbnDTP.Checked)
                            {
                                // Check to see if pressure is reached on the delivery tank pressure
                                if ((pStream.getName() == "Delivery Tank Pressure") && (pStream.val.Last() > metroSetNumeric1.Value) && swcECBsolenoid.Switched)
                                {
                                    swcECBsolenoid.Switched = false;
                                }
                            }
                            else
                            {
                                // Check to see if time is reached
                                if (pStream.t.Last() > metroSetNumeric1.Value)
                                {
                                    swcECBsolenoid.Switched = false;
                                }
                            }

                            values[n][pStream.val.Length - 1] = pStream.val.Last();
                            times[n][pStream.t.Length - 1] = pStream.t.Last();


                            // Refresh the plot
                            int maxPlotIndex = pStream.val.Length - 1;

                            signalPlots[n].MaxRenderIndex = maxPlotIndex;
                            plot.Plot.AxisAuto();
                            plot.Render();
                        }
                    }
                    else if(pStream.getName() == "Calculated Tank Pressure")
                    {
                        // This case it is the calculated value
                        if (!ignore)
                        {
                            double pd=0;
                            double ps=0;
                            foreach(PhidgetStream p in all_streams)
                            {
                                if(p.getName() == "Downstream ECB Presure")
                                {
                                    pd = p.val.Last();
                                } else if(p.getName() == "Upstream ECB Pressure")
                                {
                                    ps = p.val.Last();
                                }
                            }
                            double[] constants = getEquationValues(txbEquation.Text);
                            pStream.addCaculatedPoint(duration.TotalSeconds, pd, ps, constants[0], constants[1], constants[2]);
                            
                            values[n][pStream.val.Length - 1] = pStream.val.Last();
                            times[n][pStream.t.Length - 1] = pStream.t.Last();

                            // Refresh the plot
                            int maxPlotIndex = pStream.val.Length - 1;

                            signalPlots[n].MaxRenderIndex = maxPlotIndex;
                            plot.Plot.AxisAuto();
                            plot.Render();
                        }
                    }
                    n++;
                }
            }
        }
        // End phidget event handelers
        #endregion

        // User interface event handelers
        #region UI Events
        //
        // This event is fired when the user closes the program
        //
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                man.Close();
            }
            catch (PhidgetException ex)
            {
                Console.WriteLine("Failure: " + ex.Message);
            }
            // Save calibration data
            setSavedCalibrations();
            // Save the selected tab
            Properties.Settings.Default.tab = metroSetTabControl2.SelectedIndex;
            // Save changes that have been made to settings
            Properties.Settings.Default.Save();
        }


        //
        // This event is used to open and close chanels
        //
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            Phidget p = (Phidget)e.Node.Tag;
            // Determain if event is checking or unchecking
            if (e.Node.Checked) // Checking event
            {
                // If it is not a canel, uncheck it
                if (!p.IsChannel)
                {
                    e.Node.Checked = false;
                }
                else
                {
                    // Find the phidget that has been selected
                    foreach (Phidget phg in avalible_phidgets)
                    {
                        // Find the matching phidget
                        if (phg.IsChannel && isSame(phg, p)) // if not a cannel or diffrent channel
                        {
                            if (!(phg.IsOpen))
                            {
                                try
                                {
                                    // TODO MVP: Add onchange event listener
                                    phg.Open(5000); // Try to open chanel with 5 sec timeout                                                                      
                                }catch (PhidgetException ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                    Console.WriteLine("PhidgetException " + ex.ErrorCode + " (" + ex.Description + "): " + ex.Detail);
                                    Console.WriteLine("----------------------------------------------------------------------------------");
                                }
                                
                            }
                        }
                    }
                }
            }
            else // Unchecking event
            {
                // Close Channel
                // Find the phidget that has been selected
                foreach (Phidget phg in avalible_phidgets)
                {
                    // Find the matching phidget
                    if (phg.IsChannel && isSame(phg, p)) // if not a cannel or diffrent channel
                    {
                        if ((phg.IsOpen))
                        {

                            phg.Close();

                        }
                    }
                }
            }

            
        }
 
       
        //
        // This event is fired when the button for ECB testing is toggled
        //
        private void btnIsECB_Click(object sender, EventArgs e)
        {
            bool currentState = Properties.Settings.Default.IsECB;
            RadioButton me = (RadioButton)sender;
            Console.WriteLine(me.Checked);
            if (currentState) // Switch form ECB to not ECB
            {
                Properties.Settings.Default.IsECB = false; // Set setting.IsECB to false
                me.Checked = false; // Set combo.Checked to false
                // Hide the ECB UI panels
                panECBsetUp.Visible = false;
                panECBcontrol.Visible = false;
                // splitContainer1.Locked = false;
            }
            else // Switch form not ECB to ECB
            {
                Properties.Settings.Default.IsECB = true; // Set setting.IsECB to true
                me.Checked = true; // Set combo.Checked to true
                // Show the ECB UI panels
                panECBsetUp.Visible = true;
                panECBcontrol.Visible = true;
            }
        }
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            //updateStreamsWithCalibration();
            Button myButton = (Button)sender;
            // Check to see it there are any points to be saved
            bool hasData = false;
            foreach (ScottPlot.Plottable.SignalPlotXY signal in signalPlots)
            {
                if (!(signal.Xs[3] == 0))
                {
                    hasData = true;
                }
            }
            // Figure out if it is the start or stop button that has been pressed
            if ((string)myButton.Tag == "start")
            {
                // Check to see if ther is data on the plot
                if (hasData)
                {
                    DialogResult result = MetroSetMessageBox.Show(this, "Starting a new test will erase previous test data. Do you want to save data before proceeding?", "Caution!", MessageBoxButtons.YesNoCancel);
                    switch (result)
                    {
                        case DialogResult.Yes:
                            saveData();
                            break;
                        case DialogResult.No:
                            break;
                        case DialogResult.Cancel:
                            return;
                    }
                    clearTestData();
                }
                
                myButton.Tag = "stop";
                myButton.BackgroundImage = Properties.Resources.stop;

                // Iterate through the input channels
                foreach (MetroSetComboBox cbx in inChannels)
                {
                    // Check to see if nontreivial index has been selected
                    if (!(cbx.SelectedIndex == -1))
                    {

                        List<Phidget> phidgets = (List<Phidget>)cbx.Tag; // Get all of the avalible phidgets
                        Phidget p = phidgets[cbx.SelectedIndex]; // Get the phidget that is selected
                        if (p.ChannelClass == ChannelClass.VoltageInput) // Ignore phidgets that are not Voltage inputs
                        {
                            /*
                            if (!p.IsOpen) // Check if the chanel is already open
                            {
                                
                                 * This portion of code origionaly served to create streams and open channels when the start button
                                 * was selected. This activity has been moved to run when the coombo box selection is first made
                                 * because we first check if the phidget channel is open, this code will likly never run. Only in
                                 * in the event that the chanel faild to oppen the first time will it try again here
                                 
                                VoltageInput VI = (VoltageInput)p;  // Create Voltage input channel form selected phidget

                                // Create a phidget stream
                                PhidgetStream phgStream = new PhidgetStream(VI, getStreamName(cbx));
                                phgStream.setUnits("Volts");  // TODO MVP: and meaningful units
                                all_streams.Add(phgStream);  // Add to list of streams
                                updateStreamsWithCalibration();

                                values.Add(new double[maxDataCount]);
                                times.Add(new double[maxDataCount]);
                                

                                // Add stream to plots
                                ScottPlot.Plottable.SignalPlotXY sp = new ScottPlot.Plottable.SignalPlotXY();
                                sp = plot.Plot.AddSignalXY(times.Last(), values.Last());
                                sp.Label = phgStream.getName();
                                signalPlots.Add(sp);

                                // Add event handelers
                                VI.VoltageChange += voltageChange;

                                VI.Open(1000);
                                VI.DataInterval = dataInterval;
                                
                            }
                            */
                            // Find if this is the first one
                            if (startTime == DateTime.MinValue)
                            {
                                startTime = DateTime.Now;
                                ignoreVoltageChange = false;
                            }
                        }

                    }
                }
            }
            else
            {
                if(hasData)
                {
                    btnSave.Visible = true;
                }
                myButton.Tag = "start";
                myButton.BackgroundImage = Properties.Resources.start;
                ignoreVoltageChange = true;
            }
        }
        #endregion


        // Functions that do stuff for other functions 
        #region Helper functions
        //
        // This fonction coparise two phidges to see if they are the same address
        //
        private bool isSame(Phidget p1, Phidget p2)
        {
            // TODO MVP: Make sure you cannever try to open two channels on one port
            // Check if ether of the phidgest are null
            if (p1 == null || p2 == null)
            {
                return false;
            }
            // Check to see if the phidgest to be tested are that channels 
            if (p1.IsChannel && p2.IsChannel)  // Both are channels
            {
                return (p1.DeviceSerialNumber == p2.DeviceSerialNumber && p1.HubPort == p2.HubPort && p1.ChannelClassName == p2.ChannelClassName && p1.IsHubPortDevice == p2.IsHubPortDevice &&  p1.Channel == p2.Channel);
            } else if (!p1.IsChannel && !p2.IsChannel)  // Both are not channels
            {
                return (p1.DeviceSerialNumber == p2.DeviceSerialNumber && p1.HubPort == p2.HubPort && p1.ChannelClassName == p2.ChannelClassName && p1.IsHubPortDevice == p2.IsHubPortDevice);
            }
            else
            {
                return false;
            }
            
        }
        private bool isSame(Phidget p, string s)
        {
            string[] strings = s.Split('_');
            // Check if ether of the phidgest are null
            if (p == null || s == "")
            {
                return false ; 
            }

            // Check to see if the phidgest to be tested are that channels 
            if (p.IsChannel && strings.Count() == 5)
            {
                return (p.DeviceSerialNumber.ToString() == strings[0] && p.HubPort.ToString() == strings[1] && ((int)p.ChannelClass).ToString() == strings[2] && p.IsHubPortDevice.ToString() == strings[3] && p.Channel.ToString() == strings[4]);
            }
            else if (!p.IsChannel && strings.Count() == 5)  // Both are not channels
            {
                return (p.DeviceSerialNumber.ToString() == strings[0] && p.HubPort.ToString() == strings[1] && ((int)p.ChannelClass).ToString() == strings[2] && p.IsHubPortDevice.ToString() == strings[3]);
            }
            else
            {
                return false;
            }
        }

        private void addECBChanel (Phidget phidget)
        {
            // Ignore if it is not a channel or null
            if (!(phidget == null) && phidget.IsChannel)
            {
                string disp = phidget.DeviceSerialNumber + " - Port: " + phidget.HubPort;
                if (phidget.ChannelClass == ChannelClass.VoltageInput)
                {
                    List<Phidget> list = (List<Phidget>) cbxSupplyTank.Tag;
                    list.Add(phidget);
                    // cbxSupplyTank.Items.Add(disp);
                    AddWithNull(cbxSupplyTank, disp);

                    list = (List<Phidget>)cbxDeliveryTank.Tag;
                    list.Add(phidget);
                    // cbxDeliveryTank.Items.Add(disp);
                    AddWithNull (cbxDeliveryTank, disp);

                    list = (List<Phidget>)cbxECBpressDown.Tag;
                    list.Add(phidget);
                    // cbxECBpressDown.Items.Add(disp);
                    AddWithNull(cbxECBpressDown, disp);

                    list = (List<Phidget>)cbxECBpressUp.Tag;
                    list.Add(phidget);
                    // cbxECBpressUp.Items.Add(disp);
                    AddWithNull(cbxECBpressUp, disp);

                    list = (List<Phidget>)cbxFlow.Tag;
                    list.Add(phidget);
                    // cbxFlow.Items.Add(disp);
                    AddWithNull(cbxFlow, disp);
                } else if(phidget.ChannelClass == ChannelClass.DigitalOutput)
                {
                    if (!phidget.IsHubPortDevice)
                    {
                        disp = disp + " - Ch: " + phidget.Channel;
                    }
                    // cbxSolenoidUp.Items.Add(disp);
                    AddWithNull(cbxSolenoidUp, disp);
                    List<Phidget> list = (List<Phidget>)cbxSolenoidUp.Tag;
                    list.Add(phidget);

                    // cbxECBsolenoid.Items.Add(disp);
                    AddWithNull(cbxECBsolenoid, disp);
                    list = (List<Phidget>)cbxECBsolenoid.Tag;
                    list.Add(phidget);

                    // cbxECBvent.Items.Add(disp);
                    AddWithNull(cbxECBvent, disp);
                    list = (List<Phidget>)cbxECBvent.Tag;
                    list.Add(phidget);

                    // cbxTankVent.Items.Add(disp);
                    AddWithNull(cbxTankVent, disp);
                    list = (List<Phidget>)cbxTankVent.Tag;
                    list.Add(phidget);
                }
                
                if(isSame(phidget, Properties.Settings.Default.supplyTankPressure))
                {
                    cbxSupplyTank.SelectedIndex = (cbxSupplyTank.Tag as List<Phidget>).Count - 1;
                } 
                else if (isSame(phidget, Properties.Settings.Default.downstreamPressure))
                {
                    cbxECBpressDown.SelectedIndex = (cbxECBpressDown.Tag as List<Phidget>).Count - 1;
                } 
                else if (isSame(phidget, Properties.Settings.Default.upstreamPressure))
                {
                    cbxECBpressUp.SelectedIndex = (cbxECBpressUp.Tag as List<Phidget>).Count - 1;
                }
                else if (isSame(phidget, Properties.Settings.Default.flow))
                {
                    cbxFlow.SelectedIndex = (cbxFlow.Tag as List<Phidget>).Count - 1;
                }
                else if (isSame(phidget, Properties.Settings.Default.deliveryTankPressure))
                {
                    cbxDeliveryTank.SelectedIndex = (cbxDeliveryTank.Tag as List<Phidget>).Count - 1;
                }
                else if (isSame(phidget, Properties.Settings.Default.updtreamSolinoid))
                {
                    cbxSolenoidUp.SelectedIndex = (cbxSolenoidUp.Tag as List<Phidget>).Count - 1;
                }
                else if (isSame(phidget, Properties.Settings.Default.downstreamSolinoid))
                {
                    cbxECBsolenoid.SelectedIndex = (cbxECBsolenoid.Tag as List<Phidget>).Count - 1;
                }
                else if (isSame(phidget, Properties.Settings.Default.ecbVentSolinoid))
                {
                    cbxECBvent.SelectedIndex = (cbxECBvent.Tag as List<Phidget>).Count - 1;
                }
                else if (isSame(phidget, Properties.Settings.Default.ecbVentSolinoid))
                {
                    cbxTankVent.SelectedIndex = (cbxTankVent.Tag as List<Phidget>).Count - 1;
                }
            }
        }
        private void removeECBChanel(Phidget phidget)
        {

        }

        private string getStreamName(MetroSetComboBox cb)
        {
            switch (cb.Name)
            {
                case "cbxSupplyTank":
                    return "Supply Tank Presure";
                case "cbxDeliveryTank":
                    return "Delivery Tank Pressure";
                case "cbxECBpressDown":
                    return "Downstream ECB Presure";
                case "cbxECBpressUp":
                    return "Upstream ECB Pressure";
                case "cbxSolenoidUp":
                    return "Upstream Solenoid";
                case "cbxECBsolenoid":
                    return "ECB Solenoid";
                case "cbxECBvent":
                    return "ECB Vent Solenoid";
                case "cbxTankVent":
                    return "Delivery Tank Vent Solenoid";
                case "cbxFlow":
                    return "Flow Sensor";
                default:
                    return "";
            }
        }
        #endregion



        private void CheckedChanged(object sender, EventArgs e)
        {
            // Ignore all events that are unchecked
            RadioButton radio = (RadioButton)sender;
            if (radio.Checked)
            {
                // Select time vs. pressure
                switch (radio.Text)
                {
                    case "Targeting pressure":
                        lblTarget.Text = "Target Pressure:";
                        lblUnit.Text = "[PSI]";
                        break;
                    case "Targeting time":
                        lblTarget.Text = "Target Time:";
                        lblUnit.Text = "[sec]";
                        break;
                }
            }
            
        }



        private void cbx_SelectedIndexChanged(object sender, EventArgs e)
        {

            MetroSetComboBox cbx = (MetroSetComboBox)sender;
            List<Phidget> phidgets = cbx.Tag as List<Phidget>;
            Phidget p;
            if (!(phidgets.Count <= cbx.SelectedIndex))
            {
                p = phidgets[cbx.SelectedIndex];
            }
            else
            {
                p = null;
            }

            switch (cbx.Name)
            {
                case "cbxSupplyTank":
                    Properties.Settings.Default.supplyTankPressure = phidgetToString(p);
                    ckb_supply.Enabled = (p!=null);
                    ckb_supply.Tag = p;
                    break;
                case "cbxECBpressDown":
                    Properties.Settings.Default.downstreamPressure = phidgetToString(p);
                    ckb_down.Enabled = (p!=null);
                    ckb_down.Tag = p;
                    break;
                case "cbxECBpressUp":
                    Properties.Settings.Default.upstreamPressure = phidgetToString(p);
                    ckb_up.Enabled = (p!=null);
                    ckb_up.Tag = p;
                    break;
                case "cbxFlow":
                    Properties.Settings.Default.flow = phidgetToString(p);
                    ckb_flow.Enabled = (p!=null);
                    ckb_flow.Tag = p;
                    break;
                case "cbxDeliveryTank":
                    Properties.Settings.Default.deliveryTankPressure = phidgetToString(p);
                    ckb_delivery.Enabled = (p!=null);
                    ckb_delivery.Tag = p;
                    break;
                default:
                    break;
            }

            if (p != null && p.ChannelClass == ChannelClass.VoltageInput)
            {
                VoltageInput VI = (VoltageInput)p;  // Create Voltage input channel form selected phidget

                // Create a phidget stream
                PhidgetStream phgStream = new PhidgetStream(VI, getStreamName(cbx));
                phgStream.setUnits("Volts");  // TODO MVP: and meaningful units
                all_streams.Add(phgStream);  // Add to list of streams

                updateStreamsWithCalibration();

                values.Add(new double[maxDataCount]);
                times.Add(new double[maxDataCount]);


                // Add stream to plots
                ScottPlot.Plottable.SignalPlotXY sp = new ScottPlot.Plottable.SignalPlotXY();
                sp.MarkerShape = ScottPlot.MarkerShape.none;
                sp = plot.Plot.AddSignalXY(times.Last(), values.Last());
                sp.Label = phgStream.getName();
                signalPlots.Add(sp);

                // Add event handelers for the first one that is attached
                if (!hasVoltageListener)
                {
                    VI.VoltageChange += voltageChange;
                    hasVoltageListener = true;
                }
                
                VI.Attach += VI_Attach;

                VI.Open();

                // If there is a stream called upstream and downstream, then add a caculated stream
                bool isUp = false;
                bool isDown = false;
                bool isCalculated = false;
                PhidgetStream calcStream = null;
                foreach (PhidgetStream stream in all_streams)
                {
                    if (stream.getName() == "Upstream ECB Pressure")
                    {
                        calcStream = new PhidgetStream(new Phidget(), "Calculated Tank Pressure");
                        isUp = true;
                    }
                    else if (stream.getName() == "Downstream ECB Presure")
                    {
                        isDown = true;
                    }
                    else if (stream.getName() == "Calculated Tank Pressure")
                    {
                        isCalculated = true;
                    }
                }
                if (!isCalculated && isUp && isDown)
                {
                    all_streams.Add(calcStream);

                    values.Add(new double[maxDataCount]);
                    times.Add(new double[maxDataCount]);

                    // Add stream to plots
                    ScottPlot.Plottable.SignalPlotXY csp = new ScottPlot.Plottable.SignalPlotXY();
                    csp.LineStyle = ScottPlot.LineStyle.Dash;
                    csp.MarkerShape = ScottPlot.MarkerShape.none;
                    csp = plot.Plot.AddSignalXY(times.Last(), values.Last());
                    csp.Label = calcStream.getName();
                    signalPlots.Add(csp);
                }                

            } else if (p == null)
            {
                foreach (Phidget phidget in phidgets)
                {
                    if (phidget.IsOpen)
                    {
                        phidget.Close();
                    }
                }
            }
        }

        private void bgw_phidget_open_DoWork(object sender, DoWorkEventArgs e)
        {
            List<PhidgetStream> streams = e.Argument as List<PhidgetStream>;
            foreach (PhidgetStream phidgetStream in all_streams)
            {
                if (!phidgetStream.phidget.IsOpen)
                {
                    phidgetStream.phidget.Open(1000);
                    phidgetStream.phidget.DataInterval = dataInterval;
                    // Console.WriteLine(phidgetStream.getName());
                    // phidgetStream.phidget.Open();
                }
            }
        }

        private void bgw_phidget_open_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
        private void VI_Attach(object sender, AttachEventArgs e)
        {
            // TODO: Add to 
            Phidget P = sender as Phidget;
            P.DataInterval = dataInterval;
            // Console.WriteLine(P.ChannelName);
        }
        private void DI_Attach(object sender, AttachEventArgs e)
        {
            Phidget P = sender as Phidget;
            // Console.WriteLine(P.ChannelName);
        }

        private void solinoid_SelectedIndexChanged(object sender, EventArgs e)
        {
            MetroSetComboBox cbx = (MetroSetComboBox)sender;
            List<Phidget> phidgets = cbx.Tag as List<Phidget>;
            Phidget p;
            if (!(phidgets.Count <= cbx.SelectedIndex))
            {
                p = phidgets[cbx.SelectedIndex];
            }
            else
            {
                p = null;
            }

            MetroSetSwitch msSwitch;
            switch (cbx.Name)
            {
                case "cbxSolenoidUp":
                    Properties.Settings.Default.updtreamSolinoid = phidgetToString(p);
                    msSwitch = swcUpSolenoid;
                    break;
                case "cbxECBsolenoid":
                    Properties.Settings.Default.downstreamSolinoid = phidgetToString(p);
                    msSwitch = swcECBsolenoid;
                    break;
                case "cbxECBvent":
                    Properties.Settings.Default.ecbVentSolinoid = phidgetToString(p);
                    msSwitch = swcVentSolenoid;
                    break;
                case "cbxTankVent":
                    Properties.Settings.Default.tankVentSolinoid = phidgetToString(p);
                    msSwitch = swcTankVentSolenoid;
                    break;
                default:
                    msSwitch = new MetroSetSwitch();
                    break;
            }

            // Close the chanel for this combo box

            if (cbx.SelectedItem.ToString() == "")
            {
                foreach(Phidget ph in phidgets)
                {
                    msSwitch.Enabled = false;
                    msSwitch.Switched = false;
                    msSwitch.Tag = null;
                    if (ph.IsOpen)
                    {
                        ph.Close();
                    }
                }
            }
            else
            {
                // Create a phidget stream
                PhidgetStream phgStream = new PhidgetStream(p, getStreamName(cbx));
                phgStream.setUnits("ON/OFF");
                all_streams.Add(phgStream);  // Add to list of streams

                // Add digital output to the be tracked
                values.Add(new double[maxDataCount]);
                times.Add(new double[maxDataCount]);

                // Add stream to plots
                ScottPlot.Plottable.SignalPlotXY sp = new ScottPlot.Plottable.SignalPlotXY();
                sp = plot.Plot.AddSignalXY(times.Last(), values.Last());
                sp.Label = phgStream.getName();
                signalPlots.Add(sp);

                msSwitch.Enabled = true;
                
                msSwitch.Tag = p;
                p.Attach += DI_Attach;
                // TODO: Move this to background worker
                p.Open();
                digitalOutputs.Add((DigitalOutput)p);
            } 
        }

        private void swc_SwitchedChanged(object sender)
        {
            MetroSetSwitch msSwitch = (MetroSetSwitch)sender;
            DigitalOutput p = (DigitalOutput)msSwitch.Tag;
            if (!(p == null) && p.IsOpen)
            {
                p.State = msSwitch.Switched;

                if((string)btnStartStop.Tag == "stop")
                {
                    int n = 0;
                    foreach (PhidgetStream pStream in all_streams)
                    {
                        if (p.ChannelClass == ChannelClass.DigitalOutput && pStream.getName() != "Calculated Tank Pressure" && isSame(p, pStream.phidget)) // Identify what stream has changed voltage
                        {
                            TimeSpan duration = DateTime.Now - startTime;
                            if (p.State)
                            {
                                pStream.addPoint(0, duration.TotalSeconds);
                                values[n][pStream.val.Length - 1] = pStream.val.Last();
                                times[n][pStream.t.Length - 1] = pStream.t.Last();
                                pStream.addPoint(1, duration.TotalSeconds+1);
                                values[n][pStream.val.Length - 1] = pStream.val.Last();
                                times[n][pStream.t.Length - 1] = pStream.t.Last();
                            }
                            else
                            {
                                pStream.addPoint(1, duration.TotalSeconds);
                                values[n][pStream.val.Length - 1] = pStream.val.Last();
                                times[n][pStream.t.Length - 1] = pStream.t.Last();
                                pStream.addPoint(0, duration.TotalSeconds+1);
                                values[n][pStream.val.Length - 1] = pStream.val.Last();
                                times[n][pStream.t.Length - 1] = pStream.t.Last();
                            }

                            // Refresh the plot
                            int maxPlotIndex = pStream.val.Length - 1;
                            signalPlots[n].MaxRenderIndex = maxPlotIndex;
                            plot.Plot.AxisAuto();
                            plot.Render();
                        }
                        n++;
                    }
                }
            }
        }
        private void saveData()
        {
            string saveName = saveNameGenerator(); // Format the file save name

            // Format the data on the chart into savable form
            string csvString = buildCSV(signalPlots);
            
            using(SaveFileDialog fd = new SaveFileDialog())
            {
                fd.FileName = saveName;
                fd.DefaultExt = "*.csv";
                fd.InitialDirectory = Properties.Settings.Default.defaultSaveDir;  // Get the default save location from settings
                fd.Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";  // Set file type filter

                if(fd.ShowDialog() == DialogResult.OK)  // Show save dialog
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(fd.FileName);  // Get info from the save location the user selected
                    Properties.Settings.Default.defaultSaveDir = fileInfo.DirectoryName;  // Set new location for next defalt
                    System.IO.File.WriteAllText(fd.FileName, csvString);  // Write data to CSV file
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveData();
        }

        private string saveNameGenerator()
        {
            string str_return = string.Empty;
            bool isTime = false;
            foreach(Control c in panel1.Controls)
            {
                if (c.AccessibilityObject.Role == AccessibleRole.RadioButton)
                {
                    RadioButton radio = (RadioButton)c;
                    if (radio.Checked)
                    {
                        // Determain if the test is inflation or deflation
                        if (radio.Location.Y < 85)  // Inflation
                        {
                            str_return = "I_";
                        }
                        else  // Deflation
                        {
                            str_return = "D_";
                        }

                        // Select time vs. pressure
                        switch (radio.Text)
                        {
                            case "Targeting pressure":
                                str_return += "P_";
                                break;
                            case "Targeting time":
                                isTime = true;
                                str_return += "T_";
                                break;
                        }
                    }
                }
            }

            string supplyPressure="XXX";
            string axelPressure = "XXX";
            foreach(PhidgetStream phidgetStream in all_streams)
            {
                if (phidgetStream.getName() == "Supply Tank Presure")
                {
                    int startPressure = (int)phidgetStream.getValues()[0];
                    supplyPressure = startPressure.ToString();
                    supplyPressure = paddThreeDig(supplyPressure);
                } else if(phidgetStream.getName() == "Delivery Tank Pressure")
                {
                    int sdp = (int)phidgetStream.getValues()[0];
                    axelPressure = sdp.ToString();
                    axelPressure = paddThreeDig(axelPressure);
                }
            }

            // Add starting supply presure
            str_return += supplyPressure + "_";
            // Add starting dilivery tank pressure
            str_return += axelPressure + "_";
            // Add target time or pressure
            str_return += paddThreeDig(metroSetNumeric1.Value.ToString()) + "_";
            // Add axle volume
            str_return += paddTwoDig(metroSetNumeric3.Value.ToString()) + "_";
            // Add Month
            str_return += paddTwoDig(startTime.Date.Month.ToString()) + "-";
            str_return += paddTwoDig(startTime.Date.Day.ToString()) + "-";
            str_return += startTime.Date.Year.ToString() + "_";
            int h = startTime.TimeOfDay.Hours;
            int m = startTime.TimeOfDay.Minutes;
            str_return += paddTwoDig(h.ToString()) + "-";
            str_return += paddTwoDig(m.ToString());


            return str_return;
        }

        private string paddThreeDig(string str)
        {
            switch (str.Length)
            {
                case 0:
                    return "000";
                case 1:
                    return "00" + str;
                case 2:
                    return "0" + str;
                default:
                    return str;
            }
        }

        private string paddTwoDig(string str)
        {
            switch (str.Length)
            {
                case 0:
                    return "00";
                case 1:
                    return "0" + str;
                default:
                    return str;
            }
        }
        

        private string buildCSV(List<ScottPlot.Plottable.SignalPlotXY> signalPlots)
        {
            string toReturn= string.Empty;
            int numCols = signalPlots.Count + 2;
            // Create the collumn hedders
            for (int n = 0; n <= signalPlots.Count; n++)
            {
                if (n == signalPlots.Count) //- 1
                {
                    toReturn += "Equation: " + txbEquation.Text.Replace("= ", "") + "\n"; //signalPlots[n].Label + " (Times)," + signalPlots[n].Label + " (Values)
                }
                else
                {
                    toReturn += signalPlots[n].Label + " (Times)," + signalPlots[n].Label + " (Values),";
                }  
            }

            // Get the data
            for (int n = 0; n< signalPlots[0].PointCount; n++)
            {
                string ln = "";
                for (int i = 0; i < signalPlots.Count; i++)
                {
                    if (i == signalPlots.Count - 1)
                    {
                        if(signalPlots[i].Xs[n] == 0)
                        {
                            ln += ",";
                        }
                        else
                        {
                            ln += signalPlots[i].Xs[n].ToString() + "," + signalPlots[i].Ys[n].ToString();
                        }                                               
                    }
                    else
                    {
                        if (signalPlots[i].Xs[n] == 0)
                        {
                            ln += ",,";
                        }
                        else
                        {

                            ln += signalPlots[i].Xs[n].ToString() + "," + signalPlots[i].Ys[n].ToString() + ",";
                        }
                    }

                }
                bool isEmpty = true;
                foreach(char c in ln.Replace(",", ""))
                {
                    if(!(c == '0'))
                    {
                        isEmpty = false;
                    }
                }
                if (isEmpty)
                {
                    ln = "";
                }
                else
                {
                    ln += "\n";
                }
                toReturn += ln;
            }
            return toReturn;
        }
        private void clearTestData()
        {
            int n = 0;
            foreach (PhidgetStream stream in all_streams)
            {
                stream.Clear();
                values[n] = new double[maxDataCount];
                times[n] = new double[maxDataCount];

                signalPlots[n].Xs = times[n];
                signalPlots[n].Ys = values[n];   


                // Refresh the plot
                int maxPlotIndex = stream.val.Length - 1;

                signalPlots[n].MaxRenderIndex = maxPlotIndex;

                n++;
            }
            startTime = new DateTime();
            btnSave.Visible = false;
            plot.Plot.AxisAuto();
            plot.Render();
        }
        

        private string phidgetToString(Phidget phidget)
        {
            if (!(phidget == null))
            {
                if (!(phidget.IsChannel))
                {
                    return phidget.DeviceSerialNumber.ToString() + "_" + phidget.HubPort.ToString() + "_"
                            + ((int)phidget.ChannelClass).ToString() + "_" + (phidget.IsHubPortDevice).ToString();
                }
                else
                {
                    return phidget.DeviceSerialNumber.ToString() + "_" + phidget.HubPort.ToString() + "_"
                            + ((int)phidget.ChannelClass).ToString() + "_" + (phidget.IsHubPortDevice).ToString() + "_" + phidget.Channel.ToString();
                }
            } else { return ""; }
        }

        private void AddWithNull(MetroSetComboBox cbx, string discription)
        {
            // Remove the last item if it is null
            if (!(cbx.Items.Count == 0) && (cbx.Items[cbx.Items.Count - 1].ToString() == ""))
            {
                cbx.Items.RemoveAt(cbx.Items.Count - 1);
            }

            // Add new item
            cbx.Items.Add(discription);

            // Add the null item back to the end
            cbx.Items.Add("");
        }

        private void calibration_Click(object sender, EventArgs e)
        {
            System.Drawing.Color froeColor = System.Drawing.Color.FromArgb(170, 170, 170);
            ckb_supply.ForeColor = froeColor;
            ckb_delivery.ForeColor = froeColor;
            pnl_calibration.Visible = true;
        }
        private void colorMe()
        {
            System.Drawing.Color acent = System.Drawing.Color.FromArgb(220, 37, 51);
            System.Drawing.Color froeColor = System.Drawing.Color.FromArgb(170, 170, 170);
            ckb_supply.ForeColor = froeColor;
            ckb_supply.CheckSignColor = acent;
            ckb_delivery.ForeColor = froeColor;
            ckb_delivery.CheckSignColor = acent;
            ckb_up.ForeColor = froeColor;
            ckb_up.CheckSignColor = acent;
            ckb_down.ForeColor = froeColor;
            ckb_down.CheckSignColor = acent;
            ckb_flow.ForeColor = froeColor;
            ckb_flow.CheckSignColor = acent;
        }

        private void bgwColorMe_DoWork(object sender, DoWorkEventArgs e)
        {
            colorMe();
        }

        private void ckb_CheckedChanged(object sender)
        {
            MetroSetCheckBox ckb = sender as MetroSetCheckBox;
            // if checked, add collumn, else remove column
            if (ckb.Checked)
            {
                DataGridViewColumn newColumn = new DataGridViewColumn(dgv_calibration.Columns[0].CellTemplate);
                newColumn.Name = ckb.Text;
                newColumn.ReadOnly = true;
                
                dgv_calibration.Columns.Add(newColumn);
            }
            else
            {
                DataGridViewColumn newColumn = new DataGridViewColumn(dgv_calibration.Columns[0].CellTemplate);
                newColumn.Name = ckb.Text;
                // Find the colum index with name that matched sender.text
                int n = 0;
                foreach(DataGridViewColumn column in dgv_calibration.Columns)
                {
                    if (column.Name == ckb.Text)
                    {
                        break;
                    }
                    n++;
                }
                dgv_calibration.Columns.RemoveAt(n);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            pnl_calibration.Visible = false;
            // Remove any perviously added lines
            plot_calibration.Plot.Clear();
            // TODO MVP: Add closing of channel
            // TODO: add warning for losing calibration data without setting
        }

        // This is run when the start/Capture button is clicked
        private void btn_calibration_Click(object sender, EventArgs e)
        {
            // Check to see if there are at least two data points in the expected value table
            if (dgv_calibration.RowCount >= 3)
            {
                Button myButton = (Button)sender;
                if ((string)myButton.Tag == "start")
                {
                    btn_save_calibration.Visible = false;
                    // Eptye dataGridView Taple value
                    foreach (DataGridViewRow r in dgv_calibration.Rows)
                    {
                        foreach(DataGridViewCell c in r.Cells)
                        {
                            if (c.ColumnIndex != 0)
                            {
                                if (c.Value != null)
                                {
                                    // TODO: Give warning message to user that data will be lost
                                    c.Value = null;
                                }
                                
                            }     
                        }
                    }
                    myButton.Tag = "capture";
                    myButton.BackgroundImage = Properties.Resources.Capture;
                    currentCalibrationRow = 0;

                    // Iterate through each ckb and open the phidgets that are selected and open their phidget chanels
                    // Supply
                    if (ckb_supply.Checked)
                    {
                        VoltageInput supply = ckb_supply.Tag as VoltageInput;
                        if (supply != null && !supply.IsOpen)
                        {
                            // Add event handelers
                            supply.VoltageChange += calibrationVoltageChange;

                            // Open Channel
                            supply.Open(1000);
                        }
                    }
                    // Upstream
                    if (ckb_up.Checked)
                    {
                        VoltageInput up = ckb_up.Tag as VoltageInput;
                        // add even handlers
                        up.VoltageChange += calibrationVoltageChange;

                        // Open Channel
                        up.Open(1000);
                    }
                    // Downstream
                    if (ckb_down.Checked)
                    {
                        VoltageInput down = ckb_down.Tag as VoltageInput;
                        // add even handlers
                        down.VoltageChange += calibrationVoltageChange;

                        // Open Channel
                        down.Open(1000);
                    }
                    // Delivery
                    if (ckb_delivery.Checked)
                    {
                        VoltageInput delivery = ckb_delivery.Tag as VoltageInput;
                        // add even handlers
                        delivery.VoltageChange += calibrationVoltageChange;

                        // Open Channel
                        delivery.Open(1000);
                    }
                    // Flow
                    if (ckb_flow.Checked)
                    {
                        VoltageInput flow = ckb_flow.Tag as VoltageInput;
                        // add even handlers
                        flow.VoltageChange += calibrationVoltageChange;

                        // Open Channel
                        flow.Open(1000);
                    }

                } else if ((string)myButton.Tag == "capture")
                {
                    currentCalibrationRow++;
                    //TODO Add caputred values to the plot
                    double[] ys;
                    double[] xs;
                    List<double> all_y = new List<double>();
                    List<double> all_x = new List<double>();
                    foreach(DataGridViewRow row in dgv_calibration.Rows)
                    {
                        foreach(DataGridViewCell cell in row.Cells)
                        {
                            if (cell.ColumnIndex != 0)
                            {
                                if (cell.Value == null)
                                {
                                    continue;
                                }
                                else
                                {
                                    all_x.Add((double)cell.Value);
                                    try
                                    {
                                        all_y.Add(Convert.ToDouble(row.Cells[0].Value));
                                    }
                                    catch (FormatException ex)
                                    {
                                        Console.WriteLine(ex.Message.ToString());
                                       
                                    }
                            }
                            }
                        }
                    }
                    if (currentCalibrationRow > 1)
                    {
                        // Remove any perviously added lines
                        plot_calibration.Plot.Clear();
                        // Create list
                        ys = all_y.ToArray();
                        xs = all_x.ToArray();
                        double first = xs[0];
                        double last = xs[xs.Length - 1];
                        var model = new ScottPlot.Statistics.LinearRegressionLine(xs, ys);
                        plot_calibration.Plot.Title($"Y = {model.slope:0.0000}x + {model.offset:0.0} " + $"(R² = {model.rSquared:0.0000})");
                        plot_calibration.Plot.AddScatter(xs, ys, lineWidth: 0);
                        // Add the new trend line
                        plot_calibration.Plot.AddLine(model.slope, model.offset, (first, last), lineWidth: 2);

                        plot_calibration.Plot.AxisAuto();
                        plot_calibration.Render();
                       if(!(currentCalibrationRow < dgv_calibration.Rows.Count - 1))  // This means the end of calibration has been reached
                       {
                            gain = model.slope;
                            offset = model.offset;
                       }
                    }
 
                }
            }
            else
            {
                // TODO: add message saying that you must have at least two point to start a calibration
            }

        }

        private void calibrationVoltageChange(object sender, VoltageInputVoltageChangeEventArgs e)
        {
            VoltageInput voltage = sender as VoltageInput;
            if (currentCalibrationRow < dgv_calibration.Rows.Count - 1) // Make sure testing should not be ended
            { 
                // Write the value ov foltage to the girdview
                DataGridViewRow row = dgv_calibration.Rows[currentCalibrationRow];
                foreach (DataGridViewCell c in row.Cells)
                {
                    if (c.ColumnIndex != 0) // Skip the first column
                    {
                        switch (dgv_calibration.Columns[c.ColumnIndex].HeaderText)
                        {
                            case "Supply Tank Sensor":
                                if(isSame((Phidget)ckb_supply.Tag, (Phidget)sender))
                                {
                                    c.Value = voltage.Voltage;
                                }
                                break;
                            case "Upstream Sensor":
                                if (isSame((Phidget)ckb_up.Tag, (Phidget)sender))
                                {
                                    c.Value = voltage.Voltage;
                                }
                                break;
                            case "Downstream Sensor":
                                if (isSame((Phidget)ckb_down.Tag, (Phidget)sender))
                                {
                                    c.Value = voltage.Voltage;
                                }
                                break;
                            case "Delivery Tank Sensor":
                                if (isSame((Phidget)ckb_delivery.Tag, (Phidget)sender))
                                {
                                    c.Value = voltage.Voltage;
                                }
                                break;
                            case "Flow Sensor":
                                if (isSame((Phidget)ckb_flow.Tag, (Phidget)sender))
                                {
                                    c.Value = voltage.Voltage;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else
            {
                // TODO MVP: add end of calipration ruteane
                // Close sender
                Phidget p = sender as Phidget;
                if (p.IsOpen)
                {
                    p.Close();
                }
                btn_save_calibration.Visible = true;
                btn_calibration.Tag = "start";
                btn_calibration.BackgroundImage = Properties.Resources.start;
            }
        }

        private double calulateGain(double[] Xs, double[] Ys)
        {
            if (Xs.Length == Ys.Length)
            {
                int n = Xs.Length;
                double sumXY=0;
                double sumX=0;
                double sumY=0;
                double sumX2=0;
                for(int i = 0; i < n; i++)
                {
                    sumXY += Xs[i] * Ys[i];
                    sumX += Xs[i];
                    sumY += Ys[i];
                    sumX2 += Xs[i] * Xs[i];
                }
                return (n*sumXY - sumX*sumY) / (n*sumX2 - (sumX*sumX));
            }
            else { return 0; }
        }
        private double calulateOffset(double m, double[] Xs, double[] Ys)
        {
            if(Xs.Length == Ys.Length)
            {
                int n = Xs.Length;
                double sumY = 0;
                double sumX = 0;
                for (int i = 0; i < n; i++)
                {
                    sumX += Xs[i];
                    sumY += Ys[i];
                }
                return (sumY - m*sumX) / n;
            }
            else { return 0; }
        }

        private void btn_save_calibration_Click(object sender, EventArgs e)
        {
            int n = 0;
            foreach(MetroSetCheckBox ckb in ckbs_calib)
            {
                if (ckb.Checked)
                {
                    ignoreCellChange = true;
                    gains[n] = gain;
                    dgv_setCalibration.Rows[n].Cells[1].Value = gain;
                    offsets[n] = offset;
                    dgv_setCalibration.Rows[n].Cells[2].Value = offset;
                    btns_calib[n].BackgroundImage = Properties.Resources.enabled_check;
                    ignoreCellChange = false;
                }
                n++;
            }
        }

        private void setSavedCalibrations()
        {
            string g = "";
            string o = "";
            // Build save sting
            for (int n = 0; n < gains.Count; n++)
            {
                g += gains[n].ToString() + ",";
                o += offsets[n].ToString() + ",";
            }
            Properties.Settings.Default.gains = g;
            Properties.Settings.Default.offsets = o;
        }


        // This gets the users saved calibration form the Settings
        private void getSavedCalibrations()
        {
            metroSetTabControl2.SelectedIndex = Properties.Settings.Default.tab;
            // Properties.Settings.Default.Reset();
            string[] gs = Properties.Settings.Default.gains.Split(',');
            string[] os = Properties.Settings.Default.offsets.Split(',');
            double g;
            double o;
            for(int n = 0; n < btns_calib.Count; n++)
            {
                try
                {
                    g = Convert.ToDouble(gs[n]);
                    o = Convert.ToDouble(os[n]);
                    if(g == 1 && o == 0) // Defalut calibration
                    {
                        btns_calib[n].BackgroundImage = Properties.Resources.nail_and_gear;
                        tlt_calibration.SetToolTip(btns_calib[n], "Calibration needed");                    
                    }
                    else
                    {
                        btns_calib[n].BackgroundImage = Properties.Resources.enabled_check;
                        tlt_calibration.SetToolTip(btns_calib[n], "Click to reset calibration");
                    }
                    gains.Add(n, g);
                    offsets.Add(n, o);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }
            ignoreCellChange = true;
            // Name each row
            dgv_setCalibration.Rows.Add();
            dgv_setCalibration.Rows.Add();
            dgv_setCalibration.Rows.Add();
            dgv_setCalibration.Rows.Add();
            dgv_setCalibration.Rows[0].Cells[0].Value = "Supply Tank Sensor";
            dgv_setCalibration.Rows[1].Cells[0].Value = "Upstream Sensor";
            dgv_setCalibration.Rows[2].Cells[0].Value = "Downstream Sensor";
            dgv_setCalibration.Rows[3].Cells[0].Value = "Delivery Tank Sensor";
            dgv_setCalibration.Rows[4].Cells[0].Value = "Flow Sensor";
            // Iterage through gains and offsets to update table
            foreach (var v in gains)
            {
                dgv_setCalibration.Rows[v.Key].Cells[1].Value = v.Value;
            }
            foreach (var v in offsets)
            {
                dgv_setCalibration.Rows[v.Key].Cells[2].Value = v.Value;
            }
            ignoreCellChange = false;
        }

        private void updateStreamsWithCalibration()
        {
            foreach(PhidgetStream stream in all_streams)
            {
                switch (stream.getName())
                {
                    case "Supply Tank Presure":
                        stream.setGain(gains[0]);
                        stream.setOffset(offsets[0]);
                        break;
                    case "Upstream ECB Pressure":
                        stream.setGain(gains[1]);
                        stream.setOffset(offsets[1]);
                        break;
                    case "Downstream ECB Presure":
                        stream.setGain(gains[2]);
                        stream.setOffset(offsets[2]);
                        break;
                    case "Delivery Tank Pressure":
                        stream.setGain(gains[3]);
                        stream.setOffset(offsets[3]);
                        break;
                    case "Flow Sensor":
                        stream.setGain(gains[4]);
                        stream.setOffset(offsets[4]);
                        break;
                    default:
                        break;

                }
            }
        }

        private void metroSetNumeric_MouseDown(object sender, MouseEventArgs e)
        {
            MetroSetNumeric metroSetNumeric = (MetroSetNumeric)sender;
            currentNumeric = metroSetNumeric.Value;
        }

        private void metroSetNumeric_Click(object sender, EventArgs e)
        {
            MetroSetNumeric metroSetNumeric = (MetroSetNumeric)sender;
            metroSetNumeric.Value = currentNumeric;
            switch (metroSetNumeric.Name)
            {
                case "metroSetNumeric1":
                    Properties.Settings.Default.targetPressure = currentNumeric;
                    break;
                case "metroSetNumeric2":
                    Properties.Settings.Default.supplyTankVol = currentNumeric;
                    break;
                case "metroSetNumeric3":
                    Properties.Settings.Default.deliveryTankVol = currentNumeric;
                    break;
                default:
                    break;
            }

        }

        private void setSavedNumerics()
        {
            metroSetNumeric1.Value = Properties.Settings.Default.targetPressure;
            metroSetNumeric2.Value = Properties.Settings.Default.supplyTankVol;
            metroSetNumeric3.Value = Properties.Settings.Default.deliveryTankVol;
        }

        private void dgv_setCalibration_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!(e.RowIndex == -1) && !ignoreCellChange)
            {
                DataGridView dgv = sender as DataGridView;
                double value = Convert.ToDouble(dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                int row = e.RowIndex;
                int column = e.ColumnIndex;

                // See if gain or offset is changed
                if (column == 1)
                {
                    gains[row] = value;
                } 
                else if (column == 2)
                {
                    offsets[row] = value;
                }
                updateStreamsWithCalibration();
                setSavedCalibrations();
            }
        }

        private void txbEquation_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            // Find the numberber of charicters in the each factor
            string[] values = getEquationVariables(textBox.Text);
            int[] startPositions = { 2, 2 + values[0].Length + 9, 2 + values[0].Length + 9 + values[1].Length + 9 };
            
            // Find position of curser
            
            // Logical test
            bool isNum = ((e.KeyValue >= 48 && e.KeyValue <= 57) || e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.OemMinus);
            bool isArroow = (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right);
            bool isRemove = (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back);
            decimal num = 0;  // Not used
            bool isNumSelected = decimal.TryParse(textBox.SelectedText, out num);

            if (isNum)  // Allow numbers and period only in input zones
            {
                if (isNumSelected)
                {
                    return;
                } 
                else if ((textBox.SelectionStart >= startPositions[0] && textBox.SelectionStart <= startPositions[0] + values[0].Length) || (textBox.SelectionStart >= startPositions[1] && textBox.SelectionStart <= startPositions[1] + values[1].Length) || (textBox.SelectionStart >= startPositions[2] && textBox.SelectionStart <= startPositions[2] + values[2].Length))
                {
                    return;
                }
                else 
                { 
                    e.SuppressKeyPress = true;
                }
            } 
            else if (isArroow)  // Allow arrows anywhere
            {
                return;
            } 
            else if (isRemove)  // Allow deleate and backsbace in safe zone but not at edge, and only if selected text is numeric
            {
                if (isNumSelected)
                {
                    return;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    if ((textBox.SelectionStart > startPositions[0] && textBox.SelectionStart <= startPositions[0] + values[0].Length) || (textBox.SelectionStart > startPositions[1] && textBox.SelectionStart <= startPositions[1] + values[1].Length) || (textBox.SelectionStart > startPositions[2] && textBox.SelectionStart <= startPositions[2] + values[2].Length))
                    {
                        return;
                    }
                    else
                    {
                        e.SuppressKeyPress = true;
                    }
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    if ((textBox.SelectionStart >= startPositions[0] && textBox.SelectionStart < startPositions[0] + values[0].Length) || (textBox.SelectionStart >= startPositions[1] && textBox.SelectionStart < startPositions[1] + values[1].Length) || (textBox.SelectionStart >= startPositions[2] && textBox.SelectionStart < startPositions[2] + values[2].Length))
                    {
                        return;
                    }
                    else
                    {
                        e.SuppressKeyPress = true;
                    }
                }
                else
                {
                    e.SuppressKeyPress = true;
                }
            }
            else
            {
                e.SuppressKeyPress = true;
            }
        }
        private string[] getEquationVariables(string equation)
        {
            double[] returnVal = { 1, 2, 3};
            equation = equation.Replace("= ", ""); // Remoce leading equals
            equation = equation.Replace(" * P_d + ", ",");  // add firest comma
            equation = equation.Replace(" * P_s + ", ",");  // add secound comma
            string[] strVals = equation.Split(',');  // Split at commas
            return strVals;
        }

        private double[] getEquationValues(string equation)
        {
            double[] returnVal = { 1, 2, 3 };
            equation = equation.Replace("= ", ""); // Remoce leading equals
            equation = equation.Replace(" * P_d + ", ",");  // add firest comma
            equation = equation.Replace(" * P_s + ", ",");  // add secound comma
            string[] strVals = equation.Split(',');  // Split at commas
            // convert strings to doubles
            try { returnVal[0] = double.Parse(strVals[0]); }
            catch { returnVal[0] = 0; }

            try { returnVal[1] = double.Parse(strVals[1]); }
            catch { returnVal[1] = 0; }

            try { returnVal[2] = double.Parse(strVals[2]); }
            catch { returnVal[2] = 0; }

            return returnVal;
        }

        private void txbEquation_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            Properties.Settings.Default.equation = textBox.Text;
        }
    }
}
