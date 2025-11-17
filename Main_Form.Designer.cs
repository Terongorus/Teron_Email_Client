using System.Runtime.CompilerServices;

namespace TeronEmailClient
{
    partial class Main_Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main_Form));
            this.email_viewer = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.email_viewer)).BeginInit();
            this.SuspendLayout();
            // 
            // email_viewer
            // 
            this.email_viewer.AllowExternalDrop = true;
            this.email_viewer.CreationProperties = null;
            this.email_viewer.DefaultBackgroundColor = System.Drawing.Color.White;
            this.email_viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.email_viewer.Location = new System.Drawing.Point(0, 0);
            this.email_viewer.Margin = new System.Windows.Forms.Padding(0);
            this.email_viewer.Name = "email_viewer";
            this.email_viewer.Size = new System.Drawing.Size(1200, 692);
            this.email_viewer.TabIndex = 0;
            this.email_viewer.ZoomFactor = 1D;
            this.email_viewer.Click += new System.EventHandler(this.email_viewer_Click);
            // 
            // Main_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 692);
            this.Controls.Add(this.email_viewer);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Main_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Teron\'s Email Client";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.email_viewer)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public Microsoft.Web.WebView2.WinForms.WebView2 email_viewer;
    }
}

