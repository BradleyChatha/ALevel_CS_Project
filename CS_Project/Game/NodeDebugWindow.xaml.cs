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
using System.Windows.Shapes;

namespace CS_Project.Game
{
    /// <summary>
    /// Interaction logic for NodeDebugWindow.xaml
    /// </summary>
    public partial class NodeDebugWindow : Window
    {
        public NodeDebugWindow()
        {
            InitializeComponent();

            this.Title += $" {Config.versionString}";
        }

        /// <summary>
        /// Updates the data shown in the tree view.
        /// </summary>
        /// <param name="root">The root of the tree to display.</param>
        public void updateNodeData(Node root)
        {
            this.root.Items.Clear();

            root.walkEveryPath(path => 
            {
                TreeViewItem parent = this.root;

                foreach(var node in path)
                {
                    // First, make sure we're not duplicating nodes.
                    // We do this by using the node's hash as the TreeViewItem's name.
                    // So we can just check the names to make sure we're not duplicating data.
                    var  nodeName   = node.hash.ToString().Replace('.', '_'); // WPF names can't use full stops.
                    bool doContinue = false;
                    foreach (ItemsControl con in parent.Items)
                    {
                        if (con.Name == nodeName)
                        {
                            parent     = con as TreeViewItem;
                            doContinue = true;
                            break;
                        }
                    }

                    if (doContinue)
                        continue;

                    // Then, make the new node
                    var item = new TreeViewItem();
                    item.Header =  "--------------------\n"
                                + $"Hash: {node.hash}\n"
                                + $"Index: {node.index}\n"
                                + $"Wins: {node.won}({node.winPercent}%)\n"
                                + $"Losses: {node.lost}({node.losePercent}%)";
                    item.Name = nodeName;

                    // Finally, add it to the tree
                    parent.Items.Add(item);
                    parent = item;
                } 
            });
        }
    }
}
