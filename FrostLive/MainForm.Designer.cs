namespace FrostLive
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.mainContainer = new System.Windows.Forms.Panel();
            this.columnsPanel = new System.Windows.Forms.Panel();
            this._historyControl = new FrostLive.Controls.TrackListControl();
            this.leftPanel = new System.Windows.Forms.Panel();
            this._newTracksControl = new FrostLive.Controls.TrackListControl();
            this.playerControl1 = new FrostLive.Controls.PlayerControl();
            this.mainContainer.SuspendLayout();
            this.columnsPanel.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainContainer
            // 
            this.mainContainer.BackColor = System.Drawing.Color.Transparent;
            this.mainContainer.Controls.Add(this.columnsPanel);
            this.mainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainContainer.Location = new System.Drawing.Point(0, 0);
            this.mainContainer.Name = "mainContainer";
            this.mainContainer.Padding = new System.Windows.Forms.Padding(13);
            this.mainContainer.Size = new System.Drawing.Size(862, 528);
            this.mainContainer.TabIndex = 0;
            // 
            // columnsPanel
            // 
            this.columnsPanel.BackColor = System.Drawing.Color.Transparent;
            this.columnsPanel.Controls.Add(this._historyControl);
            this.columnsPanel.Controls.Add(this.leftPanel);
            this.columnsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.columnsPanel.Location = new System.Drawing.Point(13, 13);
            this.columnsPanel.Name = "columnsPanel";
            this.columnsPanel.Size = new System.Drawing.Size(836, 502);
            this.columnsPanel.TabIndex = 0;
            // 
            // _historyControl
            // 
            this._historyControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._historyControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(5)))), ((int)(((byte)(8)))));
            this._historyControl.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(5)))), ((int)(((byte)(8)))));
            this._historyControl.BorderColor = System.Drawing.Color.Cyan;
            this._historyControl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._historyControl.HoverItemColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._historyControl.Location = new System.Drawing.Point(455, 0);
            this._historyControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this._historyControl.Name = "_historyControl";
            this._historyControl.SelectedItemColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._historyControl.ShowRefreshButton = true;
            this._historyControl.Size = new System.Drawing.Size(381, 497);
            this._historyControl.TabIndex = 1;
            this._historyControl.Title = "PLAY HISTORY";
            // 
            // leftPanel
            // 
            this.leftPanel.BackColor = System.Drawing.Color.Transparent;
            this.leftPanel.Controls.Add(this.playerControl1);
            this.leftPanel.Controls.Add(this._newTracksControl);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.Location = new System.Drawing.Point(0, 0);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Padding = new System.Windows.Forms.Padding(0, 0, 7, 0);
            this.leftPanel.Size = new System.Drawing.Size(448, 502);
            this.leftPanel.TabIndex = 0;
            // 
            // _newTracksControl
            // 
            this._newTracksControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this._newTracksControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(5)))), ((int)(((byte)(8)))));
            this._newTracksControl.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(5)))), ((int)(((byte)(5)))), ((int)(((byte)(8)))));
            this._newTracksControl.BorderColor = System.Drawing.Color.Cyan;
            this._newTracksControl.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._newTracksControl.HoverItemColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._newTracksControl.Location = new System.Drawing.Point(0, 154);
            this._newTracksControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this._newTracksControl.Name = "_newTracksControl";
            this._newTracksControl.SelectedItemColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this._newTracksControl.Size = new System.Drawing.Size(435, 343);
            this._newTracksControl.TabIndex = 1;
            this._newTracksControl.Title = "NEW TRACKS";
            // 
            // playerControl1
            // 
            this.playerControl1.BackColor = System.Drawing.Color.Transparent;
            this.playerControl1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.playerControl1.Location = new System.Drawing.Point(0, 0);
            this.playerControl1.MinimumSize = new System.Drawing.Size(400, 120);
            this.playerControl1.Name = "playerControl1";
            this.playerControl1.Size = new System.Drawing.Size(435, 146);
            this.playerControl1.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(10)))), ((int)(((byte)(15)))));
            this.ClientSize = new System.Drawing.Size(862, 528);
            this.Controls.Add(this.mainContainer);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(539, 339);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrostLive";
            this.mainContainer.ResumeLayout(false);
            this.columnsPanel.ResumeLayout(false);
            this.leftPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel mainContainer;
        private System.Windows.Forms.Panel columnsPanel;
        private System.Windows.Forms.Panel leftPanel;
        private Controls.TrackListControl _newTracksControl;
        private Controls.TrackListControl _historyControl;
        private Controls.PlayerControl playerControl1;
    }
}