using FrostPlayer.Controls;

namespace FrostPlayer
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            trackInfoLabel = new Label();
            progressBar = new MediaProgressBar();
            currentTimeLabel = new Label();
            durationLabel = new Label();
            playButton = new Button();
            stopButton = new Button();
            prevButton = new Button();
            nextButton = new Button();
            addButton = new Button();
            removeButton = new Button();
            clearButton = new Button();
            label1 = new Label();
            panel1 = new Panel();
            panel3 = new Panel();
            volumeControl1 = new VolumeControl();
            playlist = new PlaylistControl();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // trackInfoLabel
            // 
            trackInfoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackInfoLabel.AutoSize = true;
            trackInfoLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            trackInfoLabel.Location = new Point(12, 15);
            trackInfoLabel.Name = "trackInfoLabel";
            trackInfoLabel.Size = new Size(45, 15);
            trackInfoLabel.TabIndex = 0;
            trackInfoLabel.Text = "Трек: -";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.BarBackgroundColor = Color.FromArgb(240, 240, 240);
            progressBar.BufferColor = Color.FromArgb(100, 100, 100, 100);
            progressBar.CurrentTime = 0D;
            progressBar.Duration = 0D;
            progressBar.Location = new Point(12, 43);
            progressBar.Name = "progressBar";
            progressBar.ProgressColor = Color.DodgerBlue;
            progressBar.ShowTimeTooltip = false;
            progressBar.Size = new Size(506, 23);
            progressBar.TabIndex = 1;
            progressBar.MouseMove += progressBar_MouseMove;
            // 
            // currentTimeLabel
            // 
            currentTimeLabel.AutoSize = true;
            currentTimeLabel.Location = new Point(12, 69);
            currentTimeLabel.Name = "currentTimeLabel";
            currentTimeLabel.Size = new Size(34, 13);
            currentTimeLabel.TabIndex = 2;
            currentTimeLabel.Text = "00:00";
            // 
            // durationLabel
            // 
            durationLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            durationLabel.AutoSize = true;
            durationLabel.Location = new Point(578, 69);
            durationLabel.Name = "durationLabel";
            durationLabel.Size = new Size(28, 13);
            durationLabel.TabIndex = 3;
            durationLabel.Text = "0:00";
            durationLabel.TextAlign = ContentAlignment.TopRight;
            // 
            // playButton
            // 
            playButton.Location = new Point(95, 3);
            playButton.Name = "playButton";
            playButton.Size = new Size(86, 30);
            playButton.TabIndex = 4;
            playButton.Text = "▶️ Воспр.";
            playButton.UseVisualStyleBackColor = true;
            // 
            // stopButton
            // 
            stopButton.Location = new Point(187, 3);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(75, 30);
            stopButton.TabIndex = 5;
            stopButton.Text = "⏹️ Стоп";
            stopButton.UseVisualStyleBackColor = true;
            // 
            // prevButton
            // 
            prevButton.Location = new Point(14, 3);
            prevButton.Name = "prevButton";
            prevButton.Size = new Size(75, 30);
            prevButton.TabIndex = 6;
            prevButton.Text = "⏮️ Пред.";
            prevButton.UseVisualStyleBackColor = true;
            // 
            // nextButton
            // 
            nextButton.Location = new Point(268, 3);
            nextButton.Name = "nextButton";
            nextButton.Size = new Size(75, 30);
            nextButton.TabIndex = 7;
            nextButton.Text = "⏭️ След.";
            nextButton.UseVisualStyleBackColor = true;
            // 
            // addButton
            // 
            addButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            addButton.Location = new Point(12, 3);
            addButton.Name = "addButton";
            addButton.Size = new Size(90, 30);
            addButton.TabIndex = 11;
            addButton.Text = "➕ Добавить";
            addButton.UseVisualStyleBackColor = true;
            // 
            // removeButton
            // 
            removeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            removeButton.Location = new Point(108, 3);
            removeButton.Name = "removeButton";
            removeButton.Size = new Size(90, 30);
            removeButton.TabIndex = 12;
            removeButton.Text = "➖ Удалить";
            removeButton.UseVisualStyleBackColor = true;
            // 
            // clearButton
            // 
            clearButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            clearButton.Location = new Point(511, 3);
            clearButton.Name = "clearButton";
            clearButton.Size = new Size(90, 30);
            clearButton.TabIndex = 13;
            clearButton.Text = "🗑️ Очистить";
            clearButton.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(12, 142);
            label1.Name = "label1";
            label1.Size = new Size(64, 15);
            label1.TabIndex = 14;
            label1.Text = "Плейлист";
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Controls.Add(prevButton);
            panel1.Controls.Add(playButton);
            panel1.Controls.Add(stopButton);
            panel1.Controls.Add(nextButton);
            panel1.Location = new Point(12, 85);
            panel1.Name = "panel1";
            panel1.Size = new Size(600, 36);
            panel1.TabIndex = 15;
            // 
            // panel3
            // 
            panel3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel3.Controls.Add(addButton);
            panel3.Controls.Add(removeButton);
            panel3.Controls.Add(clearButton);
            panel3.Location = new Point(12, 352);
            panel3.Name = "panel3";
            panel3.Size = new Size(600, 37);
            panel3.TabIndex = 17;
            // 
            // volumeControl1
            // 
            volumeControl1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            volumeControl1.FillColor = Color.DodgerBlue;
            volumeControl1.Location = new Point(524, 43);
            volumeControl1.Name = "volumeControl1";
            volumeControl1.Size = new Size(88, 23);
            volumeControl1.TabIndex = 18;
            volumeControl1.Text = "volumeControl1";
            volumeControl1.ThumbBorderColor = Color.FromArgb(180, 180, 180);
            volumeControl1.ThumbColor = Color.White;
            volumeControl1.TrackColor = Color.FromArgb(220, 220, 220);
            volumeControl1.ValueChanged += VolumeTrackBar_ValueChanged;
            // 
            // playlist
            // 
            playlist.AlternateRowBackgroundColor = Color.FromArgb(248, 248, 248);
            playlist.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            playlist.DurationTextColor = Color.FromArgb(120, 120, 120);
            playlist.HeaderBackgroundColor = Color.FromArgb(245, 245, 245);
            playlist.HeaderHeight = 20;
            playlist.HeaderTextColor = Color.FromArgb(80, 80, 80);
            playlist.Location = new Point(12, 160);
            playlist.Name = "playlist";
            playlist.PlayingIndicatorColor = Color.DodgerBlue;
            playlist.RowBackgroundColor = Color.White;
            playlist.RowHeight = 20;
            playlist.SelectedItem = null;
            playlist.SelectionColor = Color.FromArgb(200, 230, 255);
            playlist.Size = new Size(583, 186);
            playlist.TabIndex = 20;
            playlist.Text = "playlist";
            playlist.TextColor = Color.FromArgb(60, 60, 60);
            playlist.WidthDurationColumn = 90;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(624, 461);
            Controls.Add(playlist);
            Controls.Add(volumeControl1);
            Controls.Add(panel1);
            Controls.Add(label1);
            Controls.Add(durationLabel);
            Controls.Add(currentTimeLabel);
            Controls.Add(progressBar);
            Controls.Add(trackInfoLabel);
            Controls.Add(panel3);
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
            MinimumSize = new Size(640, 500);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Frost Player";
            Resize += MainForm_Resize;
            panel1.ResumeLayout(false);
            panel3.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label trackInfoLabel;
        private MediaProgressBar progressBar;
        private System.Windows.Forms.Label currentTimeLabel;
        private System.Windows.Forms.Label durationLabel;
        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button prevButton;
        private System.Windows.Forms.Button nextButton;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel3;
        private VolumeControl volumeControl1;
        private PlaylistControl playlist;
    }
}