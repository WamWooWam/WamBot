using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WamBotRewrite.UI
{
    partial class MainWindow : Window
    {
        public TextBox BotLog => botLog;

        public MainWindow()
        {
            InitializeComponent();
        }

        public async void Window_Loaded(object sender, EventArgs e)
        {
            await Program.NormalMain();
        }
    }
}
