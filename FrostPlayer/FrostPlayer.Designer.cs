namespace FrostPlayer
{
    partial class FrostPlayer
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button prevButton;
        private System.Windows.Forms.Button nextButton;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label currentTimeLabel;
        private System.Windows.Forms.Label durationLabel;
        private System.Windows.Forms.TrackBar volumeTrackBar;
        private System.Windows.Forms.Label volumeLabel;
        private System.Windows.Forms.ListBox playlistListBox;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Label trackInfoLabel;
        private System.Windows.Forms.Label titleLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.playButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.prevButton = new System.Windows.Forms.Button();
            this.nextButton = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.currentTimeLabel = new System.Windows.Forms.Label();
            this.durationLabel = new System.Windows.Forms.Label();
            this.volumeTrackBar = new System.Windows.Forms.TrackBar();
            this.volumeLabel = new System.Windows.Forms.Label();
            this.playlistListBox = new System.Windows.Forms.ListBox();
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.clearButton = new System.Windows.Forms.Button();
            this.trackInfoLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.volumeTrackBar)).BeginInit();
            this.SuspendLayout();

            // titleLabel
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.titleLabel.Location = new System.Drawing.Point(20, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(140, 26);
            this.titleLabel.Text = "FrostPlayer";
            this.titleLabel.ForeColor = System.Drawing.Color.DodgerBlue;

            // trackInfoLabel
            this.trackInfoLabel.AutoSize = true;
            this.trackInfoLabel.Location = new System.Drawing.Point(20, 60);
            this.trackInfoLabel.Name = "trackInfoLabel";
            this.trackInfoLabel.Size = new System.Drawing.Size(80, 13);
            this.trackInfoLabel.Text = "Трек: Не выбран";

            // progressBar
            this.progressBar.Location = new System.Drawing.Point(20, 90);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(400, 20);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.progressBar_MouseDown);

            // currentTimeLabel
            this.currentTimeLabel.AutoSize = true;
            this.currentTimeLabel.Location = new System.Drawing.Point(20, 115);
            this.currentTimeLabel.Name = "currentTimeLabel";
            this.currentTimeLabel.Size = new System.Drawing.Size(34, 13);
            this.currentTimeLabel.Text = "00:00";

            // durationLabel
            this.durationLabel.AutoSize = true;
            this.durationLabel.Location = new System.Drawing.Point(386, 115);
            this.durationLabel.Name = "durationLabel";
            this.durationLabel.Size = new System.Drawing.Size(34, 13);
            this.durationLabel.Text = "00:00";

            // prevButton
            this.prevButton.Location = new System.Drawing.Point(20, 140);
            this.prevButton.Name = "prevButton";
            this.prevButton.Size = new System.Drawing.Size(75, 30);
            this.prevButton.Text = "⏮ Пред.";
            this.prevButton.Click += new System.EventHandler(this.prevButton_Click);

            // playButton
            this.playButton.Location = new System.Drawing.Point(101, 140);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(100, 30);
            this.playButton.Text = "▶️ Воспр.";
            this.playButton.Click += new System.EventHandler(this.playButton_Click);

            // stopButton
            this.stopButton.Location = new System.Drawing.Point(207, 140);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 30);
            this.stopButton.Text = "⏹ Стоп";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);

            // nextButton
            this.nextButton.Location = new System.Drawing.Point(288, 140);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(75, 30);
            this.nextButton.Text = "⏭ След.";
            this.nextButton.Click += new System.EventHandler(this.nextButton_Click);

            // volumeLabel
            this.volumeLabel.AutoSize = true;
            this.volumeLabel.Location = new System.Drawing.Point(20, 185);
            this.volumeLabel.Name = "volumeLabel";
            this.volumeLabel.Size = new System.Drawing.Size(87, 13);
            this.volumeLabel.Text = "Громкость: 50%";

            // volumeTrackBar
            this.volumeTrackBar.Location = new System.Drawing.Point(113, 180);
            this.volumeTrackBar.Maximum = 100;
            this.volumeTrackBar.Value = 50;
            this.volumeTrackBar.Size = new System.Drawing.Size(250, 45);
            this.volumeTrackBar.Scroll += new System.EventHandler(this.volumeTrackBar_Scroll);

            // playlistListBox
            this.playlistListBox.Location = new System.Drawing.Point(20, 230);
            this.playlistListBox.Size = new System.Drawing.Size(400, 150);
            this.playlistListBox.DoubleClick += new System.EventHandler(this.playlistListBox_DoubleClick);

            // addButton
            this.addButton.Location = new System.Drawing.Point(20, 390);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(100, 30);
            this.addButton.Text = "➕ Добавить";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);

            // removeButton
            this.removeButton.Location = new System.Drawing.Point(126, 390);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(100, 30);
            this.removeButton.Text = "➖ Удалить";
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);

            // clearButton
            this.clearButton.Location = new System.Drawing.Point(232, 390);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(100, 30);
            this.clearButton.Text = "🗑️ Очистить";
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);

            // FrostPlayer Form
            this.ClientSize = new System.Drawing.Size(450, 450);
            this.Text = "FrostPlayer";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.trackInfoLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.currentTimeLabel);
            this.Controls.Add(this.durationLabel);
            this.Controls.Add(this.prevButton);
            this.Controls.Add(this.playButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.nextButton);
            this.Controls.Add(this.volumeLabel);
            this.Controls.Add(this.volumeTrackBar);
            this.Controls.Add(this.playlistListBox);
            this.Controls.Add(this.addButton);
            this.Controls.Add(this.removeButton);
            this.Controls.Add(this.clearButton);

            ((System.ComponentModel.ISupportInitialize)(this.volumeTrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}