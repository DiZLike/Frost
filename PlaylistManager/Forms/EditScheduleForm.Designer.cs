namespace PlaylistManager.Forms
{
    partial class EditScheduleForm
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
            components = new System.ComponentModel.Container();
            lblName = new Label();
            txtName = new TextBox();
            lblPath = new Label();
            txtPath = new TextBox();
            lblStart = new Label();
            lblEnd = new Label();
            lblDays = new Label();
            btnOK = new Button();
            btnBrowse = new Button();
            toolTip1 = new ToolTip(components);
            dtpStartTime = new DateTimePicker();
            dtpEndTime = new DateTimePicker();
            pnlDays = new Panel();
            chkSunday = new CheckBox();
            chkSaturday = new CheckBox();
            chkFriday = new CheckBox();
            chkThursday = new CheckBox();
            chkWednesday = new CheckBox();
            chkTuesday = new CheckBox();
            chkMonday = new CheckBox();
            chkAllDays = new CheckBox();
            btnCancel = new Button();
            pnlDays.SuspendLayout();
            SuspendLayout();
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(23, 23);
            lblName.Margin = new Padding(4, 0, 4, 0);
            lblName.Name = "lblName";
            lblName.Size = new Size(62, 15);
            lblName.TabIndex = 0;
            lblName.Text = "Название:";
            // 
            // txtName
            // 
            txtName.Location = new Point(140, 20);
            txtName.Margin = new Padding(4, 3, 4, 3);
            txtName.Name = "txtName";
            txtName.Size = new Size(291, 23);
            txtName.TabIndex = 1;
            // 
            // lblPath
            // 
            lblPath.AutoSize = true;
            lblPath.Location = new Point(23, 58);
            lblPath.Margin = new Padding(4, 0, 4, 0);
            lblPath.Name = "lblPath";
            lblPath.Size = new Size(106, 15);
            lblPath.TabIndex = 2;
            lblPath.Text = "Путь к плейлисту:";
            // 
            // txtPath
            // 
            txtPath.Location = new Point(140, 54);
            txtPath.Margin = new Padding(4, 3, 4, 3);
            txtPath.Name = "txtPath";
            txtPath.Size = new Size(244, 23);
            txtPath.TabIndex = 3;
            // 
            // lblStart
            // 
            lblStart.AutoSize = true;
            lblStart.Location = new Point(23, 92);
            lblStart.Margin = new Padding(4, 0, 4, 0);
            lblStart.Name = "lblStart";
            lblStart.Size = new Size(87, 15);
            lblStart.TabIndex = 4;
            lblStart.Text = "Время начала:";
            // 
            // lblEnd
            // 
            lblEnd.AutoSize = true;
            lblEnd.Location = new Point(23, 127);
            lblEnd.Margin = new Padding(4, 0, 4, 0);
            lblEnd.Name = "lblEnd";
            lblEnd.Size = new Size(108, 15);
            lblEnd.TabIndex = 8;
            lblEnd.Text = "Время окончания:";
            // 
            // lblDays
            // 
            lblDays.AutoSize = true;
            lblDays.Location = new Point(23, 162);
            lblDays.Margin = new Padding(4, 0, 4, 0);
            lblDays.Name = "lblDays";
            lblDays.Size = new Size(74, 15);
            lblDays.TabIndex = 12;
            lblDays.Text = "Дни недели:";
            // 
            // btnOK
            // 
            btnOK.Location = new Point(140, 300);
            btnOK.Margin = new Padding(4, 3, 4, 3);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(117, 35);
            btnOK.TabIndex = 14;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(391, 52);
            btnBrowse.Margin = new Padding(4, 3, 4, 3);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(41, 27);
            btnBrowse.TabIndex = 4;
            btnBrowse.Text = "...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // dtpStartTime
            // 
            dtpStartTime.Format = DateTimePickerFormat.Time;
            dtpStartTime.Location = new Point(140, 90);
            dtpStartTime.Margin = new Padding(4, 3, 4, 3);
            dtpStartTime.Name = "dtpStartTime";
            dtpStartTime.ShowUpDown = true;
            dtpStartTime.Size = new Size(116, 23);
            dtpStartTime.TabIndex = 5;
            dtpStartTime.ValueChanged += dtpStartTime_ValueChanged;
            // 
            // dtpEndTime
            // 
            dtpEndTime.Format = DateTimePickerFormat.Time;
            dtpEndTime.Location = new Point(140, 125);
            dtpEndTime.Margin = new Padding(4, 3, 4, 3);
            dtpEndTime.Name = "dtpEndTime";
            dtpEndTime.ShowUpDown = true;
            dtpEndTime.Size = new Size(116, 23);
            dtpEndTime.TabIndex = 6;
            dtpEndTime.ValueChanged += dtpEndTime_ValueChanged;
            // 
            // pnlDays
            // 
            pnlDays.Controls.Add(chkSunday);
            pnlDays.Controls.Add(chkSaturday);
            pnlDays.Controls.Add(chkFriday);
            pnlDays.Controls.Add(chkThursday);
            pnlDays.Controls.Add(chkWednesday);
            pnlDays.Controls.Add(chkTuesday);
            pnlDays.Controls.Add(chkMonday);
            pnlDays.Controls.Add(chkAllDays);
            pnlDays.Location = new Point(140, 159);
            pnlDays.Margin = new Padding(4, 3, 4, 3);
            pnlDays.Name = "pnlDays";
            pnlDays.Size = new Size(292, 127);
            pnlDays.TabIndex = 7;
            // 
            // chkSunday
            // 
            chkSunday.AutoSize = true;
            chkSunday.Location = new Point(152, 98);
            chkSunday.Margin = new Padding(4, 3, 4, 3);
            chkSunday.Name = "chkSunday";
            chkSunday.Size = new Size(96, 19);
            chkSunday.TabIndex = 7;
            chkSunday.Tag = "sun";
            chkSunday.Text = "Воскресенье";
            chkSunday.UseVisualStyleBackColor = true;
            // 
            // chkSaturday
            // 
            chkSaturday.AutoSize = true;
            chkSaturday.Location = new Point(152, 69);
            chkSaturday.Margin = new Padding(4, 3, 4, 3);
            chkSaturday.Name = "chkSaturday";
            chkSaturday.Size = new Size(72, 19);
            chkSaturday.TabIndex = 6;
            chkSaturday.Tag = "sat";
            chkSaturday.Text = "Суббота";
            chkSaturday.UseVisualStyleBackColor = true;
            // 
            // chkFriday
            // 
            chkFriday.AutoSize = true;
            chkFriday.Location = new Point(152, 40);
            chkFriday.Margin = new Padding(4, 3, 4, 3);
            chkFriday.Name = "chkFriday";
            chkFriday.Size = new Size(73, 19);
            chkFriday.TabIndex = 5;
            chkFriday.Tag = "fri";
            chkFriday.Text = "Пятница";
            chkFriday.UseVisualStyleBackColor = true;
            // 
            // chkThursday
            // 
            chkThursday.AutoSize = true;
            chkThursday.Location = new Point(152, 12);
            chkThursday.Margin = new Padding(4, 3, 4, 3);
            chkThursday.Name = "chkThursday";
            chkThursday.Size = new Size(69, 19);
            chkThursday.TabIndex = 4;
            chkThursday.Tag = "thu";
            chkThursday.Text = "Четверг";
            chkThursday.UseVisualStyleBackColor = true;
            // 
            // chkWednesday
            // 
            chkWednesday.AutoSize = true;
            chkWednesday.Location = new Point(12, 98);
            chkWednesday.Margin = new Padding(4, 3, 4, 3);
            chkWednesday.Name = "chkWednesday";
            chkWednesday.Size = new Size(59, 19);
            chkWednesday.TabIndex = 3;
            chkWednesday.Tag = "wed";
            chkWednesday.Text = "Среда";
            chkWednesday.UseVisualStyleBackColor = true;
            // 
            // chkTuesday
            // 
            chkTuesday.AutoSize = true;
            chkTuesday.Location = new Point(12, 69);
            chkTuesday.Margin = new Padding(4, 3, 4, 3);
            chkTuesday.Name = "chkTuesday";
            chkTuesday.Size = new Size(72, 19);
            chkTuesday.TabIndex = 2;
            chkTuesday.Tag = "tue";
            chkTuesday.Text = "Вторник";
            chkTuesday.UseVisualStyleBackColor = true;
            // 
            // chkMonday
            // 
            chkMonday.AutoSize = true;
            chkMonday.Location = new Point(12, 40);
            chkMonday.Margin = new Padding(4, 3, 4, 3);
            chkMonday.Name = "chkMonday";
            chkMonday.Size = new Size(100, 19);
            chkMonday.TabIndex = 1;
            chkMonday.Tag = "mon";
            chkMonday.Text = "Понедельник";
            chkMonday.UseVisualStyleBackColor = true;
            // 
            // chkAllDays
            // 
            chkAllDays.AutoSize = true;
            chkAllDays.Location = new Point(12, 12);
            chkAllDays.Margin = new Padding(4, 3, 4, 3);
            chkAllDays.Name = "chkAllDays";
            chkAllDays.Size = new Size(68, 19);
            chkAllDays.TabIndex = 0;
            chkAllDays.Text = "Все дни";
            chkAllDays.UseVisualStyleBackColor = true;
            chkAllDays.CheckedChanged += chkAllDays_CheckedChanged;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(267, 300);
            btnCancel.Margin = new Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(117, 35);
            btnCancel.TabIndex = 15;
            btnCancel.Text = "Отмена";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // EditScheduleForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(448, 347);
            Controls.Add(btnCancel);
            Controls.Add(pnlDays);
            Controls.Add(dtpEndTime);
            Controls.Add(dtpStartTime);
            Controls.Add(btnBrowse);
            Controls.Add(btnOK);
            Controls.Add(lblDays);
            Controls.Add(lblEnd);
            Controls.Add(lblStart);
            Controls.Add(txtPath);
            Controls.Add(lblPath);
            Controls.Add(txtName);
            Controls.Add(lblName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EditScheduleForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Редактирование элемента расписания";
            pnlDays.ResumeLayout(false);
            pnlDays.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.Label lblDays;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.DateTimePicker dtpStartTime;
        private System.Windows.Forms.DateTimePicker dtpEndTime;
        private System.Windows.Forms.Panel pnlDays;
        private System.Windows.Forms.CheckBox chkMonday;
        private System.Windows.Forms.CheckBox chkTuesday;
        private System.Windows.Forms.CheckBox chkWednesday;
        private System.Windows.Forms.CheckBox chkThursday;
        private System.Windows.Forms.CheckBox chkFriday;
        private System.Windows.Forms.CheckBox chkSaturday;
        private System.Windows.Forms.CheckBox chkSunday;
        private System.Windows.Forms.CheckBox chkAllDays;
        private Button btnCancel;
    }
}