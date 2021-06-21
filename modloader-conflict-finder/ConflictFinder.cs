using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IMGSharp;

namespace modloader_conflict_finder
{
    public partial class ConflictFinder : Form
    {
        private string _path;
        private Dictionary<string, List<string>> _listOfFiles = new Dictionary<string, List<string>>();

        private string[] fileExclusions = {
            @".data\",
            @".profiles\",
            "modloader.ini",
            "modloader.log"
        };

        public ConflictFinder()
        {
            InitializeComponent();
        }

        private void openFolder_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                if (dialog.FileName.Split(new char[] { Path.DirectorySeparatorChar }, options: StringSplitOptions.RemoveEmptyEntries).Last().ToLower() == "modloader")
                {
                    textBox1.Text = dialog.FileName;
                    _path = dialog.FileName;
                }
                else
                {
                    MessageBox.Show("That doesn't look like a modloader folder...", "Folder Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            UpdateButtonAvailability();
        }

        private void filePath_TextChanged(object sender, EventArgs e)
        {
            _path = textBox1.Text.TrimEnd('\\');
            UpdateButtonAvailability();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_path) && _path.Split(new char[] { Path.DirectorySeparatorChar }, options: StringSplitOptions.RemoveEmptyEntries).Last().ToLower() == "modloader")
            {
                try
                {
                    _listOfFiles = new Dictionary<string, List<string>>();

                    string[] allFiles = Directory.GetFiles(_path, "*", SearchOption.AllDirectories);
                    foreach (string file in allFiles)
                    {
                        if (fileExclusions.Any(s => s.StartsWith(file.Substring(_path.Length).Split(new char[] { Path.DirectorySeparatorChar }, options: StringSplitOptions.RemoveEmptyEntries).First())))
                        {
                            continue;
                        }

                        string justName = file.Split(new char[] { Path.DirectorySeparatorChar }, options: StringSplitOptions.RemoveEmptyEntries).Last();

                        if (justName.EndsWith(".img"))
                        {
                            using (IIMGArchive archive = IMGFile.Open(file, EIMGArchiveAccessMode.Read))
                            {
                                foreach (var entry in archive.Entries)
                                {
                                    string entryName = entry.Value.Name;
                                    if (_listOfFiles.ContainsKey(entryName))
                                    {
                                        _listOfFiles[entryName].Add(string.Format("{0} ({1})", file, entry.Value.FullName));
                                    }
                                    else
                                    {
                                        _listOfFiles.Add(entryName, new List<string> { string.Format("{0} ({1})", file, entry.Value.FullName) });
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (_listOfFiles.ContainsKey(justName))
                            {
                                _listOfFiles[justName].Add(file);
                            }
                            else
                            {
                                _listOfFiles.Add(justName, new List<string> { file });
                            }
                        }
                    }

                    UniqueFilenames.Items.Clear();

                    if (_listOfFiles.Count(l => l.Value.Count > 1) > 0)
                    {
                        foreach (string key in _listOfFiles.Where(l => l.Value.Count > 1).Select(kvp => kvp.Key))
                        {
                            UniqueFilenames.Items.Add(key);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Finished looking through directories, no duplicate files found.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Something failed, logging to file.");
                    LogSomething(@".\logger.txt", ex);
                }
            }
            else
            {
                MessageBox.Show("That doesn't look like a modloader folder...", "Folder Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UniqueFilenames_SelectedIndexChanged(object sender, EventArgs e)
        {
            string keyToLookFor = UniqueFilenames.SelectedItem.ToString();
            List<string> conflictingFileNames = _listOfFiles[keyToLookFor];

            if (conflictingFileNames.Count > 1)
            {
                DuplicateFilenames.Items.Clear();
                foreach (string file in conflictingFileNames)
                {
                    DuplicateFilenames.Items.Add(file.Substring(_path.Length));
                }
            }
            else
            {
                MessageBox.Show("For some reason this file was shown, but there was only one copy of it found so it doesn't have any conflicts... apparently.");
            }
        }

        private void UpdateButtonAvailability()
        {
            goButton.Enabled = !string.IsNullOrWhiteSpace(_path) && _path.Split(new char[] { Path.DirectorySeparatorChar }, options: StringSplitOptions.RemoveEmptyEntries).Last().ToLower() == "modloader";
        }

        protected void LogSomething(string path, Exception ex)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine("-----------------------------------------------------------------------------");
                    writer.WriteLine("Date: " + DateTime.Now.ToString());
                    writer.WriteLine();

                    while (ex != null)
                    {
                        writer.WriteLine(ex.GetType().FullName);
                        writer.WriteLine("Message : " + ex.Message);
                        writer.WriteLine("StackTrace : " + ex.StackTrace);

                        ex = ex.InnerException;
                    }
                }
            }
            catch (Exception ex2)
            {
                MessageBox.Show("Yeah, can't write exception to file, might need to run this as admin or move it into another folder.");
            }
        }
    }
}