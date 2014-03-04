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
            comp = new CCCompiler();
            vm = new CCVM();
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
                        EvalResult.Text += c.ToString() + "\n";
                        vm.Eval(c);
                        EvalResult.Text += vm.GetResult();
                    }

                }
                catch(CCException ex)
                {
                    EvalResult.Text += ex.Message;
                }
            }
        }
    }
}
