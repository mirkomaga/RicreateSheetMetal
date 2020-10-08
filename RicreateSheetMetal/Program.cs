using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RicreateSheetMetal
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main()
        {
            RicompongoLamiera.saveAllAsDwg("X:\\Commesse\\Focchi\\160010 BPS\\07.Studi\\Marco\\BH\\05. EWS506\\Emissione Staffe EWS-506\\Emissione 02 - Cicli Pannelli\\Pack and go BH_EWS-506 - Cicli Pannelli_02 (05.10.2020)\\160010BPS_SANDWICHES_EWS506_Rev02\\_IDW\\_CICLIDACORREGGERETestMirko");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
    public static class GenericFunction
    {
        public static string chooseFolder(bool write = true)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    string[] files = Directory.GetFiles(fbd.SelectedPath);

                    if (write)
                    {
                        if (files.Length > 0)
                        {
                            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                            DialogResult flrCheck = MessageBox.Show("Cartella non vuota, i file duplicati verranno sostituiti", "Attenzione", buttons);
                            if (flrCheck == DialogResult.Yes)
                            {
                                //this.Close();
                            }
                            else
                            {
                                //simulo il click
                                chooseFolder();
                            }
                        }
                    }
                    return fbd.SelectedPath;
                }
                return null;
            }
        }
        public static string chooseFile(string filter)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = filter;
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    try
                    {
                        var fileStream = openFileDialog.OpenFile();


                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            fileContent = reader.ReadToEnd();
                        }
                    }
                    catch { }
                }
            }

            return filePath;
        }
        public static int countFiles(string path, string extension)
        {
            int fCount = Directory.GetFiles(path, extension, SearchOption.AllDirectories).Length;
            return fCount;
        }
    }
}
