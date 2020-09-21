﻿using System;
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
            RicompongoLamiera.saveAllAsDxf("X:\\Commesse\\NuovaSaf\\PDL\\Edificio_F");

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
