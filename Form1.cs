using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace pline
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            object tLong = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Autodesk\\AutoCAD\\R19.1\\ACAD-D001:804\\Applications\\RoundPline", "SumLine", 0);
            textBox1.Text = (string)tLong;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string t1 = textBox1.Text;
            if (!int.TryParse(t1, out _))//如果转换失败（为false）时输出括号内容
            {
                MessageBox.Show("请输入正整数");
            }

            else
            {
                string SumLine = textBox1.Text;
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Autodesk\\AutoCAD\\R19.1\\ACAD-D001:804\\Applications\\RoundPline", "SumLine", SumLine);
                textBox1.Text = SumLine;

            }

        }

        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                string t1 = textBox1.Text;
                if (!int.TryParse(t1, out _))//如果转换失败（为false）时输出括号内容
                {
                    MessageBox.Show("请输入正整数");
                }
                else
                {
                    string SumLine = textBox1.Text;
                    Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Autodesk\\AutoCAD\\R19.1\\ACAD-D001:804\\Applications\\RoundPline", "SumLine", SumLine);
                    textBox1.Text = SumLine;
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Autodesk\\AutoCAD\\R19.1\\ACAD-D001:804\\Applications\\RoundPline", "FormDisplay", "off");
        }
    }
}
