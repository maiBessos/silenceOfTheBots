using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using System.CodeDom.Compiler;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.Reflection;

//using Westwind.RemoteLoader;
using System.Collections.Generic;
using GoE;
using GoE.Utils.DynamicCompilation.SandboxCode;
using GoE.Utils;

namespace DynamicCompilation
{
	/// <summary>
	/// Summary description for BasicExecution.
	/// </summary>
	public class BasicExecution : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button btnExecute;
		private System.Windows.Forms.TextBox txtCode;
		private System.Windows.Forms.TextBox txtAssemblyCode;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private TextBox txtUsingDirectives;
        private Label label3;
        private Button btnLinkFile;


        public delegate void ResultHandler(string result, object resObj);
        ResultHandler resHandler;
		public BasicExecution(ResultHandler r, List<string> AssembliesToAdd)
		{
            assembliesToAdd = AssembliesToAdd;
            //assembliesToAdd.Add(Assembly.GetEntryAssembly().FullName);
            assembliesToAdd.Add(typeof(Program).Assembly.Location);
           // assembliesToAdd.Add(Assembly.GetExecutingAssembly().FullName);

           // Assembly parentAssembly = Assembly.GetCallingAssembly();
           // while (parentAssembly != null)
           //{ 
           //     assembliesToAdd.Add(Assembly.GetCallingAssembly().FullName);
           //     if (parentAssembly == Assembly.GetCallingAssembly())
           //         break;
           //     parentAssembly = Assembly.GetCallingAssembly();
           // }

            resHandler = r;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();


			this.txtCode.Text = @"
string cName = ""Rick"";
MessageBox.Show(""Hello World"" + cName);
return (object) DateTime.Now;
";
			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.txtCode = new System.Windows.Forms.TextBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.txtAssemblyCode = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtUsingDirectives = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnLinkFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtCode
            // 
            this.txtCode.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCode.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCode.Location = new System.Drawing.Point(3, 159);
            this.txtCode.Multiline = true;
            this.txtCode.Name = "txtCode";
            this.txtCode.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtCode.Size = new System.Drawing.Size(536, 116);
            this.txtCode.TabIndex = 0;
            this.txtCode.Text = "txtCode";
            // 
            // btnExecute
            // 
            this.btnExecute.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExecute.Location = new System.Drawing.Point(8, 8);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(96, 24);
            this.btnExecute.TabIndex = 1;
            this.btnExecute.Text = "E&xecute";
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // txtAssemblyCode
            // 
            this.txtAssemblyCode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAssemblyCode.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAssemblyCode.Location = new System.Drawing.Point(0, 337);
            this.txtAssemblyCode.Multiline = true;
            this.txtAssemblyCode.Name = "txtAssemblyCode";
            this.txtAssemblyCode.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtAssemblyCode.Size = new System.Drawing.Size(536, 175);
            this.txtAssemblyCode.TabIndex = 2;
            this.txtAssemblyCode.Text = "txtAssemblyCode";
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(0, 307);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(152, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Generated Assembly Source:";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(5, 142);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(152, 14);
            this.label2.TabIndex = 4;
            this.label2.Text = "Source Code to Execute:";
            // 
            // txtUsingDirectives
            // 
            this.txtUsingDirectives.Location = new System.Drawing.Point(8, 76);
            this.txtUsingDirectives.Multiline = true;
            this.txtUsingDirectives.Name = "txtUsingDirectives";
            this.txtUsingDirectives.Size = new System.Drawing.Size(498, 50);
            this.txtUsingDirectives.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "using directives:";
            // 
            // btnLinkFile
            // 
            this.btnLinkFile.Location = new System.Drawing.Point(239, 8);
            this.btnLinkFile.Name = "btnLinkFile";
            this.btnLinkFile.Size = new System.Drawing.Size(159, 23);
            this.btnLinkFile.TabIndex = 8;
            this.btnLinkFile.Text = "Link Code with Existing File";
            this.btnLinkFile.UseVisualStyleBackColor = true;
            this.btnLinkFile.Click += new System.EventHandler(this.btnLinkFile_Click);
            // 
            // BasicExecution
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(536, 509);
            this.Controls.Add(this.btnLinkFile);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtUsingDirectives);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtAssemblyCode);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.txtCode);
            this.Name = "BasicExecution";
            this.Text = "Basic Dynamic Execution";
            this.Load += new System.EventHandler(this.BasicExecution_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
        #endregion

        bool silentMode = false;
        List<string> assembliesToAdd;
		private void btnExecute_Click(object sender, System.EventArgs e)
		{
			string lcCode;
            //string className = "MyNamespace.MyClass";
            string className = "MyClass";
            ASandBox obj;
            if (linkedFilename == "")
            {
                // *** Must create a fully functional assembly
                lcCode = @"
                        using System;
                        using System.IO;
                        using System.Windows.Forms;" +
                                                         txtUsingDirectives.Text +
                            @"
                        namespace MyNamespace {
                        public class MyClass : GoE.Utils.DynamicCompilation.SandboxCode.ASandBox {

                        public object func(params object[] Parameters) {
                        " + this.txtCode.Text +
                            "}   }    }";
            }
            else
            {
                try
                {
                    if (prevFileTxt != "")
                        lcCode = prevFileTxt;
                    else
                        lcCode = File.ReadAllText(FileUtils.TryFindingFile(linkedFilename));

                    int classNameEndIdx = lcCode.IndexOf(" : ASandBox"); // find a class that inherits from ASandBox and implements func()
                    if(classNameEndIdx == -1)
                   { 
                        MessageBox.Show("Couldn't find a class that inherits from ASandBox in the given linked file!");
                        return;
                    }
                    string substr = lcCode.Substring(0, classNameEndIdx - 1);
                    int classNameStartIdx = substr.LastIndexOf(' ');
                    className = lcCode.Substring(classNameStartIdx+1, classNameEndIdx - classNameStartIdx - 1);

                    //className = ASandBox.ChildrenByTypename[className].GetType().FullName;
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Can't read linked file");
                    return;
                }
            }

			ICodeCompiler loCompiler = new CSharpCodeProvider().CreateCompiler();
			CompilerParameters loParameters = new CompilerParameters();

			// *** Start by adding any referenced assemblies
			loParameters.ReferencedAssemblies.Add("System.dll");
            loParameters.ReferencedAssemblies.Add("System.Core.dll");
			loParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            //loParameters.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().FullName);
            loParameters.ReferencedAssemblies.AddRange(assembliesToAdd.ToArray());
            
			// *** Load the resulting assembly into memory
			loParameters.GenerateInMemory = true;
			
			// *** Now compile the whole thing
			CompilerResults loCompiled = loCompiler.CompileAssemblyFromSource(loParameters,lcCode);

			if (loCompiled.Errors.HasErrors) 
			{
				string lcErrorMsg = "";

				// *** Create Error String
				lcErrorMsg = loCompiled.Errors.Count.ToString() + " Errors:";
				for (int x=0;x<loCompiled.Errors.Count;x++) 
					lcErrorMsg = lcErrorMsg  + "\r\nLine: " + loCompiled.Errors[x].Line.ToString() + " - " + 
						loCompiled.Errors[x].ErrorText;

                //				MessageBox.Show(lcErrorMsg + "\r\n\r\n" + lcCode,"Compiler Demo",MessageBoxButtons.OK,MessageBoxIcon.Error);

                if (!silentMode)
                    resHandler.Invoke(lcErrorMsg + "\r\n\r\n" + lcCode, null);

				return;
			}

			this.txtAssemblyCode.Text = lcCode;

			Assembly loAssembly = loCompiled.CompiledAssembly;

			// *** Retrieve an object reference - since this object is 'dynamic' we can't explicitly
			// *** type it so it's of type Object
			//object loObject  = loAssembly.CreateInstance(className);
            //if (loObject == null) 
            //{
            //    //MessageBox.Show("Couldn't load class.");
            //    resHandler.Invoke("Couldn't load class.", null);
            //    return;
            //}

            //obj = (ASandBox)loObject;
            obj = ASandBox.ChildrenByTypename[className];

			object[] loCodeParms = new object[1];
			loCodeParms[0] = "West Wind Technologies";

			try 
			{
                //object loResult = loObject.GetType().InvokeMember("func",
	            //BindingFlags.InvokeMethod,null,loObject,loCodeParms);
                object loResult = obj.func();

				//DateTime ltNow = (DateTime) loResult;
				//MessageBox.Show("Method Call Result:\r\n\r\n" + loResult.ToString(),"Compiler Demo",MessageBoxButtons.OK,MessageBoxIcon.Information);
                resHandler.Invoke("Method Call Result:\r\n\r\n" + loResult.ToString(), loResult);
			}
			catch(Exception loError) 
			{
                //MessageBox.Show(loError.Message,"Compiler Demo",MessageBoxButtons.OK,MessageBoxIcon.Information);
                if(!silentMode)
                    resHandler.Invoke(loError.Message, null);
			}
		}

		private void BasicExecution_Load(object sender, System.EventArgs e)
		{
		
		}
        
        string linkedFilename = "";
        private void btnLinkFile_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> res = InputBox.ShowDialog("file", "select CS code file");
                linkedFilename = res[0];
                //txtCode.Visible = false;
                //txtUsingDirectives.Visible = false;
                //txtAssemblyCode.Visible = false;
                CreateFileWatcher(linkedFilename);  // for some reason, this doesn't work for visual studio files! this is why we have the timer

                prevFileTxt = "";
                fileRefresher = new Timer();
                fileRefresher.Interval = 1000;
                fileRefresher.Tick += FileRefresher_Tick;
                fileRefresher.Enabled = true;
            }
            catch (Exception) 
            {
                txtCode.Visible = true;
                txtUsingDirectives.Visible = true;
                txtAssemblyCode.Visible = true;
            }
        }

        private void FileRefresher_Tick(object sender, EventArgs e)
        {
            try
            {
                var txt = File.ReadAllText(FileUtils.TryFindingFile((linkedFilename)));
                if(txt != prevFileTxt)
                {
                    prevFileTxt = txt;
                    OnChanged(sender, null);
                }
            }
            catch (Exception) { }
        }

        string prevFileTxt;
        Timer fileRefresher;
        FileSystemWatcher watcher;
        public void CreateFileWatcher(string path)
        {
            
            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher();
            watcher.Path = path.Substring(0,path.LastIndexOf(Path.DirectorySeparatorChar));
            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcher.Filter = "*.cs";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // for some reason, this doesn't work for visual studio files! this is why we have the timer
            // if linked file was saved/changed - forward the action to 'execute'
            if (!this.InvokeRequired)
            {
                silentMode = true;
                btnExecute_Click(source, e);
                silentMode = false;
            }
            else
                this.Invoke((Action)delegate ()
                {
                    silentMode = true;
                    btnExecute_Click(source, e);
                    silentMode = false;
                });
            
        }
    }

	

}
