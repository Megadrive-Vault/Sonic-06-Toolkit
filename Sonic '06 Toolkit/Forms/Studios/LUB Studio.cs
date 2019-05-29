﻿using System;
using System.IO;
using System.Windows.Forms;

namespace Sonic_06_Toolkit
{
    public partial class LUB_Studio : Form
    {
        public LUB_Studio()
        {
            InitializeComponent();
        }

        void LUB_Studio_Load(object sender, EventArgs e)
        {
            VerifyLUBs();
        }

        void VerifyLUBs()
        {
            #region Getting and verifying Lua binaries...
            //Checks the header for each file to ensure that it can be safely decompiled then adds all decompilable LUBs in the current path to the CheckedListBox.
            foreach (string LUB in Directory.GetFiles(Tools.Global.currentPath, "*.lub", SearchOption.TopDirectoryOnly))
            {
                if (File.Exists(LUB))
                {
                    if (File.ReadAllLines(LUB)[0].Contains("LuaP"))
                    {
                        clb_LUBs.Items.Add(Path.GetFileName(LUB));
                    }
                }
            }
            //Checks if there are any LUBs in the directory.
            if (clb_LUBs.Items.Count == 0)
            {
                MessageBox.Show("There are no Lua binaries to decompile in this directory.", "No Lua binaries available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            #endregion
        }

        void Btn_SelectAll_Click(object sender, EventArgs e)
        {
            //Checks all available checkboxes.
            for (int i = 0; i < clb_LUBs.Items.Count; i++) clb_LUBs.SetItemChecked(i, true);
            btn_Decompile.Enabled = true;
        }

        void Btn_DeselectAll_Click(object sender, EventArgs e)
        {
            //Unchecks all available checkboxes.
            for (int i = 0; i < clb_LUBs.Items.Count; i++) clb_LUBs.SetItemChecked(i, false);
            btn_Decompile.Enabled = false;
        }

        void Btn_Decompile_Click(object sender, EventArgs e)
        {
            //In the odd chance that someone is ever able to click Decompile without anything selected, this will prevent that.
            if (clb_LUBs.CheckedItems.Count == 0) MessageBox.Show("Please select a Lua binary.", "No Lua binaries specified", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try
            {
                //Gets all checked boxes from the CheckedListBox and builds a string for each LUB.
                foreach (string selectedLUB in clb_LUBs.CheckedItems)
                {
                    Tools.Global.lubState = "decompile";
                    Tools.LUB.Decompile(string.Empty, selectedLUB);
                }

                clb_LUBs.Items.Clear();
                VerifyLUBs();
            }
            catch
            {
                MessageBox.Show("An error occurred when decompiling the selected Lua binaries.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Tools.Notification.Dispose();
            }
        }

        void Clb_LUBs_SelectedIndexChanged(object sender, EventArgs e)
        {
            clb_LUBs.ClearSelected(); //Removes the blue highlight on recently checked boxes.

            //Enables/disables the Decompile button, depending on whether a box has been checked.
            if (clb_LUBs.CheckedItems.Count > 0)
            {
                btn_Decompile.Enabled = true;
            }
            else
            {
                btn_Decompile.Enabled = false;
            }
        }

        void LUB_Studio_FormClosing(object sender, FormClosingEventArgs e)
        {
            Tools.Global.lubState = null;
        }
    }
}