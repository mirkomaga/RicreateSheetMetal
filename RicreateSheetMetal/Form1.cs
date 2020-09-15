using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RicreateSheetMetal
{
    public partial class Form1 : Form
    {
        private static string folder;
        public Form1()
        {
            InitializeComponent();
        }

        private void btn1_Click(object sender, EventArgs e)
        {
            folder = GenericFunction.chooseFolder(false);

            tb1.Text = folder;
        }

        private void btn2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(folder))
            {
                RicompongoLamiera.main(folder, toolStripProgressBar1, toolStripStatusLabel1, listView1);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Selezionare cartella");
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
