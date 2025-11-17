using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace TeronEmailClient
{
    public partial class Service_Select : Form
    {
        public Service_Select()
        {
            App_Methods app_methods = new App_Methods();
            Main_Form main_form = new Main_Form();
            app_methods.CheckFor_Config_File(main_form, this);
            InitializeComponent();
            app_methods.Add_Services_To_List(this);
        }

        private void services_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            App_Methods app_methods = new App_Methods();

            Globals.selected_service_url = app_methods.Service_Selection(this, services_list.SelectedItem.ToString());
        }

        private void login_button_Click(object sender, EventArgs e)
        {
            App_Methods app_Methods = new App_Methods();
            Main_Form main_form = new Main_Form();
            app_Methods.Open_Service(this, main_form);
        }
    }
}
