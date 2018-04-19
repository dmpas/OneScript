﻿/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using ScriptEngine;
using ScriptEngine.Compiler;
using ScriptEngine.HostedScript;

namespace oscript
{
	internal class MakeAppBehavior : AppBehavior
	{
		private readonly string _codePath;

		private readonly string _exePath;

		public MakeAppBehavior(string codePath, string exePath)
		{
			_codePath = codePath;
			_exePath = exePath;
		}

	    public bool CreateDumpOnly { get; set; }
	    
	    public override int Execute()
		{
			Output.WriteLine("Make started...");

		    if (CreateDumpOnly)
		    {
		        using (var ms = new MemoryStream())
		        {
		            CreateDump(ms);
                }
		    }
		    else
		    {
		        CreateExe();
		    }

		    Output.WriteLine("Make completed");
			return 0;
		}

	    private void CreateDump(Stream output)
	    {
	        var offset = (int)output.Length;

	        var engine = new HostedScriptEngine
	        {
	            CustomConfig = ScriptFileHelper.CustomConfigPath(_codePath)
	        };
	        engine.Initialize();
	        ScriptFileHelper.OnBeforeScriptRead(engine);
	        var source = engine.Loader.FromFile(_codePath);
	        var compiler = engine.GetCompilerService();
	        engine.SetGlobalEnvironment(new DoNothingHost(), source);
	        var entry = compiler.Compile(source);

	        var embeddedContext = engine.GetUserAddedScripts();

	        using (var bw = new BinaryWriter(output))
	        {
	            var userAddedScripts = embeddedContext as IList<UserAddedScript> ?? embeddedContext.ToList();
	            bw.Write(userAddedScripts.Count + 1);

	            var persistor = new ModulePersistor();
	            persistor.Save(new UserAddedScript
	            {
	                Type = UserAddedScriptType.Module,
	                Symbol = "$entry",
	                Image = entry
	            }, output);

	            foreach (var item in userAddedScripts)
	                persistor.Save(item, output);

	            var signature = new byte[]
	            {
	                0x4f,
	                0x53,
	                0x4d,
	                0x44
	            };
	            output.Write(signature, 0, signature.Length);

	            bw.Write(offset);
	            OutputToFile(output);
	        }
        }

	    private void CreateExe()
		{
			using (var exeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("oscript.StandaloneRunner.exe"))
			using (var output = new MemoryStream())
			{
				exeStream?.CopyTo(output);

				CreateDump(output);
			}
		}


		private void OutputToFile(Stream memoryStream)
		{
			using (var fileOutput = new FileStream(_exePath, FileMode.Create))
			{
				memoryStream.Position = 0;
				memoryStream.CopyTo(fileOutput);
			}
		}
	}
}