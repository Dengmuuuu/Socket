using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Core
{
    public static class Utils
    {
        public static void Show(RichTextBox richtextbox,string str)
        {
            Run run = new Run(str);
            Paragraph p = new Paragraph();
            p.Inlines.Add(run);
            richtextbox.Document.Blocks.Add(p);
        }
    }
}
