namespace Nyo.Fr.EmuNWA
{
    partial class NWAToolForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServerStatusLabel = new System.Windows.Forms.Label();
            this.ClientsListView = new System.Windows.Forms.ListView();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ServerStatusLabel
            // 
            this.ServerStatusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.ServerStatusLabel.AutoSize = true;
            this.ServerStatusLabel.Location = new System.Drawing.Point(130, 0);
            this.ServerStatusLabel.Name = "ServerStatusLabel";
            this.ServerStatusLabel.Size = new System.Drawing.Size(44, 13);
            this.ServerStatusLabel.TabIndex = 0;
            this.ServerStatusLabel.Text = "Nothing";
            // 
            // ClientsListView
            // 
            this.ClientsListView.HideSelection = false;
            this.ClientsListView.Location = new System.Drawing.Point(3, 35);
            this.ClientsListView.Name = "ClientsListView";
            this.ClientsListView.Size = new System.Drawing.Size(296, 209);
            this.ClientsListView.TabIndex = 1;
            this.ClientsListView.UseCompatibleStateImageBehavior = false;
            this.ClientsListView.View = System.Windows.Forms.View.List;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.ServerStatusLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.ClientsListView, 0, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 46);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.95547F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 87.04453F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(304, 247);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // NWAToolForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(323, 299);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "NWAToolForm";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label ServerStatusLabel;
        private System.Windows.Forms.ListView ClientsListView;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}

