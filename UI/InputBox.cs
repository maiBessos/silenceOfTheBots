using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoE
{
    class InputBox
    {
        
        // returns null if dialog was closed without confirmation
        public static List<string> ShowDialog(string text, string caption, string defaultValue = null)
        {
            string[] defaults = null;
            if (defaultValue != null)
                defaults = new string[1] { defaultValue };
            return ShowDialog(new string[1] { text }, caption, defaults);
        }
        public static List<string> ShowDialog(string[] text, string caption, string[] defaultValues = null)
        {
            if (text.Count() == 0)
                return new List<string>();

            Form prompt = new Form();
            prompt.Width = 800;

            prompt.Text = caption;
            List<Label> textLabel = new List<Label>();
            List<TextBox> inputBox = new List<TextBox>();

            int height = 50;
            int defaultIdx = 0;
            foreach (string s in text)
            {
                
                string txt = "";
                if (defaultValues != null)
                    txt = defaultValues[defaultIdx++];
                else
                {
                    try 
                    { 
                        if (!Utils.FileUtils.tryReadValueMapKey(s, AppConstants.PathLocations.GUI_DEFAULTS_VALUEMAP_FILE, out txt))
                            txt = "";
                    }
                    catch(Exception) {}
                }
                TextBox txtBox = new TextBox() { Left = 350, Top = height, Width = 330, Text = txt };
                inputBox.Add(txtBox);

                Label newL = new Label(){ Left = 50, Top = height, Text = s, Width = 150 };
                // TODO: instead of making right click open a file, make it open a menu with different input options (folder dialog, file dialog, a type of some class etc.)
                newL.MouseClick += (sender,e)=> 
                {
                    try
                    {
                        if (e.Button == MouseButtons.Middle)
                        {
                            txtBox.Text = "";
                            return;
                        }

                        if (e.Button == MouseButtons.Left)
                        {
                            OpenFileDialog d = new OpenFileDialog();
                            d.ShowDialog();
                            txtBox.Text = d.FileName;
                        }
                        else
                        {
                            
                            FolderBrowserDialog d = new FolderBrowserDialog();
                            d.ShowDialog();
                            txtBox.Text = d.SelectedPath;
                        }
                    }
                    catch (Exception) { }
                    
                };
                textLabel.Add(newL);

                height += 30;
            }
            prompt.Height = 80 + height;

            Button confirmation = new Button() { Text = "Ok", Left = 380, Width = 100, Top = 20 + height };
            bool confirmed = false;
            confirmation.Click += (sender, e) => { prompt.Close(); confirmed = true; };
            prompt.Controls.Add(confirmation);

            foreach (Label l in textLabel)
                prompt.Controls.Add(l);
            foreach (TextBox t in inputBox)
                prompt.Controls.Add(t);

            prompt.ShowDialog();

            if (confirmed == false)
                return null;

            if (defaultValues == null)  // update defaults
            {
                Dictionary<string, string> vals = new Dictionary<string, string>();
                for (int i = 0; i < inputBox.Count; ++i)
                    vals[textLabel[i].Text] = inputBox[i].Text;

                try
                {
                    Utils.FileUtils.updateValueMap(vals, AppConstants.PathLocations.GUI_DEFAULTS_VALUEMAP_FILE);
                }
                catch (Exception ex) 
                {
                }
            }

            List<string> output = new List<string>();
            foreach (TextBox t in inputBox)
                output.Add(t.Text);
            return output;
        }

        public static string ShowMultiChoiceDialog(string caption, List<string> options)
        {
            Form prompt = new Form();
            prompt.Width = 500;
            prompt.Text = caption;
            
            int height = 50;
            int defaultIdx = 0;
            
            ListBox lstBox = new ListBox { Left = 0, Top = 0, Width = 330 };
            foreach (var o in options)
            lstBox.Items.Add(o);
            lstBox.Height = 70 + 12 * options.Count;
            prompt.Height = lstBox.Top + lstBox.Height;

            Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 0 };
            bool confirmed = false;
            confirmation.Click += (sender, e) => { prompt.Close(); confirmed = true; };
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(lstBox);

            prompt.ShowDialog();

            if (confirmed == false || lstBox.SelectedItem == null)
                return null;

            return (string)lstBox.SelectedItem;
        }

        /// <summary>
        /// returns null if dialog was closed without confirmation.
        /// otherwise, maps values from their "text" header string to their value
        /// </summary>
        public static Dictionary<string, T> ShowDialog<T>(string[] text, string caption, string[] initialDefaultValues = null)
        {
            string[] defaultValues = new string[text.Count()];

            if(initialDefaultValues != null)
                initialDefaultValues.CopyTo(defaultValues, 0);

            while (true)
            {
                List<string> results = ShowDialog(text, caption, defaultValues);
                if (results == null)
                    return null;
                Dictionary<string, T> ret = new Dictionary<string, T>();
                bool wasError = false;
                string problems = "The following fields have incorrect type (" + typeof(T).ToString() + ") :\n\n";
                int valHeader = 0;
                foreach (string s in results)
                {
                    try
                    {
                        ret.Add(text[valHeader], (T)Convert.ChangeType(s, typeof(T)));
                        defaultValues[valHeader] = s;
                    }
                    catch (Exception)
                    {
                        problems += s + "\n\n";
                        wasError = true;
                    }
                    valHeader++;
                }

                if (!wasError)
                    return ret;
            }
        }
        
    }
}
