﻿namespace EDDiscovery.Forms
{
    partial class SafeModeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonDbs = new System.Windows.Forms.Button();
            this.buttonNormal = new System.Windows.Forms.Button();
            this.buttonPositions = new System.Windows.Forms.Button();
            this.buttonResetTheme = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonDeleteSystemDB = new System.Windows.Forms.Button();
            this.buttonResetTabs = new System.Windows.Forms.Button();
            this.buttonRemoveDLLs = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonDbs
            // 
            this.buttonDbs.Location = new System.Drawing.Point(89, 181);
            this.buttonDbs.Name = "buttonDbs";
            this.buttonDbs.Size = new System.Drawing.Size(193, 23);
            this.buttonDbs.TabIndex = 0;
            this.buttonDbs.Text = "Move Databases";
            this.buttonDbs.UseVisualStyleBackColor = true;
            this.buttonDbs.Click += new System.EventHandler(this.buttonDbs_Click);
            // 
            // buttonNormal
            // 
            this.buttonNormal.Location = new System.Drawing.Point(89, 262);
            this.buttonNormal.Name = "buttonNormal";
            this.buttonNormal.Size = new System.Drawing.Size(193, 23);
            this.buttonNormal.TabIndex = 0;
            this.buttonNormal.Text = "Run";
            this.buttonNormal.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonNormal.UseVisualStyleBackColor = true;
            this.buttonNormal.Click += new System.EventHandler(this.Run_Click);
            // 
            // buttonPositions
            // 
            this.buttonPositions.Location = new System.Drawing.Point(89, 40);
            this.buttonPositions.Name = "buttonPositions";
            this.buttonPositions.Size = new System.Drawing.Size(193, 23);
            this.buttonPositions.TabIndex = 0;
            this.buttonPositions.Text = "Reset Window Positions";
            this.buttonPositions.UseVisualStyleBackColor = true;
            this.buttonPositions.Click += new System.EventHandler(this.buttonPositions_Click);
            // 
            // buttonResetTheme
            // 
            this.buttonResetTheme.Location = new System.Drawing.Point(89, 10);
            this.buttonResetTheme.Name = "buttonResetTheme";
            this.buttonResetTheme.Size = new System.Drawing.Size(193, 23);
            this.buttonResetTheme.TabIndex = 0;
            this.buttonResetTheme.Text = "Reset Theme";
            this.buttonResetTheme.UseVisualStyleBackColor = true;
            this.buttonResetTheme.Click += new System.EventHandler(this.buttonResetTheme_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(89, 292);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(193, 23);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonDeleteSystemDB
            // 
            this.buttonDeleteSystemDB.Location = new System.Drawing.Point(89, 210);
            this.buttonDeleteSystemDB.Name = "buttonDeleteSystemDB";
            this.buttonDeleteSystemDB.Size = new System.Drawing.Size(193, 23);
            this.buttonDeleteSystemDB.TabIndex = 0;
            this.buttonDeleteSystemDB.Text = "Delete/Rebuild System DB";
            this.buttonDeleteSystemDB.UseVisualStyleBackColor = true;
            this.buttonDeleteSystemDB.Click += new System.EventHandler(this.buttonDeleteSystemDB_Click);
            // 
            // buttonResetTabs
            // 
            this.buttonResetTabs.Location = new System.Drawing.Point(89, 70);
            this.buttonResetTabs.Name = "buttonResetTabs";
            this.buttonResetTabs.Size = new System.Drawing.Size(193, 23);
            this.buttonResetTabs.TabIndex = 0;
            this.buttonResetTabs.Text = "Reset Tabs, Remove PopOuts";
            this.buttonResetTabs.UseVisualStyleBackColor = true;
            this.buttonResetTabs.Click += new System.EventHandler(this.buttonResetTabs_Click);
            // 
            // buttonRemoveDLLs
            // 
            this.buttonRemoveDLLs.Location = new System.Drawing.Point(89, 100);
            this.buttonRemoveDLLs.Name = "buttonRemoveDLLs";
            this.buttonRemoveDLLs.Size = new System.Drawing.Size(193, 23);
            this.buttonRemoveDLLs.TabIndex = 0;
            this.buttonRemoveDLLs.Text = "Remove all Extension DLLs";
            this.buttonRemoveDLLs.UseVisualStyleBackColor = true;
            this.buttonRemoveDLLs.Click += new System.EventHandler(this.buttonRemoveDLLs_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(89, 130);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(193, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Remove all Action Packs";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // SafeModeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(370, 333);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonNormal);
            this.Controls.Add(this.buttonDeleteSystemDB);
            this.Controls.Add(this.buttonDbs);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonRemoveDLLs);
            this.Controls.Add(this.buttonResetTabs);
            this.Controls.Add(this.buttonPositions);
            this.Controls.Add(this.buttonResetTheme);
            this.Icon = global::EDDiscovery.Properties.Resources.edlogo_3mo_icon;
            this.Name = "SafeModeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EDDiscovery Safe Mode";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button buttonDbs;
        private System.Windows.Forms.Button buttonNormal;
        private System.Windows.Forms.Button buttonPositions;
        private System.Windows.Forms.Button buttonResetTheme;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonDeleteSystemDB;
        private System.Windows.Forms.Button buttonResetTabs;
        private System.Windows.Forms.Button buttonRemoveDLLs;
        private System.Windows.Forms.Button button1;
    }
}