using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Phidget22;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace ECB_Testing_Program
{
    /*
     * This class is the for building and handeling the tree that represents the user 
     * interface for the each chanel or streem of phidget device that has been connected.
     */
    public class PhidgetTree
    {

        public TreeView this_view;

        #region Constructors
        //
        // Constructors
        //
        public PhidgetTree(TreeView tree_view)
        {
            this_view = tree_view;
        }
        #endregion

        public void update(Phidget phidget)
        {
            // Do not process chanels that are actualy hubs
            if (!(phidget.ChannelClass == ChannelClass.Hub)) 
            {
                // Create a node for the current phidget
                TreeNode phidgetNode = new TreeNode(phidget.DeviceName);
                // Pass other pertinent infor in as the node tag
                phidgetNode.Tag = phidget;
                

                if (phidget.IsHubPortDevice)
                {
                    bool portNodeExist = false;
                    bool nodeOcupied = false;
                    bool parentNodeExist = false;
                    TreeNode parentNode = new TreeNode();
                    TreeNode portNode = new TreeNode();
                    // Check to see if the parent has been created
                    foreach (var node in Collect(this_view.Nodes))
                    {
                        Phidget isParent = (Phidget)node.Tag;
                        Phidget parent = phidget.Parent.Parent;
                        if (parent == null || isParent == null)
                        {
                            break;
                        }
                        if (parent.DeviceSerialNumber == isParent.DeviceSerialNumber && parent.HubPort == isParent.HubPort && parent.IsHubPortDevice == isParent.IsHubPortDevice && (!parent.IsChannel && !isParent.IsChannel || parent.Channel == isParent.Channel))
                        {
                            // Check to see if a node for that port exist
                            foreach (var pnode in Collect(node.Nodes))
                            {
                                if (pnode.Text == "Port " + (phidget.HubPort).ToString() || pnode.Text.Split('-')[0] == "Port " + (phidget.HubPort).ToString() + " ")
                                {
                                    if (pnode.Text.Split('-').Length > 1) // || 
                                    {
                                        nodeOcupied = true;
                                    }
                                    portNodeExist = true;
                                    portNode = pnode;
                                }
                            }
                            parentNodeExist = true;
                            parentNode = node;
                            break; // Get our of the for loop
                        }
                    }
                    if (!nodeOcupied) // Ignore if node is ocupied
                    {
                        if (parentNodeExist)
                        {
                            if (portNodeExist)
                            {
                                portNode.Nodes.Add(phidgetNode);
                            }
                            else
                            {
                                TreeNode tempNode = new TreeNode("Port " + (phidget.HubPort).ToString());
                                tempNode.Tag = phidget.Parent;
                                tempNode.Nodes.Add(phidgetNode);
                                parentNode.Nodes.Add(tempNode);
                            }
                        }
                        else
                        {
                            // In this case we made it to the root and found no sutible parent so we need to make one
                            // TreeNode[] tempCildren = { phidgetNode };
                            TreeNode tempNode = new TreeNode("Port " + (phidget.HubPort).ToString());
                            tempNode.Tag = phidget.Parent;
                            tempNode.Nodes.Add(phidgetNode);
                            TreeNode p = new TreeNode(phidget.Hub.DeviceName + " - " + phidget.DeviceSerialNumber);
                            p.Nodes.Add(tempNode);
                            // phidgetNode = new TreeNode(phidget.Parent.DeviceName, tempCildren); // creat node for parrent
                            p.Tag = phidget.Parent.Parent;
                            this_view.Nodes.Add(p);
                        }
                    }
                }
                else
                {
                    while (true)  // Work upstreem till reach root or tree
                    {
                        // Build an image of what the upstream nodes should look like, defining charictoristics are: S/N, HubPort, IsHubPortDivice, and Channel
                        bool parentNodeExist = false; // Assume no parent can be founs
                        TreeNode parentNode = new TreeNode();
                        // Iterate through current nodes
                        foreach (var node in Collect(this_view.Nodes))
                        {
                            Phidget isParent = (Phidget) node.Tag;
                            Phidget parent = phidget.Parent;
                            if (parent == null || isParent == null)
                            {
                                break;
                            }
                            // Test enach node against Parent 
                            if (parent.DeviceSerialNumber == isParent.DeviceSerialNumber && parent.HubPort == isParent.HubPort && parent.IsHubPortDevice == isParent.IsHubPortDevice && (!parent.IsChannel && !isParent.IsChannel || parent.Channel == isParent.Channel))
                            {
                                parentNodeExist = true; // Set test to true
                                parentNode = node;
                                break; // Get our of the for loop
                            }
                        }
                        // See if such a node exist, if so add this node to it's collection and exit, if not create one and iterate
                        if (parentNodeExist)
                        {
                            // In ths case you found a branch
                            // If the parent branch is a VENT hub then the name should be the port that it is in
                            
                            if (phidget.Parent.DeviceClass == DeviceClass.Hub)
                            {
                                phidgetNode.Text = "Port " + (phidget.HubPort).ToString() + " - " + phidget.DeviceName;
                            }
                            else if (phidget.IsChannel)
                            {
                                phidgetNode.Text = phidget.ChannelName + " - Chanel " + phidget.Channel;
                            }

                            // If the phidget is not a port device, then remove all other port devises

                            parentNode.Nodes.Add(phidgetNode);
                            break;
                        } else if(phidget.Parent == null)
                        {
                            // This case means that the phidget is a usb device and needs to be added as root                            
                            Phidget node = (Phidget) phidgetNode.Tag;
                            phidgetNode.Text = node.DeviceName + " - " + node.DeviceSerialNumber;
                            this_view.Nodes.Add(phidgetNode);
                            break;
                        }
                        else 
                        {
                            // In this case we need to keep looking
                            // If the parent branch is a VENT hub then the name should be the port that it is in
                            if (phidget.Parent.DeviceClass == DeviceClass.Hub)
                            {
                                phidgetNode.Text = "Port " + (phidget.HubPort).ToString() + " - " + phidget.DeviceName;
                            } else if (phidget.IsChannel)
                            {
                                phidgetNode.Text = phidget.ChannelName + " - Chanel " + phidget.Channel;
                            }
                            TreeNode[] tempCildren = { phidgetNode };
                            phidgetNode = new TreeNode(phidget.Parent.DeviceName, tempCildren); // creat node for parrent
                            phidgetNode.Tag = phidget.Parent;
                            // replace phidget with parent
                            phidget = phidget.Parent;
                        }
                    }
                    // remove any HubPortDevices that currently ocupy this hub
                }
            }
            this_view.Sort();
            this_view.EndUpdate();
        }

        
        public void remove(Phidget phidget)
        {
            List<TreeNode> toRemove = new List<TreeNode>(); ;
            // Iterate the current phidgets in tree
            foreach (TreeNode node in Collect(this_view.Nodes))
            {
                // Ignore null nodes
                if (!(node == null)) {
                    Phidget phgNode = (Phidget)node.Tag;
                    // compare each node to the phidget chanel that needs to be removed
                    if (phgNode.DeviceSerialNumber == phidget.DeviceSerialNumber && phgNode.HubPort == phidget.HubPort && ((phgNode.IsChannel && phidget.IsChannel) && phgNode.Channel == phidget.Channel || (!phgNode.IsChannel && !phidget.IsChannel)))
                    {
                        // Add to list to remove
                        toRemove.Add(node);
                        // Remove this node
                        //this_view.Nodes.Remove(node);
                    }
                }
            }
            foreach (TreeNode n in toRemove)
            {
                this_view.Nodes.Remove(n);
            }
            this_view.Sort();
            this_view.EndUpdate();
        }

        #region helper functions
        //
        // This helps to iterate through each node in tree structure
        //
        IEnumerable<TreeNode> Collect(TreeNodeCollection nodes)
        {
            foreach(TreeNode node in nodes)
            {
                yield return node;
                foreach (var child in Collect(node.Nodes))
                    yield return child;
            }
        }
        #endregion

    }
}
