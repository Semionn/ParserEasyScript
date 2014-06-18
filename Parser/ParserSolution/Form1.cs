using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ParserNamespace
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Test1();
        }

        public void Test1()
        {
            Source s = new Source(textBox1.Text);
            Context context = new Context(null);
            Expression sp = new Expression(ref s);
            label1.Text = "Результат: " + sp.ToString();
            label2.Text = "Дерево: " + Environment.NewLine + sp.PrintTree();
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Test1();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }

        private void button2_Click(object sender, EventArgs e)
        {
            ProgramParse ps = new ProgramParse();
            ps.Parse(textBox2.Text);
            ps.Run();
            textBox3.Text = ps.output;
        }
    }
}
