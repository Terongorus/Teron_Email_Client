using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Diagnostics;

namespace TeronEmailClient
{
    public class Globals
    {
        //predefined login URLs for the supported services
        public static string gmail_service_url = "https://mail.google.com";
        public static string outlook_service_url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=9199bf20-a13f-4107-85dc-02114787ef48&scope=https%3A%2F%2Foutlook.office.com%2F.default%20openid%20profile%20offline_access&redirect_uri=https%3A%2F%2Foutlook.office.com%2Fmail%2F&client-request-id=0d4b7e40-cde5-8b50-d26a-215a386c3e3a&response_mode=fragment&client_info=1&prompt=select_account&nonce=019a8b11-1562-7456-b784-7a59a4075a41&state=eyJpZCI6IjAxOWE4YjExLTE1NjItN2ExMS04YTgxLWI1NmJjYTk1NGE3MSIsIm1ldGEiOnsiaW50ZXJhY3Rpb25UeXBlIjoicmVkaXJlY3QifX0%3D%7CaHR0cHM6Ly9vdXRsb29rLm9mZmljZS5jb20vbWFpbC8_dWk9ZW4lMjUyRFVTJnJzPVVTJmF1dGg9MQ&claims=%7B%22access_token%22%3A%7B%22xms_cc%22%3A%7B%22values%22%3A%5B%22CP1%22%5D%7D%7D%7D&x-client-SKU=msal.js.browser&x-client-VER=4.14.0&response_type=code&code_challenge=JQ8HABBCNvlc52acmvIUOwg2t4gBzcxzdzvOZXfvMk0&code_challenge_method=S256";
        public static string abv_service_url = "https://www.abv.bg/";

        //user-selected service URL
        public static string selected_service_url = "";

        //configuration file path
        public static string config_file_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"TeronEmailClient","config.xml");
    }

    internal class App_Methods
    {
        public void CheckFileExists()
        {
            if (File.Exists(Globals.config_file_path) == false)
            {
                // Ensure the directory exists before creating the file
                string directoryPath = Path.GetDirectoryName(Globals.config_file_path);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Create a new config file if it doesn't exist
                XDocument new_config = new XDocument(
                    new XElement("Configuration")
                );
                new_config.Save(Globals.config_file_path);
                if (new_config.Root != null)
                {
                    new_config.Root.Add(new XElement("SelectedService", ""));
                    new_config.Root.Add(new XElement("RememberMode", "false"));
                    new_config.Save(Globals.config_file_path);
                }
                else
                {
                    MessageBox.Show("Error creating configuration file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        public void Save_Config(Service_Select selector, bool remember_mode_state)
        {
            CheckFileExists();

            XDocument config_doc = XDocument.Load(Globals.config_file_path);
            if (config_doc != null)
            {
                XElement root = config_doc.Element("Configuration");
                if (root != null)
                {
                    XElement serviceElement = root.Element("SelectedService");
                    XElement remember_mode = root.Element("RememberMode");
                    if (serviceElement != null && remember_mode != null && remember_mode_state)
                    {
                        serviceElement.Value = Globals.selected_service_url;
                        remember_mode.Value = remember_mode_state.ToString().ToLowerInvariant();
                    }
                    else
                    {
                        root.Add(new XElement("SelectedService", Globals.selected_service_url));
                        root.Add(new XElement("RememberMode", remember_mode_state.ToString().ToLowerInvariant()));
                    }
                    config_doc.Save(Globals.config_file_path);
                }
            }
        }        

        public void Add_Services_To_List(Service_Select selector)
        {
            selector.services_list.Items.Clear();
            selector.services_list.Items.Add("Gmail");
            selector.services_list.Items.Add("Outlook");
            selector.services_list.Items.Add("ABV Mail");
        }

        public string Service_Selection(Service_Select selector, string source)
        {
            switch (source)
            {
                case "Gmail":
                    return Globals.gmail_service_url;
                case "Outlook":
                    return Globals.outlook_service_url;
                case "ABV Mail":
                    return Globals.abv_service_url;
                default:
                    break;
            }

            return null;
        }

        public void CheckFor_Config_File(Main_Form main_form, Service_Select selector)
        {
            CheckFileExists();

            XDocument config_file_read = XDocument.Load(Globals.config_file_path);

            //run the main logic
            if (config_file_read.Root.Element("RememberMode").Value.ToString() == "true")
            {
                //load the value directly from the list
                Globals.selected_service_url = config_file_read.Root.Element("SelectedService").Value;

                if (string.IsNullOrEmpty(Globals.selected_service_url))
                {
                    MessageBox.Show("No service selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    selector.Hide();
                    main_form.email_viewer.Source = new Uri(Globals.selected_service_url);
                }
            }
            else
            {
                selector.Show();
                main_form.Hide();
            }
        }

        public void Open_Service(Service_Select selector, Main_Form main_form)
        {
            CheckFileExists();

            XDocument config_doc_read = XDocument.Load(Globals.config_file_path);

            if (config_doc_read.Root.Element("RememberMode").Value == "false" && string.Compare(config_doc_read.Root.Element("SelectedService").Value, Globals.selected_service_url) == 0)
            {
                if (selector.remember_check_box.Checked && !string.IsNullOrEmpty(Globals.selected_service_url))
                {
                    bool remember_mode = selector.remember_check_box.CheckState == CheckState.Checked ? true : false;
                    // Save the selected service URL to the config file
                    Save_Config(selector, remember_mode);
                }

                //use the user_defined URL to login
                if (string.IsNullOrEmpty(Globals.selected_service_url))
                {
                    MessageBox.Show("No service selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    selector.Hide();
                    main_form.Show();
                    main_form.email_viewer.Source = new Uri(Globals.selected_service_url);
                }
            }
            else if (config_doc_read.Root.Element("RememberMode").Value == "false" && string.Compare(config_doc_read.Root.Element("SelectedService").Value, Globals.selected_service_url) != 0)
            {
                //load the value directly from the list, if there's a mismatch
                config_doc_read.Root.Element("SelectedService").Value = Globals.selected_service_url;
                config_doc_read.Save(Globals.config_file_path);
                Globals.selected_service_url = config_doc_read.Root.Element("SelectedService").Value;

                if (string.IsNullOrEmpty(Globals.selected_service_url))
                {
                    MessageBox.Show("No service selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    selector.Hide();
                    main_form.Show();
                    main_form.email_viewer.Source = new Uri(Globals.selected_service_url);
                }
            }
            else
            {
                config_doc_read.Root.Element("RememberMode").Value = "false";
                config_doc_read.Save(Globals.config_file_path);
                MessageBox.Show("Configuration file is corrupted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Main application logic
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Instantiate the classes and objects
            Service_Select selector = new Service_Select();
            Main_Form main_form = new Main_Form();
            App_Methods app_methods = new App_Methods();

            app_methods.CheckFileExists();

            XDocument config_file_read = XDocument.Load(Globals.config_file_path);

            // Attach an event handler to ensure the application exits completely
            main_form.FormClosed += (sender, e) => ForceTerminateApplication();
            selector.FormClosed += (sender, e) => ForceTerminateApplication();

            try
            {
                // Run the main logic
                if (config_file_read.Root.Element("RememberMode").Value == "true")
                {
                    // Load the value directly from the list
                    Globals.selected_service_url = config_file_read.Root.Element("SelectedService").Value;

                    if (string.IsNullOrEmpty(Globals.selected_service_url))
                    {
                        MessageBox.Show("No service selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    else
                    {
                        main_form.email_viewer.Source = new Uri(Globals.selected_service_url);
                        Application.Run(main_form); // Set main_form as the main form
                    }
                }
                else
                {
                    Application.Run(selector); // Set selector as the main form
                }
            }
            finally
            {
                // Ensure the application is terminated completely
                ForceTerminateApplication();
            }
        }

        /// <summary>
        /// Forces the application to terminate completely.
        /// </summary>
        private static void ForceTerminateApplication()
        {
            // Ensure all threads and resources are disposed of
            Application.ExitThread();
            Application.Exit();
            TerminateAppProcesses();
            Environment.Exit(0);
        }

        /// <summary>
        /// Terminates all running processes with the same name as the current executable.
        /// </summary>
        public static void TerminateAppProcesses()
        {
            try
            {
                string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                var processes = Process.GetProcessesByName(exeName);
                foreach (var proc in processes)
                {
                    // Don't kill the current process until the end
                    if (proc.Id != Process.GetCurrentProcess().Id)
                    {
                        try { proc.Kill(); } catch { /* ignore */ }
                    }
                }
            }
            catch { /* ignore errors */ }
        }
    }
}
