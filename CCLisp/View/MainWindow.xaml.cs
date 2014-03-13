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

using CCLisp.Model;
using System.IO;
using Microsoft.Win32;

namespace CCLisp.View
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private CCParser parser;
        private CCCompiler comp;
        private CCVM vm;

        public MainWindow()
        {
            InitializeComponent();

            parser = new CCParser();
            vm = new CCVM();
            comp = new CCCompiler(vm);
        }

        private void Eval_Click(object sender, RoutedEventArgs e)
        {
            EvalResult.Text = "";
            using (var sr = new StringReader(EvalText.Text))
            {
                IEnumerable<CCObject> obj;
                try
                {
                    obj = parser.Read(sr);

                    foreach (var i in obj)
                    {
                        var c = comp.Compile(i);
                        EvalResult.Text += "Compile result:\n" + c.ToString() + "\n\n";
                        vm.Eval(c);
                        var result = vm.GetResult();
                        EvalResult.Text += "evaluated value:\n" + result + "\n\n";
                    }

                }
                catch(CCException ex)
                {
                    EvalResult.Text += ex.Message;
                }
            }
        }


        private void MenuFileLoad_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Lisp Core (.core)| *.core";
            ofd.Multiselect = false;
            bool? result = ofd.ShowDialog();

            if (result == true)
            {
                vm.LoadCore(ofd.FileName);
                comp.LoadSymbol(ofd.FileName);
            }
        }

        private void MenuFileSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Lisp Core (.core)| *.core";

            bool? result = sfd.ShowDialog();

            if (result == true)
            {
                vm.SaveCore(sfd.FileName);
                comp.SaveSymbol(sfd.FileName);
            }

        }

        private void MenuFileQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
