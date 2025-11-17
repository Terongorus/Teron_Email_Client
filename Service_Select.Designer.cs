namespace TeronEmailClient
{
    partial class Service_Select
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
            this.services_list = new System.Windows.Forms.ComboBox();
            this.remember_check_box = new System.Windows.Forms.CheckBox();
            this.login_button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // services_list
            // 
            this.services_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.services_list.FormattingEnabled = true;
            this.services_list.Location = new System.Drawing.Point(237, 160);
            this.services_list.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.services_list.Name = "services_list";
            this.services_list.Size = new System.Drawing.Size(310, 33);
            this.services_list.TabIndex = 0;
            this.services_list.SelectedIndexChanged += new System.EventHandler(this.services_list_SelectedIndexChanged);
            // 
            // remember_check_box
            // 
            this.remember_check_box.AutoSize = true;
            this.remember_check_box.Location = new System.Drawing.Point(273, 202);
            this.remember_check_box.Name = "remember_check_box";
            this.remember_check_box.Size = new System.Drawing.Size(239, 29);
            this.remember_check_box.TabIndex = 1;
            this.remember_check_box.Text = "Remember selection?";
            this.remember_check_box.UseVisualStyleBackColor = true;
            // 
            // login_button
            // 
            this.login_button.Location = new System.Drawing.Point(300, 254);
            this.login_button.Margin = new System.Windows.Forms.Padding(20);
            this.login_button.Name = "login_button";
            this.login_button.Size = new System.Drawing.Size(185, 47);
            this.login_button.TabIndex = 2;
            this.login_button.Text = "Login to service";
            this.login_button.UseVisualStyleBackColor = true;
            this.login_button.Click += new System.EventHandler(this.login_button_Click);
            // 
            // Service_Select
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.login_button);
            this.Controls.Add(this.remember_check_box);
            this.Controls.Add(this.services_list);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "Service_Select";
            this.Text = "Service_Select";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.ComboBox services_list;
        public System.Windows.Forms.CheckBox remember_check_box;
        public System.Windows.Forms.Button login_button;
    }
}