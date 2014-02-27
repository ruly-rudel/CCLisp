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
        private CCInterpreter interp;

        public MainWindow()
        {
            InitializeComponent();

            interp = new CCInterpreter();
        }

        private void Eval_Click(object sender, RoutedEventArgs e)
        {
            EvalResult.Text = "";
            using (var sr = new StringReader(EvalText.Text))
            {
                IEnumerable<CCObject> obj;
                try
                {
                    obj = interp.Read(sr);

                    foreach (var i in obj)
                    {
                        var c = interp.Compile(i);
                        EvalResult.Text += c.ToString() + "\n";
                        //var eobj = interp.GetResult();
                        //if (eobj != null)
                        //{
                        //    EvalResult.Text += eobj.ToString();
                        //}
                        //else
                        //{
                        //    EvalResult.Text += "()\n";
                        //}
                    }

                }
                catch(CCException ex)
                {
                    EvalResult.Text += ex.Message;
                }
            }




            //var eobj = interp.Eval(obj);
            //if (eobj != null)
            //{
            //    EvalResult.Text += eobj.ToString();
            //}
            //else
            //{
            //    EvalResult.Text = interp.LogString;
            //}
        }
    }
}
